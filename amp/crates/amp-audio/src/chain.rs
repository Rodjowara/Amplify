//! Audio output pipeline (`AudioPipeline`) connecting Snapcast to `cpal` (and DSP stages).

use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::mpsc as std_mpsc;
use std::thread::{self, JoinHandle};
use std::time::Duration;

use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use snapcast_client::{ClientEvent, SampleFormat};
use tokio::sync::Mutex;

use crate::snapcast::{SnapcastClientHandle, SnapcastStreamConfig};

/// Active session state held by [`AudioPipeline`].
struct ActiveSession {
    snapcast: Arc<SnapcastClientHandle>,
    stop_flag: Arc<AtomicBool>,
    output_thread: Option<JoinHandle<()>>,
}

impl ActiveSession {
    async fn stop(mut self) {
        self.stop_flag.store(true, Ordering::SeqCst);
        if let Some(handle) = self.output_thread.take() {
            let _ = handle.join();
        }
        self.snapcast.stop().await;
    }
}

/// Thread-safe controller managing the active Snapcast stream and `cpal` audio output.
///
/// Can be shared across gRPC handlers (`amp-control`) and UI callbacks (`amp-ui`).
#[derive(Default)]
pub struct AudioPipeline {
    session: Mutex<Option<ActiveSession>>,
}

impl std::fmt::Debug for AudioPipeline {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.debug_struct("AudioPipeline").finish_non_exhaustive()
    }
}

impl AudioPipeline {
    /// Create a new idle [`AudioPipeline`].
    pub fn new() -> Self {
        Self {
            session: Mutex::new(None),
        }
    }

    /// Connect (or reconnect) to Maestro's `snapserver` at `host:port` and start audio playback.
    ///
    /// If a stream is already active, it is cleanly stopped before starting the new connection.
    pub async fn set_stream(&self, host: impl Into<String>, port: u16) {
        let mut guard = self.session.lock().await;
        if let Some(prev) = guard.take() {
            prev.stop().await;
        }

        let cfg = SnapcastStreamConfig {
            host: host.into(),
            port,
            ..SnapcastStreamConfig::default()
        };

        let mut handle = SnapcastClientHandle::spawn(cfg);
        let (format_tx, format_rx) = std_mpsc::channel::<SampleFormat>();

        // Forward StreamStarted format events so `cpal` opens at the exact server sample rate
        if let Some(mut event_rx) = handle.take_event_rx() {
            tokio::spawn(async move {
                while let Some(event) = event_rx.recv().await {
                    if let ClientEvent::StreamStarted { format, .. } = event
                        && format_tx.send(format).is_err()
                    {
                        break;
                    }
                }
            });
        }

        let snapcast = Arc::new(handle);
        let stop_flag = Arc::new(AtomicBool::new(false));
        let output_thread =
            spawn_cpal_worker(Arc::clone(&snapcast), format_rx, Arc::clone(&stop_flag));

        *guard = Some(ActiveSession {
            snapcast,
            stop_flag,
            output_thread: Some(output_thread),
        });
    }

    /// Stop the current Snapcast client and audio output if running.
    pub async fn stop(&self) {
        let mut guard = self.session.lock().await;
        if let Some(prev) = guard.take() {
            prev.stop().await;
        }
    }
}

/// Spawns a dedicated OS thread owning the `!Send` `cpal::Stream`.
///
/// Whenever `StreamStarted` arrives with the server's [`SampleFormat`] (e.g. `48000:16:2`),
/// the worker opens (or re-opens) the audio output device at that exact sample rate and
/// channel count, and passes the hardware DAC latency into [`SnapcastClientHandle::pull_synced_f32`].
fn spawn_cpal_worker(
    snapcast: Arc<SnapcastClientHandle>,
    format_rx: std_mpsc::Receiver<SampleFormat>,
    stop_flag: Arc<AtomicBool>,
) -> JoinHandle<()> {
    thread::Builder::new()
        .name("amp-audio-cpal".into())
        .spawn(move || {
            let mut active_stream: Option<cpal::Stream> = None;
            while !stop_flag.load(Ordering::Relaxed) {
                if let Ok(format) = format_rx.recv_timeout(Duration::from_millis(50)) {
                    drop(active_stream.take());
                    active_stream = open_cpal_stream(&snapcast, format);
                }
            }
        })
        .expect("failed to spawn amp-audio-cpal thread")
}

fn open_cpal_stream(
    snapcast: &Arc<SnapcastClientHandle>,
    format: SampleFormat,
) -> Option<cpal::Stream> {
    let host = cpal::default_host();
    let Some(device) = host.default_output_device() else {
        eprintln!("[amp-audio] no default audio output device found");
        return None;
    };

    let desired_cfg = cpal::StreamConfig {
        channels: format.channels(),
        sample_rate: cpal::SampleRate(format.rate()),
        buffer_size: cpal::BufferSize::Default,
    };

    let snapcast_cb = Arc::clone(snapcast);
    let stream = device
        .build_output_stream(
            &desired_cfg,
            move |data: &mut [f32], info: &cpal::OutputCallbackInfo| {
                let dac_usec = dac_delay_usec(info);
                snapcast_cb.pull_synced_f32(data, dac_usec);
            },
            |err| eprintln!("[amp-audio] cpal stream error: {err}"),
            None,
        )
        .ok()?;

    stream.play().ok()?;
    Some(stream)
}

/// Computes the hardware DAC playback delay in microseconds from `cpal`'s callback timestamps.
fn dac_delay_usec(info: &cpal::OutputCallbackInfo) -> i64 {
    let ts = info.timestamp();
    ts.playback
        .duration_since(&ts.callback)
        .map_or(0, |d| d.as_micros() as i64)
}
