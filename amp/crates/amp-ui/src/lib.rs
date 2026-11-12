mod backend;
mod models;

use std::path::PathBuf;
use qtbridge::QApp;

// Files of shared/qml, embedded at build time by build.rs.
include!(concat!(env!("OUT_DIR"), "/shared_qml.rs"));

// Writes the embedded shared QML files to a temp folder and returns that folder.
fn extract_shared_qml() -> std::io::Result<PathBuf> {
    let dir = std::env::temp_dir().join("amplify-shared-qml");
    for (relative_path, bytes) in SHARED_QML_FILES {
        let path = dir.join(relative_path);
        if let Some(parent) = path.parent() {
            std::fs::create_dir_all(parent)?;
        }
        std::fs::write(&path, bytes)?;
    }
    Ok(dir)
}

pub fn run() {
    // let shared_qml = concat!(env!("CARGO_MANIFEST_DIR"), "/../../../shared/qml");
    let shared_qml = extract_shared_qml().expect("failed to extract the shared QML files");

    QApp::new()
        .register::<backend::Greeter>()
        //.add_import_path(shared_qml)
        .add_import_path(&shared_qml.to_string_lossy())
        .load_qml(include_bytes!("../qml/main.qml"))
        .run();
}