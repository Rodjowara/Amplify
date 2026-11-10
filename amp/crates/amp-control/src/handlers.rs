use std::sync::Mutex;

use amp_audio::Player;
use tonic::{Request, Response, Status};

use crate::proto::amp::{AmpState, GetStateRequest, SetStreamRequest, amp_service_server::AmpService};
use crate::proto::dummy::{PingRequest, PingResponse, dummy_service_server::DummyService};

pub struct DummyHandler;

#[tonic::async_trait]
impl DummyService for DummyHandler {
    async fn ping(&self, request: Request<PingRequest>) -> Result<Response<PingResponse>, Status> {
        let message = format!("pong: {}", request.into_inner().message);
        Ok(Response::new(PingResponse { message }))
    }
}

pub struct AmpHandler {
    state: Mutex<AmpState>,
    player: Player,
}

impl AmpHandler {
    pub fn new(name: &str) -> Self {
        Self {
            state: Mutex::new(AmpState { name: name.into(), stream: None }),
            player: Player::default(),
        }
    }
}

#[tonic::async_trait]
impl AmpService for AmpHandler {
    async fn get_state(&self, _: Request<GetStateRequest>) -> Result<Response<AmpState>, Status> {
        Ok(Response::new(self.state.lock().unwrap().clone()))
    }

    async fn set_stream(&self, request: Request<SetStreamRequest>) -> Result<Response<AmpState>, Status> {
        let stream = request
            .into_inner()
            .stream
            .ok_or_else(|| Status::invalid_argument("stream is required"))?;
        if stream.host.trim().is_empty() {
            return Err(Status::invalid_argument("host must not be empty"));
        }
        let port = u16::try_from(stream.port)
            .ok()
            .filter(|&p| p != 0)
            .ok_or_else(|| Status::invalid_argument("port must be between 1 and 65535"))?;

        self.player.set_stream(stream.host.clone(), port).await;

        let mut state = self.state.lock().unwrap();
        state.stream = Some(stream);
        Ok(Response::new(state.clone()))
    }
}
