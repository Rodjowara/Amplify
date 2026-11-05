fn main() {
    let addr = "0.0.0.0:50051".parse().expect("invalid listen address");
    amp_control::server::spawn(addr);

    // Blocks on the main thread until the UI is closed.
    amp_ui::run();
}