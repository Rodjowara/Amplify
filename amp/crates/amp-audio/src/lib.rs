use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Arc, Mutex as StdMutex};
use std::thread;
use std::time::Duration;

use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use snapcast_client::stream::{SampleEncoding, Stream};
use snapcast_client::time_provider::TimeProvider;
use snapcast_client::{AudioFrame, ClientCommand, ClientConfig, ClientEvent, SnapClient};
use tokio::sync::{Mutex, mpsc};

#[derive(Default)]
pub struct Player {
    session: Mutex<Option<Session>>,
}

struct Session {
    commands: mpsc::Sender<ClientCommand>,
    client: tokio::task::JoinHandle<()>,
    stop: Arc<AtomicBool>,
    output: thread::JoinHandle<()>,
    // Never read, but dropping them would close the client's channels.
    _events: mpsc::Receiver<ClientEvent>,
    _audio: mpsc::Receiver<AudioFrame>,
}

impl Player {
    pub async fn set_stream(&self, host: String, port: u16) {
        let mut session = self.session.lock().await;
        if let Some(old) = session.take() {
            old.stop().await;
        }

        let (mut client, _events, _audio) = SnapClient::new(ClientConfig {
            host,
            port,
            client_name: "Amplify-Amp".into(),
            instance: 1,
            ..ClientConfig::default()
        });
        let commands = client.command_sender();
        let time = Arc::clone(&client.time_provider);
        let stream = Arc::clone(&client.stream);
        let client = tokio::spawn(async move {
            let _ = client.run().await;
        });
        let stop = Arc::new(AtomicBool::new(false));
        let output = spawn_output(time, stream, Arc::clone(&stop));

        *session = Some(Session { commands, client, stop, output, _events, _audio });
    }
}

impl Session {
    async fn stop(self) {
        self.stop.store(true, Ordering::Relaxed);
        let _ = self.output.join();
        let _ = self.commands.send(ClientCommand::Stop).await;
        let _ = self.client.await;
    }
}

// cpal::Stream is !Send, so it lives on its own thread until stopped.
fn spawn_output(
    time: Arc<StdMutex<TimeProvider>>,
    stream: Arc<StdMutex<Stream>>,
    stop: Arc<AtomicBool>,
) -> thread::JoinHandle<()> {
    thread::spawn(move || {
        let Some(device) = cpal::default_host().default_output_device() else { return };
        let Ok(config) = device.default_output_config() else { return };
        let Ok(output) = device.build_output_stream(
            &config.into(),
            move |data: &mut [f32], _: &cpal::OutputCallbackInfo| fill(&time, &stream, data),
            |err| eprintln!("cpal stream error: {err}"),
            None,
        ) else {
            return;
        };
        if output.play().is_err() {
            return;
        }
        while !stop.load(Ordering::Relaxed) {
            thread::sleep(Duration::from_millis(50));
        }
    })
}

fn fill(time: &StdMutex<TimeProvider>, stream: &StdMutex<Stream>, out: &mut [f32]) {
    let diff = time.lock().unwrap_or_else(|e| e.into_inner()).diff_to_server_usec();
    let server_now = snapcast_proto::time::now_usec() + diff;

    let mut stream = stream.lock().unwrap_or_else(|e| e.into_inner());
    let format = stream.format();
    let frames = out.len() / format.channels().max(1) as usize;
    let frame_size = format.frame_size() as usize;
    if frames == 0 || frame_size == 0 {
        return out.fill(0.0);
    }

    let mut raw = vec![0u8; frames * frame_size];
    stream.get_player_chunk_or_silence(server_now, 0, &mut raw, frames as u32);
    decode(stream.encoding(), format.bits(), &raw, out);
}

