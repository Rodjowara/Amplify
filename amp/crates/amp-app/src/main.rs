use std::net::{IpAddr, Ipv4Addr, SocketAddr, UdpSocket};

const PORT: u16 = 50051;

fn main() {
    amp_control::spawn(SocketAddr::new(Ipv4Addr::UNSPECIFIED.into(), PORT));
    amp_ui::run(format!("{}:{PORT}", lan_ip()));
}

fn lan_ip() -> IpAddr {
    UdpSocket::bind((Ipv4Addr::UNSPECIFIED, 0))
        .and_then(|socket| {
            socket.connect((Ipv4Addr::new(192, 0, 2, 1), 9))?;
            socket.local_addr()
        })
        .map_or(Ipv4Addr::LOCALHOST.into(), |addr| addr.ip())
}
