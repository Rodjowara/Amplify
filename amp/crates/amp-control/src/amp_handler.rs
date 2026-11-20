use std::sync::Arc;
use std::time::Duration;

use amp_audio::output::PlayerSlot;
use amp_audio::snapcast::SnapcastClientHandle;
use amp_core::state::{SharedState, StreamConfig};
use tokio::sync::Mutex;
use tonic::{Request, Response, Status};

use crate::proto::amp::{amp_service_server::AmpService, HandshakeRequest, HandshakeResponse};

const CONNECT_TIMEOUT: Duration = Duration::from_secs(2);

pub struct AmpHandler {
    state: SharedState,
    slot: PlayerSlot,
    // Held for the whole handshake so concurrent handshakes run one at a time.
    session: Mutex<Option<Arc<SnapcastClientHandle>>>,
}

impl AmpHandler {
    pub fn new(state: SharedState, slot: PlayerSlot) -> Self {
        Self { state, slot, session: Mutex::new(None) }
    }
}

#[tonic::async_trait]
impl AmpService for AmpHandler {
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

        let mut session = self.session.lock().await;

        // Silence the output first, then stop the old stream and clear the old config.
        *self.slot.lock().expect("slot lock poisoned") = None;
        if let Some(old) = session.take() {
            old.stop().await;
        }
        self.state.write().expect("state lock poisoned").stream = None;

        let response = match SnapcastClientHandle::connect(req.snapcast_host.clone(), port, CONNECT_TIMEOUT).await {
            Ok(client) => {
                let client = Arc::new(client);
                // Let the audio output start playing this stream.
                *self.slot.lock().expect("slot lock poisoned") = Some(Arc::clone(&client));
                *session = Some(client);
                self.state.write().expect("state lock poisoned").stream = Some(StreamConfig {
                    host: req.snapcast_host,
                    port,
                });
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
