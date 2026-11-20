mod common;

use amp_control::proto::amp::HandshakeRequest;
use amp_core::state::{SharedState, StreamConfig};
use amp_audio::output::PlayerSlot;
use common::*;

struct Fixture {
    state: SharedState,
    slot: PlayerSlot,
    client: amp_control::proto::amp::amp_service_client::AmpServiceClient<tonic::transport::Channel>,
}

async fn start_amp() -> Fixture {
    let port = free_port();
    let state = SharedState::default();
    let slot = PlayerSlot::default();
    tokio::spawn(amp_control::server::serve(
        format!("127.0.0.1:{port}").parse().unwrap(),
        state.clone(),
        slot.clone(),
    ));
    Fixture { state, slot, client: connect_client(port).await }
}

fn req(host: &str, port: u32) -> HandshakeRequest {
    HandshakeRequest { snapcast_host: host.into(), snapcast_port: port }
}

#[tokio::test]
async fn rejects_invalid_input() {
    let mut f = start_amp().await;
    for (host, port) in [("", 1704), ("   ", 1704), ("127.0.0.1", 0), ("127.0.0.1", 65536)] {
        let err = f.client.handshake(req(host, port)).await.unwrap_err();
        assert_eq!(err.code(), tonic::Code::InvalidArgument, "host={host:?} port={port}");
    }
    assert!(f.state.read().unwrap().stream.is_none());
    assert!(f.slot.lock().unwrap().is_none());
}

#[tokio::test]
async fn successful_handshake_updates_state_and_slot() {
    let mut f = start_amp().await;
    let snap_port = start_test_snapserver().await;

    let reply = f.client.handshake(req("127.0.0.1", snap_port as u32)).await.unwrap().into_inner();

    assert!(reply.success);
    assert!(reply.error.is_none());
    assert_eq!(
        f.state.read().unwrap().stream,
        Some(StreamConfig { host: "127.0.0.1".into(), port: snap_port })
    );
    assert!(f.slot.lock().unwrap().is_some());
}

#[tokio::test]
async fn unreachable_server_reports_failure() {
    let mut f = start_amp().await;

    let reply = f.client.handshake(req("127.0.0.1", free_port() as u32)).await.unwrap().into_inner();

    assert!(!reply.success);
    assert!(reply.error.is_some());
    assert!(f.state.read().unwrap().stream.is_none());
    assert!(f.slot.lock().unwrap().is_none());
}

#[tokio::test]
async fn new_handshake_replaces_the_old_stream() {
    let mut f = start_amp().await;
    let first = start_test_snapserver().await;
    let second = start_test_snapserver().await;

    assert!(f.client.handshake(req("127.0.0.1", first as u32)).await.unwrap().into_inner().success);
    assert!(f.client.handshake(req("127.0.0.1", second as u32)).await.unwrap().into_inner().success);

    assert_eq!(f.state.read().unwrap().stream.as_ref().unwrap().port, second);
}

#[tokio::test]
async fn failed_handshake_clears_the_previous_stream() {
    let mut f = start_amp().await;
    let good = start_test_snapserver().await;

    assert!(f.client.handshake(req("127.0.0.1", good as u32)).await.unwrap().into_inner().success);
    let reply = f.client.handshake(req("127.0.0.1", free_port() as u32)).await.unwrap().into_inner();

    assert!(!reply.success);
    assert!(f.state.read().unwrap().stream.is_none());
    assert!(f.slot.lock().unwrap().is_none());
}