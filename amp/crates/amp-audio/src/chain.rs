//! Audio output pipeline (`AudioPipeline`) connecting Snapcast to `cpal` (and DSP stages).

use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};
use std::thread::{self, JoinHandle};
use std::time::Duration;

use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
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

        let snapcast = Arc::new(SnapcastClientHandle::spawn(cfg));
        let stop_flag = Arc::new(AtomicBool::new(false));
        let output_thread = spawn_cpal_worker(Arc::clone(&snapcast), Arc::clone(&stop_flag));

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

/// Spawns a dedicated OS thread owning the `!Send` `cpal::Stream` and pulling
/// time-synchronized samples from [`SnapcastClientHandle::pull_synced_f32`].
fn spawn_cpal_worker(
    snapcast: Arc<SnapcastClientHandle>,
    stop_flag: Arc<AtomicBool>,
) -> JoinHandle<()> {
    thread::Builder::new()
        .name("amp-audio-cpal".into())
        .spawn(move || {
            let host = cpal::default_host();
            let Some(device) = host.default_output_device() else {
                return;
            };
            let Ok(supported_cfg) = device.default_output_config() else {
                return;
            };

            let stream_cfg: cpal::StreamConfig = supported_cfg.into();
            let Ok(stream) = device.build_output_stream(
                &stream_cfg,
                move |data: &mut [f32], _: &cpal::OutputCallbackInfo| {
                    snapcast.pull_synced_f32(data, 0);
                },
                |err| eprintln!("[amp-audio] cpal stream error: {err}"),
                None,
            ) else {
                return;
            };

            if stream.play().is_err() {
                return;
            }

            while !stop_flag.load(Ordering::Relaxed) {
                thread::sleep(Duration::from_millis(50));
            }
        })
        .expect("failed to spawn amp-audio-cpal thread")
}

