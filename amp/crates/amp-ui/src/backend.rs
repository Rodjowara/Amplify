use qtbridge::qobject;

#[derive(Default)]
pub struct Greeter {}

#[qobject(Singleton)]
impl Greeter {
    qproperty!("message", Read = message, Constant);

    fn message(&self) -> String {
        "Hello from Rust!".into()
    }
}
