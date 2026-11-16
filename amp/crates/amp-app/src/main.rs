use amp_core::state::SharedState;
use amp_audio::output::start_output;
use amp_audio::session::PlayerSlot;

fn main() {
    let state = SharedState::default();
    let slot = PlayerSlot::default();

    let _audio = start_output(slot.clone());

    let addr = "0.0.0.0:50051".parse().expect("invalid listen address");
    amp_control::server::spawn(addr, state.clone(), slot.clone());

    // Blocks on the main thread until the UI is closed.
    amp_ui::run(state);
}