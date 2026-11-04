//! Local Snapcast demo for Amplify (`amp-audio`).
//!
//! Usage:
//!   cargo run -p amp-audio --example snapcast_demo
//!
//! What this demo does:
//! 1. Checks if an external `snapserver` is already listening on `127.0.0.1:1704` (e.g. in WSL2).
//! 2. If port 1704 is free, automatically starts an embedded `SnapServer` in the background
//!    (using `snapcast-server`, encoding FLAC at 48 kHz stereo) that streams a stereo chord
//!    (440 Hz on the Left channel, 554.37 Hz on the Right channel).
//! 3. Connects `SnapcastClientHandle` (`amp_audio::snapcast`) to `127.0.0.1:1704`.
//! 4. Logs all Snapcast protocol events (`Connected`, `ServerSettings`, `StreamStarted`,
//!    `TimeSyncComplete`) and outputs the received audio to the default speaker via `cpal`.

use std::collections::VecDeque;
use std::f32::consts::TAU;
use std::sync::{Arc, Mutex};
use std::time::Duration;

use amp_audio::snapcast::{SnapcastClientHandle, SnapcastStreamConfig};
use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use snapcast_client::ClientEvent;
use snapcast_server::{ServerConfig, ServerEvent, SnapServer};
use tokio::net::TcpListener;

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    println!("=== Amplify: Local Snapcast Demo (amp-audio) ===");

    let addr = "127.0.0.1:1704";

    // 1. If no external snapserver is bound to 127.0.0.1:1704, start a local test server
    if let Ok(listener) = TcpListener::bind(addr).await {
        println!(
            "[Server] Port 1704 is free -> Starting local SnapServer on {addr} (FLAC 48000:16:2)..."
        );
        spawn_test_snapserver(listener);
    } else {
        println!("[Server] Detected external snapserver on {addr}, connecting to it...");
    }

    // 2. Spawn the Snapcast client from our `amp-audio` crate
    let mut handle = SnapcastClientHandle::spawn(SnapcastStreamConfig {
        host: "127.0.0.1".to_string(),
        port: 1704,
        client_name: "Stage 1 Left (Local Test)".to_string(),
        instance: 1,
    });

    // Simple ring buffer to feed the sound card (`cpal`) from `audio_rx`
    let ring_buf: Arc<Mutex<VecDeque<f32>>> =
        Arc::new(Mutex::new(VecDeque::with_capacity(48_000 * 2)));

    // 3. Open the default audio output device via `cpal`
    let _cpal_stream = start_cpal_output(Arc::clone(&ring_buf));

    // 4. Receive events and audio frames for 8 seconds
    let mut total_frames_received = 0usize;
    let timeout = tokio::time::sleep(Duration::from_secs(8));
    tokio::pin!(timeout);

    loop {
        tokio::select! {
            _ = &mut timeout => {
                println!("\n[Demo] 8-second test completed. Total chunks received: {total_frames_received}");
                break;
            }
            Some(ev) = handle.event_rx.recv() => {
                match ev {
                    ClientEvent::Connected { host, port } => {
                        println!("[Client] Connected to snapserver at {host}:{port}");
                    }
                    ClientEvent::ServerSettings { buffer_ms, latency, volume, muted } => {
                        println!(
                            "[Client] ServerSettings -> buffer: {buffer_ms}ms, latency: {latency}ms, vol: {volume}%, muted: {muted}"
                        );
                    }
                    ClientEvent::StreamStarted { codec, format } => {
                        println!(
                            "[Client] StreamStarted  -> codec: {codec}, format: {}Hz / {}bit / {}ch",
                            format.rate(),
                            format.bits(),
                            format.channels()
                        );
                    }
                    ClientEvent::TimeSyncComplete { diff_ms } => {
                        println!("[Client] TimeSyncComplete -> clock offset to server: {diff_ms:.2} ms");
                    }
                    ClientEvent::VolumeChanged { volume, muted } => {
                        println!("[Client] VolumeChanged -> vol: {volume}%, muted: {muted}");
                    }
                    ClientEvent::Disconnected { reason } => {
                        println!("[Client] Disconnected: {reason}");
                    }
                }
            }
            Some(frame) = handle.audio_rx.recv() => {
                total_frames_received += 1;
                if total_frames_received == 1 || total_frames_received % 50 == 0 {
                    // Compute RMS level of the chunk to verify signal presence
                    let sum_sq: f32 = frame.samples.iter().map(|s| s * s).sum();
                    let rms = (sum_sq / frame.samples.len().max(1) as f32).sqrt();
                    println!(
                        "[Audio] Chunk #{total_frames_received:<4} | {} samples ({}Hz {}ch) | timestamp: {} us | RMS: {:.3}",
                        frame.samples.len(),
                        frame.sample_rate,
                        frame.channels,
                        frame.timestamp_usec,
                        rms
                    );
                }
                if let Ok(mut buf) = ring_buf.lock() {
                    // Cap queue at ~1 second of stereo samples
                    if buf.len() < 48_000 * 2 {
                        buf.extend(frame.samples);
                    }
                }
            }
        }
    }

    handle.stop().await;
    Ok(())
}

