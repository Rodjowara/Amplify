use std::sync::{Arc, Mutex};
use std::time::Duration;

use snapcast_client::{
    ClientCommand, ClientConfig, ClientEvent, SnapClient,
    stream::{SampleEncoding, Stream},
    time_provider::TimeProvider,
};
use tokio::sync::mpsc;
use tokio::task::JoinHandle;

const PCM16_BITS: u16 = 16;
const LOOKAHEAD_FRAMES: usize = 2;

#[derive(Default)]
struct Resampler {
    prev_frame: Vec<f32>,
    curr_frame: Vec<f32>,
    phase: f64,
}

impl Resampler {
    fn input_frames_needed(
        &mut self,
        output_frames: usize,
        stream_rate: u32,
        output_rate: u32,
        stream_channels: usize,
    ) -> usize {
        if self.prev_frame.len() != stream_channels {
            self.prev_frame = vec![0.0; stream_channels];
            self.curr_frame = vec![0.0; stream_channels];
            self.phase = 0.0;
        }
        let ratio = stream_rate as f64 / output_rate as f64;
        (self.phase + output_frames as f64 * ratio).floor() as usize
    }

    fn resample_into(
        &mut self,
        input_samples: &[f32],
        stream_channels: usize,
        stream_rate: u32,
        output: &mut [f32],
        output_channels: usize,
        output_rate: u32,
    ) {
        let output_frames = output.len() / output_channels;
        let input_frames = input_samples.len() / stream_channels;
        let ratio = stream_rate as f64 / output_rate as f64;

        let mut source = Vec::with_capacity((input_frames + LOOKAHEAD_FRAMES) * stream_channels);
        source.extend_from_slice(&self.prev_frame);
        source.extend_from_slice(&self.curr_frame);
        source.extend_from_slice(input_samples);

        for (out_idx, out_frame) in output.chunks_exact_mut(output_channels).enumerate() {
            let position = self.phase + out_idx as f64 * ratio;
            let frame_idx = (position.floor() as usize).min(input_frames);
            let frac = (position - frame_idx as f64) as f32;
            let first = &source[frame_idx * stream_channels..(frame_idx + 1) * stream_channels];
            let second =
                &source[(frame_idx + 1) * stream_channels..(frame_idx + 2) * stream_channels];
            write_interpolated_frame(first, second, frac, out_frame);
        }

        let end_position = self.phase + output_frames as f64 * ratio;
        self.phase = end_position - input_frames as f64;
        let next_prev_start = input_frames * stream_channels;
        let next_curr_start = (input_frames + 1) * stream_channels;
        self.prev_frame
            .copy_from_slice(&source[next_prev_start..next_curr_start]);
        self.curr_frame
            .copy_from_slice(&source[next_curr_start..next_curr_start + stream_channels]);
    }
}

pub struct SnapcastConnection {
    command_tx: mpsc::Sender<ClientCommand>,
    time_provider: Arc<Mutex<TimeProvider>>,
    stream: Arc<Mutex<Stream>>,
    resampler: Resampler,
    task: JoinHandle<()>,
}

impl SnapcastConnection {
    pub async fn connect(host: String, port: u16, timeout: Duration) -> Result<Self, String> {
        let (handle, mut event_rx) = Self::spawn(host, port);

        let wait_for_connect = async {
            while let Some(event) = event_rx.recv().await {
                match event {
                    ClientEvent::Connected { .. } => return Ok(()),
                    ClientEvent::Disconnected { reason } => {
                        return Err(format!("disconnected: {reason}"));
                    }
                    _ => {}
                }
            }
            Err("client stopped before connecting".to_string())
        };

        let result = tokio::time::timeout(timeout, wait_for_connect)
            .await
            .unwrap_or_else(|_| Err("timed out connecting to the snapserver".into()));
        if let Err(error) = result {
            handle.stop().await;
            return Err(error);
        }
        Ok(handle)
    }

