pub mod proto {
    // The string is the proto package name.
    tonic::include_proto!("amplify.dummy");
}

mod handlers;
pub mod server;