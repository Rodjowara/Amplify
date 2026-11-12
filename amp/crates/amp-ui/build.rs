use std::env;
use std::fs;
use std::path::{Path, PathBuf};

// Embeds every file of shared/qml in the Amp binary.
fn main() {
    let manifest_dir = PathBuf::from(env::var("CARGO_MANIFEST_DIR").unwrap());
    let shared_qml = manifest_dir.join("../../../shared/qml");
    println!("cargo:rerun-if-changed={}", shared_qml.display());

    let mut files = Vec::new();
    collect_files(&shared_qml, &mut files);
    files.sort();

    let mut code = String::from("pub const SHARED_QML_FILES: &[(&str, &[u8])] = &[\n");
    for file in &files {
        let relative = file
            .strip_prefix(&shared_qml)
            .unwrap()
            .to_string_lossy()
            .replace('\\', "/");
        code.push_str(&format!("    ({relative:?}, include_bytes!({file:?})),\n"));
    }
    code.push_str("];\n");

    let out_file = PathBuf::from(env::var("OUT_DIR").unwrap()).join("shared_qml.rs");
    fs::write(out_file, code).unwrap();
}

fn collect_files(dir: &Path, files: &mut Vec<PathBuf>) {
    for entry in fs::read_dir(dir).expect("shared/qml folder not found") {
        let path = entry.unwrap().path();
        if path.is_dir() {
            collect_files(&path, files);
        } else {
            files.push(path);
        }
    }
}