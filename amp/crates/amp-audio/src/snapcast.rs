use std::sync::{Arc, Mutex};

use snapcast_client::{
    AudioFrame, ClientCommand, ClientConfig, ClientEvent, DEFAULT_STREAM_PORT, SnapClient,
    stream::{SampleEncoding, Stream},
    time_provider::TimeProvider,
};
use tokio::sync::mpsc;
use tokio::task::JoinHandle;

/// Configuration for connecting an Amp instance to Maestro's Snapcast server.
#[derive(Debug, Clone)]
pub struct SnapcastStreamConfig {
    /// Hostname or IP address of Maestro's snapserver (e.g. "127.0.0.1" or "192.168.1.10").
    pub host: String,
    /// Snapcast stream TCP port (default: 1704).
    pub port: u16,
    /// Human-readable name of this Amp instance sent in the Hello handshake.
    pub client_name: String,
    /// Instance ID when running multiple simulated Amps on the same laptop.
    pub instance: u32,
}

impl Default for SnapcastStreamConfig {
    fn default() -> Self {
        Self {
            host: "127.0.0.1".to_string(),
            port: DEFAULT_STREAM_PORT,
            client_name: "Amplify-Amp".to_string(),
            instance: 1,
        }
    }
}

/// Active handle to a running Snapcast client session.
///
/// Provides:
/// - `audio_rx`: decoded interleaved `f32` frames (`AudioFrame`) as they arrive,
/// - `event_rx`: connection, codec, volume, and clock-sync events (`ClientEvent`),
/// - `stream` & `time_provider`: shared time-synchronized buffer for sample-accurate pull from `cpal`,
/// - `command_tx`: control channel to stop the client or update volume/mute.
pub struct SnapcastClientHandle {
    pub event_rx: mpsc::Receiver<ClientEvent>,
    pub audio_rx: mpsc::Receiver<AudioFrame>,
    pub command_tx: mpsc::Sender<ClientCommand>,
    pub time_provider: Arc<Mutex<TimeProvider>>,
    pub stream: Arc<Mutex<Stream>>,
    task: Mutex<Option<JoinHandle<anyhow::Result<()>>>>,
}

impl SnapcastClientHandle {
    /// Spawn a Snapcast client in the background on the active Tokio runtime.
    pub fn spawn(cfg: SnapcastStreamConfig) -> Self {
        let client_cfg = ClientConfig {
            host: cfg.host,
            port: cfg.port,
            client_name: cfg.client_name,
            instance: cfg.instance,
            ..ClientConfig::default()
        };

        let (mut client, event_rx, audio_rx) = SnapClient::new(client_cfg);
        let command_tx = client.command_sender();
        let time_provider = Arc::clone(&client.time_provider);
        let stream = Arc::clone(&client.stream);

        let task = tokio::spawn(async move { client.run().await });

        Self {
            event_rx,
            audio_rx,
            command_tx,
            time_provider,
            stream,
            task: Mutex::new(Some(task)),
        }
    }

    /// Estimated current server timestamp in microseconds (`now_usec + diff_to_server_usec`).
    pub fn server_now_usec(&self) -> i64 {
        let diff = self
            .time_provider
            .lock()
            .unwrap_or_else(|e| e.into_inner())
            .diff_to_server_usec();
        snapcast_proto::time::now_usec() + diff
    }

    /// Read time-synchronized stereo `f32` samples for the audio output callback (`cpal`).
    ///
    /// Uses Snapcast's `TimeProvider` and `Stream::get_player_chunk_or_silence` so that
    /// multiple Amps play the exact same sample at the exact same wall-clock instant.
    pub fn pull_synced_f32(&self, output: &mut [f32], dac_latency_usec: i64) -> bool {
        let server_now_usec = self.server_now_usec();

        let mut stream = self.stream.lock().unwrap_or_else(|e| e.into_inner());
        let format = stream.format();
        let encoding = stream.encoding();
        let channels = format.channels().max(1) as usize;
        let frames = (output.len() / channels) as u32;
        let frame_size = format.frame_size() as usize;

        if frames == 0 || frame_size == 0 {
            output.fill(0.0);
            return false;
        }

        let mut raw_bytes = vec![0u8; frames as usize * frame_size];
        let has_audio = stream.get_player_chunk_or_silence(
            server_now_usec,
            dac_latency_usec,
            &mut raw_bytes,
            frames,
        );

        match encoding {
            SampleEncoding::Float32 => {
                for (out_sample, chunk) in output.iter_mut().zip(raw_bytes.chunks_exact(4)) {
                    *out_sample = f32::from_le_bytes([chunk[0], chunk[1], chunk[2], chunk[3]]);
                }
            }
            SampleEncoding::PcmInt => match format.bits() {
                16 => {
                    for (out_sample, chunk) in output.iter_mut().zip(raw_bytes.chunks_exact(2)) {
                        let s = i16::from_le_bytes([chunk[0], chunk[1]]);
                        *out_sample = s as f32 / i16::MAX as f32;
                    }
                }
                24 => {
                    for (out_sample, chunk) in output.iter_mut().zip(raw_bytes.chunks_exact(4)) {
                        let s = i32::from_le_bytes([chunk[0], chunk[1], chunk[2], chunk[3]]);
                        *out_sample = s as f32 / snapcast_proto::PCM_24BIT_MAX;
                    }
                }
                32 => {
                    for (out_sample, chunk) in output.iter_mut().zip(raw_bytes.chunks_exact(4)) {
                        let s = i32::from_le_bytes([chunk[0], chunk[1], chunk[2], chunk[3]]);
                        *out_sample = s as f32 / i32::MAX as f32;
                    }
                }
                _ => output.fill(0.0),
            },
        }

        has_audio
    }

    /// Gracefully stop the Snapcast client and wait for its background task to finish.
    pub async fn stop(&self) {
        let _ = self.command_tx.send(ClientCommand::Stop).await;
        let task = self.task.lock().unwrap_or_else(|e| e.into_inner()).take();
        if let Some(t) = task {
            let _ = t.await;
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use snapcast_server::{ServerConfig, SnapServer};
    use std::time::Duration;
    use tokio::net::TcpListener;

    #[test]
    fn default_config_uses_standard_port() {
        let cfg = SnapcastStreamConfig::default();
        assert_eq!(cfg.host, "127.0.0.1");
        assert_eq!(cfg.port, 1704);
    }

    #[tokio::test]
    async fn client_connects_and_receives_flac_chunks_from_server() {
        let listener = TcpListener::bind("127.0.0.1:0").await.unwrap();
        let port = listener.local_addr().unwrap().port();

        let (mut server, _events) = SnapServer::new(ServerConfig {
            buffer_ms: 200,
            ..ServerConfig::default()
        });
        let mut sender = server.add_f32_stream("default").unwrap();

        tokio::spawn(async move {
            let _ = server.serve(listener).await;
        });

        tokio::spawn(async move {
            let chunk = vec![0.25f32; 960 * 2];
            for _ in 0..20 {
                if sender.send(&chunk).await.is_err() {
                    break;
                }
            }
        });

        let mut client = SnapcastClientHandle::spawn(SnapcastStreamConfig {
            host: "127.0.0.1".into(),
            port,
            client_name: "Test-Amp".into(),
            instance: 1,
        });

        let frame = tokio::time::timeout(Duration::from_secs(3), client.audio_rx.recv())
            .await
            .expect("timed out waiting for audio frame")
            .expect("audio channel closed");

        assert_eq!(frame.sample_rate, 48_000);
        assert_eq!(frame.channels, 2);
        assert!(!frame.samples.is_empty());

        client.stop().await;
    }
}
