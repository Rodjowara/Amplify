use std::time::Duration;

use amp_audio::output::PlaybackSource;
use amp_audio::snapcast::SnapcastConnection;
use amp_core::state::{SharedState, SnapserverAddress};
use tokio::sync::Mutex;
use tonic::{Request, Response, Status};

use crate::proto::amp::{amp_service_server::AmpService, HandshakeRequest, HandshakeResponse};

const CONNECT_TIMEOUT: Duration = Duration::from_secs(2);

pub struct MaestroService {
    state: SharedState,
    slot: PlaybackSource,
    // Held for the whole handshake so concurrent handshakes run one at a time.
    handshake_lock: Mutex<()>,
}

impl MaestroService {
    pub fn new(state: SharedState, slot: PlaybackSource) -> Self {
        Self { state, slot, handshake_lock: Mutex::new(()) }
    }
}

#[tonic::async_trait]
impl AmpService for MaestroService {
    async fn handshake(
        &self,
        request: Request<HandshakeRequest>,
    ) -> Result<Response<HandshakeResponse>, Status> {
        let req = request.into_inner();

        if req.snapcast_host.trim().is_empty() {
            return Err(Status::invalid_argument("snapcast_host must not be empty"));
        }
        let port = u16::try_from(req.snapcast_port)
            .ok()
            .filter(|p| *p != 0)
            .ok_or_else(|| Status::invalid_argument("snapcast_port must be between 1 and 65535"))?;

        let _handshake_guard = self.handshake_lock.lock().await;

        // Silence the output first, then stop the old stream and clear the old config.
        let old = self.slot.lock().expect("slot lock poisoned").take();
        if let Some(old) = old {
            old.stop().await;
        }
        self.state.write().expect("state lock poisoned").snapserver = None;

        let response = match SnapcastConnection::connect(req.snapcast_host.clone(), port, CONNECT_TIMEOUT).await {
            Ok(connection) => {
                // Let the audio output start playing this stream.
                *self.slot.lock().expect("slot lock poisoned") = Some(connection);
                self.state.write().expect("state lock poisoned").snapserver = Some(SnapserverAddress {
                    host: req.snapcast_host,
                    port,
                });

                println!("Connection successful!");

                HandshakeResponse { success: true, error: None }
            }
            Err(e) => {
                eprintln!("failed to connect to snapserver: {e}");
                HandshakeResponse { success: false, error: Some(e) }
            }
        };

        Ok(Response::new(response))
    }
}
