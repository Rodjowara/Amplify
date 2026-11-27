use amp_core::CONTROL_PORT;
use qtbridge::qobject;
use std::net::UdpSocket;

#[derive(Default)]
pub struct Greeter {}

const ROUTE_PROBE_ADDRESS: &str = "8.8.8.8:80";

fn ip_address() -> String {
    UdpSocket::bind("0.0.0.0:0")
        .and_then(|socket| {
            socket.connect(ROUTE_PROBE_ADDRESS)?;
            socket.local_addr()
        })
        .map(|address| format!("{}:{CONTROL_PORT}", address.ip()))
        .unwrap_or_else(|_| "No network".into())
}

#[qobject(Singleton)]
impl Greeter {
    qproperty!("message", Read = message, Constant);

    fn message(&self) -> String {
        ip_address().into()
    }
}
