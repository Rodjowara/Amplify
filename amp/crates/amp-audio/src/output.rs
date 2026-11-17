use std::thread;
use std::time::Duration;

use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use snapcast_client::SampleFormat;

use crate::chain::dac_delay_usec;
use crate::session::PlayerSlot;

const FORMAT_POLL_INTERVAL: Duration = Duration::from_millis(50);

pub fn start_output(slot: PlayerSlot) {
    thread::Builder::new()
        .name("amp-audio-output".into())
        .spawn(move || {
            let mut current_format = None;
            let mut active_stream = None;
            loop {
                let synced = slot.lock().unwrap_or_else(|err| err.into_inner()).clone();
                let format = synced
                    .map(|synced| synced.format())
                    .filter(|format| format.rate() > 0);
                if format != current_format {
                    drop(active_stream.take());
                    active_stream = format.and_then(|format| open_stream(&slot, format));
                    current_format = format;
                }
                thread::sleep(FORMAT_POLL_INTERVAL);
            }
        })
        .expect("failed to spawn amp-audio-output thread");
}

fn open_stream(slot: &PlayerSlot, format: SampleFormat) -> Option<cpal::Stream> {
    let device = cpal::default_host().default_output_device()?;
    let stream_config = cpal::StreamConfig {
        channels: format.channels(),
        sample_rate: cpal::SampleRate(format.rate()),
        buffer_size: cpal::BufferSize::Default,
    };
    println!("[Audio] Output: {stream_config:?}");

    let slot = slot.clone();
    let stream = device
        .build_output_stream(
            &stream_config,
            move |data: &mut [f32], info: &cpal::OutputCallbackInfo| {
                if let Ok(guard) = slot.try_lock()
                    && let Some(synced) = guard.as_ref()
                {
                    synced.pull_synced_f32(data, dac_delay_usec(info));
                    return;
                }
                data.fill(0.0);
            },
            |err| eprintln!("[Audio] cpal stream error: {err}"),
            None,
        )
        .inspect_err(|err| eprintln!("[Audio] failed to open output at {format}: {err}"))
        .ok()?;

    stream.play().ok()?;
    Some(stream)
}
