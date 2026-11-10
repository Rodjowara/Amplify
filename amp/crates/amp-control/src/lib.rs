mod handlers;

use std::net::SocketAddr;
use std::thread;

use proto::amp::amp_service_server::AmpServiceServer;
use proto::dummy::dummy_service_server::DummyServiceServer;
use tokio::net::TcpListener;
use tonic::transport::server::TcpIncoming;

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
        let result = runtime.block_on(async {
            let listener = TcpListener::bind(addr).await?;
            serve(listener).await.map_err(std::io::Error::other)
        });
        if let Err(e) = result {
            eprintln!("gRPC server stopped: {e}");
        }
    });
}

async fn serve(listener: TcpListener) -> Result<(), tonic::transport::Error> {
    tonic::transport::Server::builder()
        .add_service(DummyServiceServer::new(handlers::DummyHandler))
        // "Amp" is a temporary name for the device.
        .add_service(AmpServiceServer::new(handlers::AmpHandler::new("Amp")))
        .serve_with_incoming(TcpIncoming::from(listener))
        .await
}

#[cfg(test)]
mod tests {
    use super::*;
    use proto::amp::amp_service_client::AmpServiceClient;
    use proto::amp::{GetStateRequest, SetStreamRequest, StreamInfo};

    #[tokio::test]
    async fn set_stream_round_trip() {
        let listener = TcpListener::bind("127.0.0.1:0").await.unwrap();
        let url = format!("http://{}", listener.local_addr().unwrap());
        tokio::spawn(serve(listener));
        let mut client = AmpServiceClient::connect(url).await.unwrap();

        let state = client.get_state(GetStateRequest {}).await.unwrap().into_inner();
        assert_eq!(state.name, "Amp");
        assert!(state.stream.is_none());

        // Nothing listens on port 1, so the player just fails to connect.
        let stream = StreamInfo { host: "127.0.0.1".into(), port: 1 };
        let request = SetStreamRequest { stream: Some(stream.clone()) };
        client.set_stream(request).await.unwrap();

        let state = client.get_state(GetStateRequest {}).await.unwrap().into_inner();
        assert_eq!(state.stream, Some(stream));

        let bad = SetStreamRequest { stream: Some(StreamInfo { host: "".into(), port: 1704 }) };
        let err = client.set_stream(bad).await.unwrap_err();
        assert_eq!(err.code(), tonic::Code::InvalidArgument);
    }
}
