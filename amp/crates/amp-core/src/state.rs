use std::sync::{Arc, RwLock};

#[derive(Debug, Clone, PartialEq)]
pub struct StreamConfig {
    pub host: String,
    pub port: u16,
}

#[derive(Default)]
pub struct AmpState {
    pub stream: Option<StreamConfig>,
}

pub type SharedState = Arc<RwLock<AmpState>>;