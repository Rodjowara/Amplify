use std::time::Duration;

use amp_control::proto::amp::{
    amp_service_client::AmpServiceClient, GetStateRequest, SetStreamRequest, StreamInfo,
};

#[tokio::test]
async fn set_stream_updates_state() {
    let addr = "127.0.0.1:50098".parse().unwrap();
    tokio::spawn(amp_control::server::serve(addr));
    tokio::time::sleep(Duration::from_millis(200)).await;

    let mut client = AmpServiceClient::connect("http://127.0.0.1:50098")
        .await
        .unwrap();

    let initial = client.get_state(GetStateRequest {}).await.unwrap().into_inner();
    assert!(initial.stream.is_none());

    client
        .set_stream(SetStreamRequest {
            stream: Some(StreamInfo { host: "192.168.1.10".into(), port: 1704 }),
        })
        .await
        .unwrap();

    let after = client.get_state(GetStateRequest {}).await.unwrap().into_inner();
    assert_eq!(after.stream.unwrap().port, 1704);
}