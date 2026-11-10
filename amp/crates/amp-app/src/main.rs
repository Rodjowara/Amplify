fn main() {
    amp_control::spawn("0.0.0.0:50051".parse().unwrap());
    amp_ui::run();
}
