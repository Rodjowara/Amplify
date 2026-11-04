use amp_control::proto::{dummy_service_client::DummyServiceClient, PingRequest};

#[tokio::test]
async fn ping_returns_pong() {
    let addr = "127.0.0.1:50099".parse().unwrap();
    tokio::spawn(amp_control::server::serve(addr));
    tokio::time::sleep(std::time::Duration::from_millis(200)).await;

    let mut client = DummyServiceClient::connect("http://127.0.0.1:50099")
        .await
        .unwrap();
    let reply = client
        .ping(PingRequest { message: "hello".into() })
        .await
        .unwrap()
        .into_inner();

    assert_eq!(reply.message, "pong: hello");
}