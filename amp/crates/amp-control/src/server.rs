use std::net::SocketAddr;
use std::thread::{self, JoinHandle};

use tonic::transport::Server;

use crate::amp_handler::AmpHandler;
use crate::handlers::DummyHandler;
use crate::proto::amp::amp_service_server::AmpServiceServer;
use crate::proto::dummy::dummy_service_server::DummyServiceServer;

pub async fn serve(addr: SocketAddr) -> Result<(), tonic::transport::Error> {
    Server::builder()
        .add_service(DummyServiceServer::new(DummyHandler))
            //Amp is a temporary name for the device
        .add_service(AmpServiceServer::new(AmpHandler::new("Amp")))
        .serve(addr)
        .await
}

/// Runs the gRPC server on its own thread with its own tokio runtime.
pub fn spawn(addr: SocketAddr) -> JoinHandle<()> {
    thread::Builder::new()
        .name("amp-control".into())
        .spawn(move || {
            let runtime = tokio::runtime::Builder::new_multi_thread()
                .enable_all()
                .build()
                .expect("failed to build tokio runtime");

            if let Err(e) = runtime.block_on(serve(addr)) {
                eprintln!("gRPC server stopped: {e}");
            }
        })
        .expect("failed to spawn control thread")
}