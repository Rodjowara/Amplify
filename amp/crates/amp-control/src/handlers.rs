use tonic::{Request, Response, Status};

use crate::proto::{dummy_service_server::DummyService, PingRequest, PingResponse};

#[derive(Debug, Default)]
pub struct DummyHandler;

#[tonic::async_trait]
impl DummyService for DummyHandler {
    async fn ping(
        &self,
        request: Request<PingRequest>,
    ) -> Result<Response<PingResponse>, Status> {
        let message = request.into_inner().message;
        Ok(Response::new(PingResponse {
            message: format!("pong: {message}"),
        }))
    }
}