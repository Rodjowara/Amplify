mod backend;
mod models;

use std::path::PathBuf;
use amp_core::state::SharedState;
use qtbridge::QApp;

include!(concat!(env!("OUT_DIR"), "/embedded_qml.rs"));

// Embedded so the binary runs alone (e.g. on the Pi); Qt imports the modules from this temp folder
fn extract_qml(files: &[(&str, &[u8])], folder_name: &str) -> std::io::Result<PathBuf> {
    let dir = std::env::temp_dir().join(folder_name);
    for (relative_path, bytes) in files {
        let path = dir.join(relative_path);
        if let Some(parent) = path.parent() {
            std::fs::create_dir_all(parent)?;
        }
        std::fs::write(&path, bytes)?;
    }
    Ok(dir)
}

pub fn run(_state: SharedState) {
    // let shared_qml = concat!(env!("CARGO_MANIFEST_DIR"), "/../../../shared/qml");
    let shared_qml = extract_qml(SHARED_QML_FILES, "amplify-shared-qml")
        .expect("failed to extract the shared QML files");
    let amp_qml = extract_qml(AMP_QML_FILES, "amplify-amp-qml")
        .expect("failed to extract the amp QML files");

    QApp::new()
        .register::<backend::Greeter>()
        //.add_import_path(shared_qml)
        .add_import_path(&shared_qml.to_string_lossy())
        .add_import_path(&amp_qml.to_string_lossy())
        .load_qml(include_bytes!("../qml/main.qml"))
        .run();
}
