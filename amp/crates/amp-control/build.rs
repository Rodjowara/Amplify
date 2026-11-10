fn main() -> Result<(), Box<dyn std::error::Error>> {
    let root = "../../../shared/protos";
    tonic_prost_build::configure()
        .compile_protos(&[format!("{root}/dummy.proto"), format!("{root}/amp.proto")], &[root.into()])?;
    println!("cargo:rerun-if-changed={root}");
    Ok(())
}
