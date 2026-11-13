use std::sync::{Arc, Mutex};

use snapcast_client::{
    ClientCommand, ClientConfig, ClientEvent, SnapClient,
    stream::{SampleEncoding, Stream},
    time_provider::TimeProvider,
};
use tokio::sync::mpsc;
use tokio::task::JoinHandle;

const BITS_PER_SAMPLE_16: u16 = 16;
const BITS_PER_SAMPLE_24: u16 = 24;
const BITS_PER_SAMPLE_32: u16 = 32;
const BYTES_PER_SAMPLE_16: usize = 2;
const BYTES_PER_SAMPLE_32: usize = 4;

pub(crate) struct SnapcastClientHandle {
    command_tx: mpsc::Sender<ClientCommand>,
    time_provider: Arc<Mutex<TimeProvider>>,
    stream: Arc<Mutex<Stream>>,
    task: Mutex<Option<JoinHandle<()>>>,
}

impl SnapcastClientHandle {
    pub(crate) fn spawn(host: String, port: u16) -> (Self, mpsc::Receiver<ClientEvent>) {
        let client_config = ClientConfig {
            host,
            port,
            ..ClientConfig::default()
        };

        let (mut client, event_rx, _audio_rx) = SnapClient::new(client_config);
        let command_tx = client.command_sender();
        let time_provider = Arc::clone(&client.time_provider);
        let stream = Arc::clone(&client.stream);

        let task = tokio::spawn(async move {
            let _ = client.run().await;
        });

        (
            Self {
                command_tx,
                time_provider,
                stream,
                task: Mutex::new(Some(task)),
            },
            event_rx,
        )
    }

    pub(crate) fn pull_synced_f32(&self, output: &mut [f32], dac_latency_usec: i64) -> bool {
        let diff_usec = self
            .time_provider
            .lock()
            .unwrap_or_else(|err| err.into_inner())
            .diff_to_server_usec();
        let server_now_usec = snapcast_proto::time::now_usec() + diff_usec;

        let mut stream = self.stream.lock().unwrap_or_else(|err| err.into_inner());
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
                let (chunks, _) = raw_bytes.as_chunks::<BYTES_PER_SAMPLE_32>();
                for (out_sample, chunk) in output.iter_mut().zip(chunks) {
                    *out_sample = f32::from_le_bytes(*chunk);
                }
            }
            SampleEncoding::PcmInt => match format.bits() {
                BITS_PER_SAMPLE_16 => {
                    let (chunks, _) = raw_bytes.as_chunks::<BYTES_PER_SAMPLE_16>();
                    for (out_sample, chunk) in output.iter_mut().zip(chunks) {
                        let sample = i16::from_le_bytes(*chunk);
                        *out_sample = sample as f32 / i16::MAX as f32;
                    }
                }
                BITS_PER_SAMPLE_24 => {
                    let (chunks, _) = raw_bytes.as_chunks::<BYTES_PER_SAMPLE_32>();
                    for (out_sample, chunk) in output.iter_mut().zip(chunks) {
                        let sample = i32::from_le_bytes(*chunk);
                        *out_sample = sample as f32 / snapcast_proto::PCM_24BIT_MAX;
                    }
                }
                BITS_PER_SAMPLE_32 => {
                    let (chunks, _) = raw_bytes.as_chunks::<BYTES_PER_SAMPLE_32>();
                    for (out_sample, chunk) in output.iter_mut().zip(chunks) {
                        let sample = i32::from_le_bytes(*chunk);
                        *out_sample = sample as f32 / i32::MAX as f32;
                    }
                }
                _ => output.fill(0.0),
            },
        }

        has_audio
    }

    pub(crate) async fn stop(&self) {
        let _ = self.command_tx.send(ClientCommand::Stop).await;
        let task = self.task.lock().unwrap_or_else(|err| err.into_inner()).take();
        if let Some(task) = task {
            let _ = task.await;
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use snapcast_server::{ServerConfig, SnapServer};
    use std::time::Duration;
    use tokio::net::TcpListener;

    const TEST_BUFFER_MS: u32 = 200;
    const TEST_CHANNELS: usize = 2;
    const TEST_CHUNK_FRAMES: usize = 960;
    const TEST_CHUNK_COUNT: usize = 20;
    const TEST_SAMPLE_VALUE: f32 = 0.25;
    const TEST_SAMPLE_RATE: u32 = 48_000;
    const TEST_TIMEOUT: Duration = Duration::from_secs(3);
    const POLL_INTERVAL: Duration = Duration::from_millis(20);

    #[tokio::test]
    async fn client_connects_and_buffers_audio_chunks_from_server() {
        let listener = TcpListener::bind("127.0.0.1:0").await.unwrap();
        let port = listener.local_addr().unwrap().port();

        let (mut server, _events) = SnapServer::new(ServerConfig {
            buffer_ms: TEST_BUFFER_MS,
            ..ServerConfig::default()
        });
        let mut sender = server.add_f32_stream("default").unwrap();

        tokio::spawn(async move {
            let _ = server.serve(listener).await;
        });

        tokio::spawn(async move {
            let chunk = vec![TEST_SAMPLE_VALUE; TEST_CHUNK_FRAMES * TEST_CHANNELS];
            for _ in 0..TEST_CHUNK_COUNT {
                if sender.send(&chunk).await.is_err() {
                    break;
                }
            }
        });

        let (client, mut event_rx) = SnapcastClientHandle::spawn("127.0.0.1".into(), port);

        let stream_format = tokio::time::timeout(TEST_TIMEOUT, async {
            while let Some(event) = event_rx.recv().await {
                if let ClientEvent::StreamStarted { format, .. } = event {
                    return format;
                }
            }
            panic!("event channel closed before StreamStarted");
        })
        .await
        .expect("timed out waiting for StreamStarted");

        assert_eq!(stream_format.rate(), TEST_SAMPLE_RATE);
        assert_eq!(stream_format.channels() as usize, TEST_CHANNELS);

        tokio::time::timeout(TEST_TIMEOUT, async {
            loop {
                let queued_chunks = client.stream.lock().unwrap().chunk_count();
                if queued_chunks > 0 {
                    break;
                }
                tokio::time::sleep(POLL_INTERVAL).await;
            }
        })
        .await
        .expect("timed out waiting for decoded audio chunks");

        client.stop().await;
    }
}
