use std::env;
use std::fs;
use std::path::{Path, PathBuf};

// Embeds every file of shared/qml and of the amp's own QML module in the Amp binary.
fn main() {
    let manifest_dir = PathBuf::from(env::var("CARGO_MANIFEST_DIR").unwrap());
    let shared_qml = manifest_dir.join("../../../shared/qml");
    let amp_qml = manifest_dir.join("qml");

    let mut code = embed("SHARED_QML_FILES", &shared_qml, &shared_qml);
    code.push_str(&embed("AMP_QML_FILES", &amp_qml, &amp_qml.join("Amp")));

    let out_file = PathBuf::from(env::var("OUT_DIR").unwrap()).join("embedded_qml.rs");
    fs::write(out_file, code).unwrap();
}

fn embed(const_name: &str, root: &Path, dir: &Path) -> String {
    println!("cargo:rerun-if-changed={}", dir.display());

    let mut files = Vec::new();
    collect_files(dir, &mut files);
    files.sort();

    let mut code = format!("pub const {const_name}: &[(&str, &[u8])] = &[\n");
    for file in &files {
        let relative = file
            .strip_prefix(root)
            .unwrap()
            .to_string_lossy()
            .replace('\\', "/");
        code.push_str(&format!("    ({relative:?}, include_bytes!({file:?})),\n"));
    }
    code.push_str("];\n");
    code
}

fn collect_files(dir: &Path, files: &mut Vec<PathBuf>) {
    for entry in fs::read_dir(dir).expect("QML folder not found") {
        let path = entry.unwrap().path();
        if path.is_dir() {
            collect_files(&path, files);
        } else {
            files.push(path);
        }
    }
}
