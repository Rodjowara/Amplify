use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use crate::session::PlayerSlot;

const DAC_LATENCY_USEC: i64 = 0;

pub fn start_output(slot: PlayerSlot) -> Option<cpal::Stream>{
    let device = cpal::default_host().default_output_device()?;
    let config = device.default_output_config().ok()?;
    println!("[Audio] Output: {:?}", config.config());

    let stream_config:cpal::StreamConfig = config.into();
    let stream = device.build_output_stream(
        &stream_config,
        move |data: &mut[f32], _: &cpal::OutputCallbackInfo|{
            if let Ok(guard) = slot.try_lock(){
                if let Some(synced) = guard.as_ref(){
                    synced.pull_synced_f32(data, DAC_LATENCY_USEC);
                    return;
                }
            }
            data.fill(0.0);
        },
        |err| eprintln!("[Audio] cpal stream error: {err}"),
        None,
    ).ok()?;

    stream.play().ok()?;
    Some(stream)
}