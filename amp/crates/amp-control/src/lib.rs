pub mod proto {
    pub mod amp {
        tonic::include_proto!("amplify.amp");
    }
}

mod amp_handler;
pub mod server;