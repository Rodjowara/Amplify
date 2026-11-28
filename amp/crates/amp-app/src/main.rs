use std::net::{Ipv4Addr, SocketAddr};

use amp_core::CONTROL_PORT;
use amp_core::state::SharedState;
use amp_audio::output::start_output;
use amp_audio::output::PlaybackSource;

fn main() {
    let state = SharedState::default();
    let slot = PlaybackSource::default();

    let _output_stream = start_output(slot.clone());

    let addr = SocketAddr::from((Ipv4Addr::UNSPECIFIED, CONTROL_PORT));
    amp_control::server::spawn(addr, state.clone(), slot);

    match local_ip_address::local_ip() {
        Ok(ip) => println!("Control server listening on {}:{}", ip, CONTROL_PORT),
        Err(e) => eprintln!("Failed to get local IP address: {}", e),
    }

    // Blocks on the main thread until the UI is closed.
    amp_ui::run(state);
}