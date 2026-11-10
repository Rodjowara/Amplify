mod handlers;

use std::net::SocketAddr;
use std::thread;

use proto::amp::amp_service_server::AmpServiceServer;
use proto::dummy::dummy_service_server::DummyServiceServer;

mod proto {
    pub mod dummy {
        tonic::include_proto!("amplify.dummy");
    }
    pub mod amp {
        tonic::include_proto!("amplify.amp");
    }
}

/// Runs the gRPC server on its own thread with its own tokio runtime.
pub fn spawn(addr: SocketAddr) {
    thread::spawn(move || {
        let runtime = tokio::runtime::Runtime::new().expect("failed to build tokio runtime");
        let server = tonic::transport::Server::builder()
            .add_service(DummyServiceServer::new(handlers::DummyHandler))
            // "Amp" is a temporary name for the device.
            .add_service(AmpServiceServer::new(handlers::AmpHandler::new("Amp")))
            .serve(addr);
        if let Err(e) = runtime.block_on(server) {
            eprintln!("gRPC server stopped: {e}");
        }
    });
}