/// Spawns a background `SnapServer` that generates a stereo sine wave
/// (440 Hz on Left, 554.37 Hz on Right) at 48 kHz.
fn spawn_test_snapserver(listener: TcpListener) {
    let config = ServerConfig {
        buffer_ms: 400, // 400ms buffer for faster startup in demo mode
        ..ServerConfig::default()
    };
    let (mut server, mut events) = SnapServer::new(config);
    let mut f32_sender = server
        .add_f32_stream("default")
        .expect("valid sample format for f32 stream");

    // Log server events
    tokio::spawn(async move {
        while let Some(event) = events.recv().await {
            if let ServerEvent::ClientConnected { id, hello } = event {
                println!(
                    "[Server] Client connected: id={id}, name='{}', host='{}' ({}/{})",
                    hello.client_name, hello.host_name, hello.os, hello.arch
                );
            }
        }
    });

    // Audio generator: 20ms chunks (960 stereo frames at 48 kHz)
    tokio::spawn(async move {
        let sample_rate = 48_000.0f32;
        let frames_per_chunk = 960usize;
        let mut phase_l = 0.0f32;
        let mut phase_r = 0.0f32;
        let freq_l = 440.0f32; // A4 (left channel)
        let freq_r = 554.37f32; // C#5 (right channel)

        loop {
            let mut chunk = Vec::with_capacity(frames_per_chunk * 2);
            for _ in 0..frames_per_chunk {
                chunk.push(phase_l.sin() * 0.2); // L
                chunk.push(phase_r.sin() * 0.2); // R
                phase_l = (phase_l + TAU * freq_l / sample_rate) % TAU;
                phase_r = (phase_r + TAU * freq_r / sample_rate) % TAU;
            }
            if f32_sender.send(&chunk).await.is_err() {
                break;
            }
        }
    });

    tokio::spawn(async move {
        let _ = server.serve(listener).await;
    });
}

/// Opens the default output audio device via `cpal` (if a sound card is available).
fn start_cpal_output(ring_buf: Arc<Mutex<VecDeque<f32>>>) -> Option<cpal::Stream> {
    let host = cpal::default_host();
    let device = match host.default_output_device() {
        Some(d) => d,
        None => {
            println!("[Audio] Warning: No default audio output device detected (headless mode).");
            return None;
        }
    };

    let dev_name = device.name().unwrap_or_else(|_| "Unknown".into());
    let config = match device.default_output_config() {
        Ok(c) => c,
        Err(e) => {
            println!("[Audio] Could not query default output config for '{dev_name}': {e}");
            return None;
        }
    };

    println!(
        "[Audio] Active output device: '{dev_name}' ({:?})",
        config.config()
    );

    let stream_config: cpal::StreamConfig = config.into();
    let stream = device
        .build_output_stream(
            &stream_config,
            move |data: &mut [f32], _: &cpal::OutputCallbackInfo| {
                if let Ok(mut buf) = ring_buf.lock() {
                    for sample in data.iter_mut() {
                        *sample = buf.pop_front().unwrap_or(0.0);
                    }
                } else {
                    data.fill(0.0);
                }
            },
            |err| eprintln!("[Audio] cpal stream error: {err}"),
            None,
        )
        .ok()?;

    stream.play().ok()?;
    Some(stream)
}
