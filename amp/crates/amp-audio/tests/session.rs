// NOTE: same test is inside amp-control/tests/handshake.rs

// #[tokio::test]
// async fn start_connects_to_a_running_server() {
//     let port = start_test_snapserver().await;
//     let session = StreamSession::start("127.0.0.1".into(), port, Duration::from_secs(3)).await.expect("should connect");
//
//     tokio::time::timeout(Duration::from_secs(3), session.stop())
//         .await
//         .expect("stop() must not hang");
// }
//
// #[tokio::test]
// async fn start_fails_when_nothing_is_listening() {
//     assert!(StreamSession::start("127.0.0.1".into(), free_port(), Duration::from_secs(1)).await.is_err());
// }
