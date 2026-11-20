use std::net::{Ipv4Addr, SocketAddr};

use amp_core::state::SharedState;
use amp_audio::output::start_output;
use amp_audio::session::PlayerSlot;

const CONTROL_PORT: u16 = 50051;

fn main() {
    let state = SharedState::default();
    let slot = PlayerSlot::default();

    let _audio = start_output(slot.clone());

    let addr = SocketAddr::from((Ipv4Addr::UNSPECIFIED, CONTROL_PORT));
    amp_control::server::spawn(addr, state.clone(), slot);

    // Blocks on the main thread until the UI is closed.
    amp_ui::run(state);
}