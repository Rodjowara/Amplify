use std::sync::{Arc, Mutex};

use snapcast_client::{
    ClientCommand, ClientConfig, ClientEvent, SnapClient,
    stream::{SampleEncoding, Stream},
    time_provider::TimeProvider,
};
use tokio::sync::mpsc;
use tokio::task::JoinHandle;

const BITS_PER_SAMPLE_16: u16 = 16;
const BYTES_PER_SAMPLE_16: usize = 2;
const BYTES_PER_SAMPLE_32: usize = 4;

pub struct SnapcastClientHandle {
    command_tx: mpsc::Sender<ClientCommand>,
    time_provider: Arc<Mutex<TimeProvider>>,
    stream: Arc<Mutex<Stream>>,
    task: JoinHandle<()>,
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
                task,
            },
            event_rx,
        )
    }

    pub(crate) fn pull_synced_f32(&self, output: &mut [f32], dac_latency_usec: i64) {
        let diff_usec = self.time_provider.lock().unwrap().diff_to_server_usec();
        let server_now_usec = snapcast_proto::time::now_usec() + diff_usec;

        let mut stream = self.stream.lock().unwrap();
        let format = stream.format();
        let channels = format.channels().max(1) as usize;
        let frames = (output.len() / channels) as u32;
        let frame_size = format.frame_size() as usize;

        if frames == 0 || frame_size == 0 {
            output.fill(0.0);
            return;
        }

        let mut raw_bytes = vec![0u8; frames as usize * frame_size];
        stream.get_player_chunk_or_silence(
            server_now_usec,
            dac_latency_usec,
            &mut raw_bytes,
            frames,
        );

        match (stream.encoding(), format.bits()) {
            (SampleEncoding::Float32, _) => {
                let (chunks, _) = raw_bytes.as_chunks::<BYTES_PER_SAMPLE_32>();
                for (out_sample, chunk) in output.iter_mut().zip(chunks) {
                    *out_sample = f32::from_le_bytes(*chunk);
                }
            }
            (SampleEncoding::PcmInt, BITS_PER_SAMPLE_16) => {
                let (chunks, _) = raw_bytes.as_chunks::<BYTES_PER_SAMPLE_16>();
                for (out_sample, chunk) in output.iter_mut().zip(chunks) {
                    *out_sample = i16::from_le_bytes(*chunk) as f32 / i16::MAX as f32;
                }
            }
            _ => output.fill(0.0),
        }
    }

    pub(crate) async fn stop(&self) {
        let _ = self.command_tx.send(ClientCommand::Stop).await;
        self.task.abort();
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use snapcast_server::{ServerConfig, SnapServer};
    use std::time::Duration;
    use tokio::net::TcpListener;

    const TEST_CHANNELS: usize = 2;
    const TEST_CHUNK_FRAMES: usize = 960;
    const TEST_SAMPLE_VALUE: f32 = 0.25;
    const TEST_TIMEOUT: Duration = Duration::from_secs(3);
    const POLL_INTERVAL: Duration = Duration::from_millis(20);

    #[tokio::test]
    async fn client_connects_and_buffers_audio_chunks_from_server() {
        let listener = TcpListener::bind("127.0.0.1:0").await.unwrap();
        let port = listener.local_addr().unwrap().port();

        let (mut server, _events) = SnapServer::new(ServerConfig::default());
        let mut sender = server.add_f32_stream("default").unwrap();

        tokio::spawn(async move {
            let _ = server.serve(listener).await;
        });

        let (client, _event_rx) = SnapcastClientHandle::spawn("127.0.0.1".into(), port);

        tokio::time::timeout(TEST_TIMEOUT, async {
            let chunk = vec![TEST_SAMPLE_VALUE; TEST_CHUNK_FRAMES * TEST_CHANNELS];
            while client.stream.lock().unwrap().chunk_count() == 0 {
                let _ = sender.send(&chunk).await;
                tokio::time::sleep(POLL_INTERVAL).await;
            }
        })
        .await
        .expect("timed out waiting for decoded audio chunks");

        client.stop().await;
    }
}
