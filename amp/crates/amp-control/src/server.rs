use std::net::SocketAddr;
use std::thread::{self, JoinHandle};

use amp_audio::session::PlayerSlot;
use amp_core::state::SharedState;
use tonic::transport::Server;

use crate::amp_handler::AmpHandler;
use crate::proto::amp::amp_service_server::AmpServiceServer;

pub async fn serve(
    addr: SocketAddr,
    state: SharedState,
    slot: PlayerSlot,
) -> Result<(), tonic::transport::Error> {
    Server::builder()
        .add_service(AmpServiceServer::new(AmpHandler::new(state, slot)))
        .serve(addr)
        .await
}

/// Runs the gRPC server on its own thread with its own tokio runtime.
pub fn spawn(addr: SocketAddr, state: SharedState, slot: PlayerSlot) -> JoinHandle<()> {
    thread::Builder::new()
        .name("amp-control".into())
        .spawn(move || {
            let runtime = tokio::runtime::Builder::new_multi_thread()
                .enable_all()
                .build()
                .expect("failed to build tokio runtime");

            if let Err(e) = runtime.block_on(serve(addr, state, slot)) {
                eprintln!("gRPC server stopped: {e}");
            }
        })
        .expect("failed to spawn control thread")
}