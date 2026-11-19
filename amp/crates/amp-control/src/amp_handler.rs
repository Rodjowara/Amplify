use std::sync::Arc;
use std::time::Duration;

use amp_audio::session::{PlayerSlot, StreamSession};
use amp_core::state::{SharedState, StreamConfig};
use tokio::sync::Mutex;
use tonic::{Request, Response, Status};

use crate::proto::amp::{amp_service_server::AmpService, HandshakeRequest, HandshakeResponse};

#[derive(Clone)]
pub struct AmpHandler {
    state: SharedState,
    slot: PlayerSlot,
    session: Arc<Mutex<Option<StreamSession>>>, // tokio::sync::Mutex
}

impl AmpHandler {
    pub fn new(state: SharedState, slot: PlayerSlot) -> Self {
        Self { state, slot, session: Arc::new(Mutex::new(None)) }
    }
}

// TODO(#136): Maestro's sidebar shows each amp as online/offline from a status call it will make
// every 2 s (IAmpStatusChecker in Maestro.Core, a dummy for now). Add that RPC here.
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

        let response = match StreamSession::start(req.snapcast_host.clone(), port, Duration::from_secs(5)).await {
            Ok(s) => {
                // Let the audio output start playing this stream.
                *self.slot.lock().expect("slot lock poisoned") = Some(Arc::clone(&s.handle));
                *session = Some(s);
                self.state.write().expect("state lock poisoned").stream = Some(StreamConfig {
                    host: req.snapcast_host,
                    port,
                });
                // TODO(#119): Maestro has added this amp now, so `state.stream` being Some can be
                // the panel's "connected to Maestro" status.
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
