use std::sync::{Arc, RwLock};

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct StreamConfig {
    pub host: String,
    pub port: u16,
}

#[derive(Debug, Clone, Default)]
pub struct AmpState {
    /// Set after a successful handshake, None before that or after a failed one.
    pub stream: Option<StreamConfig>,
}

pub type SharedState = Arc<RwLock<AmpState>>;