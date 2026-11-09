mod backend;
mod models;

use qtbridge::QApp;

pub fn run() {
    let shared_qml = concat!(env!("CARGO_MANIFEST_DIR"), "/../../../shared/qml");

    QApp::new()
        .register::<backend::Greeter>()
        .add_import_path(shared_qml)
        .load_qml(include_bytes!("../qml/main.qml"))
        .run();
}