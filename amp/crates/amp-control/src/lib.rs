pub mod proto {
    // The string is the proto package name.
    pub mod dummy {
        tonic::include_proto!("amplify.dummy");
    }
    pub mod amp {
        tonic::include_proto!("amplify.amp");
    }
}

mod amp_handler;
mod handlers;
pub mod server;