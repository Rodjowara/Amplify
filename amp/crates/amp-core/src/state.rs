use std::sync::{Arc, RwLock};

#[derive(Debug, Clone, PartialEq)]
pub struct SnapserverAddress {
    pub host: String,
    pub port: u16,
}

#[derive(Default)]
pub struct AmpState {
    pub snapserver: Option<SnapserverAddress>,
}

pub type SharedState = Arc<RwLock<AmpState>>;