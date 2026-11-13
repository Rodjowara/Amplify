use std::sync::{Arc, Mutex};

use amp_audio::chain::AudioPipeline;
use tonic::{Request, Response, Status};

use crate::proto::amp::{
    amp_service_server::AmpService, AmpState, GetStateRequest, SetStreamRequest,
};

#[derive(Debug, Clone)]
pub struct AmpHandler {
    state: Arc<Mutex<AmpState>>,
    audio: Arc<AudioPipeline>,
}

impl AmpHandler {
    pub fn new(name: impl Into<String>) -> Self {
        Self {
            state: Arc::new(Mutex::new(AmpState {
                name: name.into(),
                stream: None,
            })),
            audio: Arc::new(AudioPipeline::new()),
        }
    }
}

#[tonic::async_trait]
impl AmpService for AmpHandler {
    async fn get_state(
        &self,
        _request: Request<GetStateRequest>,
    ) -> Result<Response<AmpState>, Status> {
        let state = self.state.lock().expect("state lock poisoned").clone();
        Ok(Response::new(state))
    }

    async fn set_stream(
        &self,
        request: Request<SetStreamRequest>,
    ) -> Result<Response<AmpState>, Status> {
        let stream = request
            .into_inner()
            .stream
            .ok_or_else(|| Status::invalid_argument("stream is required"))?;

        if stream.host.trim().is_empty() {
            return Err(Status::invalid_argument("host must not be empty"));
        }
        if stream.port == 0 || stream.port > 65535 {
            return Err(Status::invalid_argument("port must be between 1 and 65535"));
        }

        self.audio
            .set_stream(stream.host.clone(), stream.port as u16)
            .await;

        let mut state = self.state.lock().expect("state lock poisoned");
        state.stream = Some(stream);
        Ok(Response::new(state.clone()))
    }
}