fn decode(encoding: SampleEncoding, bits: u16, raw: &[u8], out: &mut [f32]) {
    let (width, sample): (usize, fn(&[u8]) -> f32) = match (encoding, bits) {
        (SampleEncoding::Float32, _) => (4, |b| f32::from_le_bytes(b.try_into().unwrap())),
        (SampleEncoding::PcmInt, 16) => {
            (2, |b| i16::from_le_bytes(b.try_into().unwrap()) as f32 / i16::MAX as f32)
        }
        (SampleEncoding::PcmInt, 24) => (4, |b| {
            i32::from_le_bytes(b.try_into().unwrap()) as f32 / snapcast_proto::PCM_24BIT_MAX
        }),
        (SampleEncoding::PcmInt, 32) => {
            (4, |b| i32::from_le_bytes(b.try_into().unwrap()) as f32 / i32::MAX as f32)
        }
        _ => return out.fill(0.0),
    };
    for (out, bytes) in out.iter_mut().zip(raw.chunks_exact(width)) {
        *out = sample(bytes);
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use snapcast_server::{ServerConfig, SnapServer};
    use tokio::net::TcpListener;

    fn decode_one(encoding: SampleEncoding, bits: u16, raw: &[u8]) -> f32 {
        let mut out = [f32::NAN];
        decode(encoding, bits, raw, &mut out);
        out[0]
    }

    #[test]
    fn decodes_pcm_16() {
        let pcm = |v: i16| decode_one(SampleEncoding::PcmInt, 16, &v.to_le_bytes());
        assert_eq!(pcm(i16::MAX), 1.0);
        assert_eq!(pcm(0), 0.0);
        assert!((pcm(i16::MIN) + 1.0).abs() < 1e-4);
    }

    #[test]
    fn decodes_pcm_24_and_32() {
        let pcm = |bits, v: i32| decode_one(SampleEncoding::PcmInt, bits, &v.to_le_bytes());
        assert_eq!(pcm(24, 8_388_607), 1.0);
        assert_eq!(pcm(24, -8_388_607), -1.0);
        assert_eq!(pcm(32, i32::MAX), 1.0);
        assert_eq!(pcm(32, 0), 0.0);
    }

    #[test]
    fn decodes_float_32() {
        assert_eq!(decode_one(SampleEncoding::Float32, 32, &0.5f32.to_le_bytes()), 0.5);
    }

    #[test]
    fn unsupported_bit_depth_is_silence() {
        let mut out = [1.0; 4];
        decode(SampleEncoding::PcmInt, 8, &[0x7f; 4], &mut out);
        assert_eq!(out, [0.0; 4]);
    }

    #[tokio::test(flavor = "multi_thread")]
    async fn plays_tone_from_snapserver() {
        let listener = TcpListener::bind("127.0.0.1:0").await.unwrap();
        let port = listener.local_addr().unwrap().port();
        let (mut server, _server_events) =
            SnapServer::new(ServerConfig { buffer_ms: 200, ..ServerConfig::default() });
        let mut sender = server.add_f32_stream("default").unwrap();
        tokio::spawn(async move { server.serve(listener).await });

        // 440 Hz stereo tone at amplitude 0.2, sent in real time as 20 ms chunks.
        tokio::spawn(async move {
            let mut tick = tokio::time::interval(Duration::from_millis(20));
            for t in (0u64..).step_by(960) {
                tick.tick().await;
                let chunk: Vec<f32> = (t..t + 960)
                    .map(|i| 0.2 * (std::f32::consts::TAU * 440.0 * i as f32 / 48_000.0).sin())
                    .flat_map(|s| [s, s])
                    .collect();
                if sender.send(&chunk).await.is_err() {
                    break;
                }
            }
        });

        let (mut client, _events, _audio) = SnapClient::new(ClientConfig {
            host: "127.0.0.1".into(),
            port,
            ..ClientConfig::default()
        });
        let time = Arc::clone(&client.time_provider);
        let stream = Arc::clone(&client.stream);
        tokio::spawn(async move { client.run().await });

        let mut out = vec![0.0f32; 960 * 2];
        let mut peak = 0.0f32;
        for _ in 0..100 {
            tokio::time::sleep(Duration::from_millis(50)).await;
            fill(&time, &stream, &mut out);
            peak = out.iter().fold(0.0, |m, s| m.max(s.abs()));
            if peak > 0.0 {
                break;
            }
        }
        assert!((peak - 0.2).abs() < 0.05, "expected a 0.2 peak tone, got {peak}");
    }
}
