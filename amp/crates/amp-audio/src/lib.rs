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

    let (width, decode): (usize, fn(&[u8]) -> f32) = match (stream.encoding(), format.bits()) {
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
    for (sample, bytes) in out.iter_mut().zip(raw.chunks_exact(width)) {
        *sample = decode(bytes);
    }
}
