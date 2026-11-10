use std::sync::Mutex;

use amp_audio::Player;
use tonic::{Request, Response, Status};

use crate::proto::amp::{
    AmpState, GetStateRequest, SetStreamRequest, StreamInfo, amp_service_server::AmpService,
};
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
        let stream = validate(request.into_inner().stream)?;
        self.player.set_stream(stream.host.clone(), stream.port as u16).await;

        let mut state = self.state.lock().unwrap();
        state.stream = Some(stream);
        Ok(Response::new(state.clone()))
    }
}

fn validate(stream: Option<StreamInfo>) -> Result<StreamInfo, Status> {
    let stream = stream.ok_or_else(|| Status::invalid_argument("stream is required"))?;
    if stream.host.trim().is_empty() {
        return Err(Status::invalid_argument("host must not be empty"));
    }
    if !(1..=65535).contains(&stream.port) {
        return Err(Status::invalid_argument("port must be between 1 and 65535"));
    }
    Ok(stream)
}

#[cfg(test)]
mod tests {
    use super::*;

    fn stream(host: &str, port: u32) -> Option<StreamInfo> {
        Some(StreamInfo { host: host.into(), port })
    }

    #[test]
    fn accepts_valid_stream() {
        assert!(validate(stream("192.168.1.10", 1704)).is_ok());
        assert!(validate(stream("h", 1)).is_ok());
        assert!(validate(stream("h", 65535)).is_ok());
    }

    #[test]
    fn rejects_invalid_stream() {
        assert!(validate(None).is_err());
        assert!(validate(stream("", 1704)).is_err());
        assert!(validate(stream("   ", 1704)).is_err());
        assert!(validate(stream("h", 0)).is_err());
        assert!(validate(stream("h", 65536)).is_err());
    }
}
