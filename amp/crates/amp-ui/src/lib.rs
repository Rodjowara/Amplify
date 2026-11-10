use std::path::PathBuf;
use std::sync::OnceLock;

use qtbridge::{QApp, qobject};

// Files of shared/qml, embedded at build time by build.rs.
include!(concat!(env!("OUT_DIR"), "/shared_qml.rs"));

static ADDRESS: OnceLock<String> = OnceLock::new();

#[derive(Default)]
pub struct AmpInfo {}

#[qobject(Singleton)]
impl AmpInfo {
    qproperty!("address", Read = address, Constant);

    fn address(&self) -> String {
        ADDRESS.get().cloned().unwrap_or_default()
    }
}

// Writes the embedded shared QML files to a temp folder so Qt can import them.
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

pub fn run(address: String) {
    let _ = ADDRESS.set(address);
    let shared_qml = extract_shared_qml().expect("failed to extract the shared QML files");

    QApp::new()
        .register::<AmpInfo>()
        .add_import_path(&shared_qml.to_string_lossy())
        .load_qml(include_bytes!("../qml/main.qml"))
        .run();
}
