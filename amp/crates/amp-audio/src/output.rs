use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};

use std::sync::{Arc, Mutex};

use crate::snapcast::SnapcastClientHandle;

pub type PlaybackSource = Arc<Mutex<Option<Arc<SnapcastClientHandle>>>>;

pub fn start_output(slot: PlaybackSource) -> Option<cpal::Stream> {
    let device = cpal::default_host().default_output_device()?;
    let config = device.default_output_config().ok()?;
    println!("[Audio] Output: {:?}", config.config());

    let stream_config: cpal::StreamConfig = config.into();
    let stream = device
        .build_output_stream(
            &stream_config,
            move |data: &mut [f32], callback_info: &cpal::OutputCallbackInfo| {
                if let Ok(guard) = slot.try_lock()
                    && let Some(client) = guard.as_ref()
                {
                    client.pull_synced_f32(data, dac_latency_usec(callback_info));
                    return;
                }
                data.fill(0.0);
            },
            |err| eprintln!("[Audio] cpal stream error: {err}"),
            None,
        )
        .ok()?;

    stream.play().ok()?;
    Some(stream)
}

fn dac_latency_usec(callback_info: &cpal::OutputCallbackInfo) -> i64 {
    let timestamps = callback_info.timestamp();
    timestamps
        .playback
        .duration_since(&timestamps.callback)
        .map_or(0, |duration| duration.as_micros() as i64)
}
