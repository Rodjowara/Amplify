mod backend;
mod models;

use amp_core::state::SharedState;
use qtbridge::QApp;

pub fn run(_state: SharedState) {
    QApp::new()
        .register::<backend::Greeter>()
        .load_qml(include_bytes!("../qml/main.qml"))
        .run();
}
