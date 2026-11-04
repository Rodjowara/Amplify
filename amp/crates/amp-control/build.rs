fn main() -> Result<(), Box<dyn std::error::Error>> {
    let proto_root = "../../../shared/protos";

    tonic_prost_build::configure()
        .compile_protos(&[format!("{proto_root}/dummy.proto")], &[proto_root.to_string()])?;

    println!("cargo:rerun-if-changed={proto_root}");
    Ok(())
}