    fn spawn(host: String, port: u16) -> (Self, mpsc::Receiver<ClientEvent>) {
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
                resampler: Resampler::default(),
                task,
            },
            event_rx,
        )
    }

    pub(crate) fn pull_synced_f32(
        &mut self,
        output: &mut [f32],
        dac_latency_usec: i64,
        output_sample_rate: u32,
        output_channels: u16,
    ) {
        let output_channels = output_channels as usize;
        if output_sample_rate == 0 || output_channels == 0 {
            output.fill(0.0);
            return;
        }
        let output_frames = output.len() / output_channels;
        if output_frames == 0 {
            output.fill(0.0);
            return;
        }

        let server_offset_usec = self
            .time_provider
            .lock()
            .expect("time provider lock poisoned")
            .diff_to_server_usec();
        let server_now_usec = snapcast_proto::time::now_usec() + server_offset_usec;

        let mut stream = self.stream.lock().expect("stream lock poisoned");
        let format = stream.format();
        let stream_rate = format.rate();
        let stream_channels = format.channels() as usize;
        let bytes_per_frame = format.frame_size() as usize;

        if stream_rate == 0 || stream_channels == 0 || bytes_per_frame == 0 {
            output.fill(0.0);
            return;
        }

        let encoding = stream.encoding();
        let bits = format.bits();

        if stream_rate == output_sample_rate && stream_channels == output_channels {
            let mut raw_bytes = vec![0u8; output_frames * bytes_per_frame];
            stream.get_player_chunk_or_silence(
                server_now_usec,
                dac_latency_usec,
                &mut raw_bytes,
                output_frames as u32,
            );
            decode_pcm_to_f32(&raw_bytes, encoding, bits, output);
            return;
        }

        let input_frames = self.resampler.input_frames_needed(
            output_frames,
            stream_rate,
            output_sample_rate,
            stream_channels,
        );
        let mut input_samples = vec![0.0; input_frames * stream_channels];
        if input_frames > 0 {
            let mut raw_bytes = vec![0u8; input_frames * bytes_per_frame];
            stream.get_player_chunk_or_silence(
                server_now_usec,
                dac_latency_usec,
                &mut raw_bytes,
                input_frames as u32,
            );
            decode_pcm_to_f32(&raw_bytes, encoding, bits, &mut input_samples);
        }

        self.resampler.resample_into(
            &input_samples,
            stream_channels,
            stream_rate,
            output,
            output_channels,
            output_sample_rate,
        );
    }

    pub async fn stop(&self) {
        let _ = self.command_tx.send(ClientCommand::Stop).await;
        self.task.abort();
    }
}

fn decode_pcm_to_f32(raw_bytes: &[u8], encoding: SampleEncoding, bits: u16, output: &mut [f32]) {
    match (encoding, bits) {
        (SampleEncoding::Float32, _) => {
            let (chunks, _) = raw_bytes.as_chunks::<{ size_of::<f32>() }>();
            for (out_sample, chunk) in output.iter_mut().zip(chunks) {
                *out_sample = f32::from_le_bytes(*chunk);
            }
        }
        (SampleEncoding::PcmInt, PCM16_BITS) => {
            let (chunks, _) = raw_bytes.as_chunks::<{ size_of::<i16>() }>();
            for (out_sample, chunk) in output.iter_mut().zip(chunks) {
                *out_sample = i16::from_le_bytes(*chunk) as f32 / i16::MAX as f32;
            }
        }
        _ => output.fill(0.0),
    }
}

