#[tokio::test]
async fn start_connects_to_a_running_server() {
    let port = start_test_snapserver().await;
    let cfg = SnapcastStreamConfig { host: "127.0.0.1".into(), port, ..Default::default() };

    let session = StreamSession::start(cfg, Duration::from_secs(3)).await.expect("should connect");

    tokio::time::timeout(Duration::from_secs(3), session.stop())
        .await
        .expect("stop() must not hang");
}

#[tokio::test]
async fn start_fails_when_nothing_is_listening() {
    let cfg = SnapcastStreamConfig { host: "127.0.0.1".into(), port: free_port(), ..Default::default() };
    assert!(StreamSession::start(cfg, Duration::from_secs(1)).await.is_err());
}