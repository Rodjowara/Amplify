mod common;

use std::time::Duration;

use amp_control::proto::dummy::{dummy_service_client::DummyServiceClient, PingRequest};

#[tokio::test]
async fn ping_returns_pong() {
    let port = common::free_port();
    let addr = format!("127.0.0.1:{port}").parse().unwrap();
    tokio::spawn(amp_control::server::serve(
        addr,
        Default::default(),
        Default::default(),
    ));

    // Retry until the server is up, instead of a fixed sleep.
    let url = format!("http://127.0.0.1:{port}");
    let mut client = None;
    for _ in 0..50 {
        if let Ok(c) = DummyServiceClient::connect(url.clone()).await {
            client = Some(c);
            break;
        }
        tokio::time::sleep(Duration::from_millis(100)).await;
    }
    let mut client = client.expect("gRPC server did not start");

    let reply = client
        .ping(PingRequest { message: "hello".into() })
        .await
        .unwrap()
        .into_inner();

    assert_eq!(reply.message, "pong: hello");
}