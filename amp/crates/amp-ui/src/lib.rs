mod backend;
mod models;

use qtbridge::QApp;

pub fn run() {
    QApp::new()
        .register::<backend::Greeter>()
        .load_qml(include_bytes!("../qml/main.qml"))
        .run();
}
