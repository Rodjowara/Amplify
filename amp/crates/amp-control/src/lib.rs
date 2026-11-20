pub mod proto {
    pub mod amp {
        tonic::include_proto!("amplify.amp");
    }
}

mod maestro_service;
pub mod server;