fn write_interpolated_frame(first: &[f32], second: &[f32], frac: f32, out_frame: &mut [f32]) {
    if out_frame.len() == 1 && first.len() > 1 {
        let sum: f32 = first
            .iter()
            .zip(second)
            .map(|(&a, &b)| a + (b - a) * frac)
            .sum();
        out_frame[0] = sum / first.len() as f32;
        return;
    }
    if first.len() == 1 {
        let sample = first[0] + (second[0] - first[0]) * frac;
        out_frame.fill(sample);
        return;
    }
    for (channel, out_sample) in out_frame.iter_mut().enumerate() {
        *out_sample = match (first.get(channel), second.get(channel)) {
            (Some(&a), Some(&b)) => a + (b - a) * frac,
            _ => 0.0,
        };
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use snapcast_server::{ServerConfig, SnapServer};
    use tokio::net::TcpListener;

    const TEST_CHANNELS: usize = 2;
    const TEST_CHUNK_FRAMES: usize = 960;
    const TEST_SAMPLE_VALUE: f32 = 0.25;
    const TEST_TIMEOUT: Duration = Duration::from_secs(3);
    const POLL_INTERVAL: Duration = Duration::from_millis(20);
    const RATE_48K: u32 = 48_000;
    const RATE_44K1: u32 = 44_100;
    const RATE_96K: u32 = 96_000;
    const CALLBACKS_PER_SECOND: usize = 100;
    const FRAMES_PER_10MS_AT_44K1: usize = 441;
    const FLOAT_EPSILON: f32 = 1e-4;

    #[tokio::test]
    async fn client_connects_and_buffers_audio_chunks_from_server() {
        let listener = TcpListener::bind("127.0.0.1:0").await.unwrap();
        let port = listener.local_addr().unwrap().port();

        let (mut server, _events) = SnapServer::new(ServerConfig::default());
        let mut sender = server.add_f32_stream("default").unwrap();

        tokio::spawn(async move {
            let _ = server.serve(listener).await;
        });

        let (client, _event_rx) = SnapcastConnection::spawn("127.0.0.1".into(), port);

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

    #[test]
    fn resampler_downsamples_48khz_to_44100hz_without_drift() {
        let mut resampler = Resampler::default();
        let mut total_input_frames = 0;
        let mut output = vec![0.0; FRAMES_PER_10MS_AT_44K1 * TEST_CHANNELS];

        for callback_idx in 0..CALLBACKS_PER_SECOND {
            let needed = resampler.input_frames_needed(
                FRAMES_PER_10MS_AT_44K1,
                RATE_48K,
                RATE_44K1,
                TEST_CHANNELS,
            );
            total_input_frames += needed;
            let input = vec![TEST_SAMPLE_VALUE; needed * TEST_CHANNELS];
            resampler.resample_into(
                &input,
                TEST_CHANNELS,
                RATE_48K,
                &mut output,
                TEST_CHANNELS,
                RATE_44K1,
            );
            if callback_idx > 0 {
                for &sample in &output {
                    assert!((sample - TEST_SAMPLE_VALUE).abs() < FLOAT_EPSILON);
                }
            }
        }

        assert_eq!(total_input_frames, RATE_48K as usize);
    }

    #[test]
    fn resampler_upsamples_48khz_mono_to_96khz_stereo() {
        let mut resampler = Resampler::default();
        let mono_channels = 1;
        let output_frames = 200;
        let needed =
            resampler.input_frames_needed(output_frames, RATE_48K, RATE_96K, mono_channels);
        assert_eq!(needed, 100);

        let input = vec![TEST_SAMPLE_VALUE; needed * mono_channels];
        let mut output = vec![0.0; output_frames * TEST_CHANNELS];
        resampler.resample_into(
            &input,
            mono_channels,
            RATE_48K,
            &mut output,
            TEST_CHANNELS,
            RATE_96K,
        );

        let steady_start = LOOKAHEAD_FRAMES * 2 * TEST_CHANNELS;
        for &sample in &output[steady_start..] {
            assert!((sample - TEST_SAMPLE_VALUE).abs() < FLOAT_EPSILON);
        }
    }

    #[test]
    fn resampler_downmixes_stereo_to_mono() {
        let mut resampler = Resampler::default();
        let mono_channels = 1;
        let output_frames = 10;
        let needed =
            resampler.input_frames_needed(output_frames, RATE_48K, RATE_48K, TEST_CHANNELS);
        assert_eq!(needed, output_frames);

        let mut input = Vec::with_capacity(needed * TEST_CHANNELS);
        for _ in 0..needed {
            input.push(0.2);
            input.push(0.6);
        }
        let mut output = vec![0.0; output_frames * mono_channels];
        resampler.resample_into(
            &input,
            TEST_CHANNELS,
            RATE_48K,
            &mut output,
            mono_channels,
            RATE_48K,
        );

        for &sample in &output[LOOKAHEAD_FRAMES..] {
            assert!((sample - 0.4).abs() < FLOAT_EPSILON);
        }
    }
}
