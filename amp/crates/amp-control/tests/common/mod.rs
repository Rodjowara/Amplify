use std::net::TcpListener as StdListener;
use std::time::Duration;

use snapcast_server::{ServerConfig, SnapServer};
use tokio::net::TcpListener;

/// A port nobody is listening on right now.
pub fn free_port() -> u16 {
    StdListener::bind("127.0.0.1:0").unwrap().local_addr().unwrap().port()
}

/// Starts an embedded snapserver on a random port and returns that port.
pub async fn start_test_snapserver() -> u16 {
    let listener = TcpListener::bind("127.0.0.1:0").await.unwrap();
    let port = listener.local_addr().unwrap().port();

    let (mut server, mut events) = SnapServer::new(ServerConfig::default());
    let sender = server.add_f32_stream("default").expect("valid stream");

    tokio::spawn(async move { while events.recv().await.is_some() {} });
    tokio::spawn(async move {
        let _keep_alive = sender; // dropping it would close the stream
        std::future::pending::<()>().await
    });
    tokio::spawn(async move {
        let _ = server.serve(listener).await;
    });
    port
}

/// Retry instead of a fixed sleep, so slow machines don't cause flaky tests.
pub async fn connect_client(
    port: u16,
) -> amp_control::proto::amp::amp_service_client::AmpServiceClient<tonic::transport::Channel> {
    let url = format!("http://127.0.0.1:{port}");
    for _ in 0..50 {
        if let Ok(client) =
            amp_control::proto::amp::amp_service_client::AmpServiceClient::connect(url.clone()).await
        {
            return client;
        }
        tokio::time::sleep(Duration::from_millis(100)).await;
    }
    panic!("gRPC server did not start");
}