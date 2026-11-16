use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::mpsc as std_mpsc;
use std::thread::{self, JoinHandle};
use std::time::Duration;

use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use snapcast_client::{ClientEvent, SampleFormat};
use tokio::sync::Mutex;

use crate::snapcast::SnapcastClientHandle;

const FORMAT_POLL_INTERVAL: Duration = Duration::from_millis(50);

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

#[derive(Default)]
pub struct AudioPipeline {
    session: Mutex<Option<ActiveSession>>,
}

impl std::fmt::Debug for AudioPipeline {
    fn fmt(&self, formatter: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        formatter.debug_struct("AudioPipeline").finish_non_exhaustive()
    }
}

impl AudioPipeline {
    pub fn new() -> Self {
        Self {
            session: Mutex::new(None),
        }
    }

    pub async fn set_stream(&self, host: impl Into<String>, port: u16) {
        let mut session_guard = self.session.lock().await;
        if let Some(previous_session) = session_guard.take() {
            previous_session.stop().await;
        }

        let (handle, mut event_rx) = SnapcastClientHandle::spawn(host.into(), port);
        let (format_tx, format_rx) = std_mpsc::channel::<SampleFormat>();

        tokio::spawn(async move {
            while let Some(event) = event_rx.recv().await {
                if let ClientEvent::StreamStarted { format, .. } = event
                    && format_tx.send(format).is_err()
                {
                    break;
                }
            }
        });

        let snapcast = Arc::new(handle);
        let stop_flag = Arc::new(AtomicBool::new(false));
        let output_thread =
            spawn_cpal_worker(Arc::clone(&snapcast), format_rx, Arc::clone(&stop_flag));

        *session_guard = Some(ActiveSession {
            snapcast,
            stop_flag,
            output_thread: Some(output_thread),
        });
    }
}

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
                if let Ok(format) = format_rx.recv_timeout(FORMAT_POLL_INTERVAL) {
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

    let stream_config = cpal::StreamConfig {
        channels: format.channels(),
        sample_rate: cpal::SampleRate(format.rate()),
        buffer_size: cpal::BufferSize::Default,
    };

    let callback_snapcast = Arc::clone(snapcast);
    let stream = device
        .build_output_stream(
            &stream_config,
            move |output: &mut [f32], callback_info: &cpal::OutputCallbackInfo| {
                let dac_latency = dac_delay_usec(callback_info);
                callback_snapcast.pull_synced_f32(output, dac_latency);
            },
            |err| eprintln!("[amp-audio] cpal stream error: {err}"),
            None,
        )
        .ok()?;

    stream.play().ok()?;
    Some(stream)
}

fn dac_delay_usec(callback_info: &cpal::OutputCallbackInfo) -> i64 {
    let timestamps = callback_info.timestamp();
    timestamps
        .playback
        .duration_since(&timestamps.callback)
        .map_or(0, |duration| duration.as_micros() as i64)
}
