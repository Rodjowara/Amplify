use std::sync::{Arc, Mutex};
use std::time::Duration;

use snapcast_client::ClientEvent;
use tokio::sync::oneshot;
use tokio::task::JoinHandle;

use crate::snapcast::SnapcastClientHandle;

/// The stream the audio output should play right now (None = silence).
pub type PlayerSlot = Arc<Mutex<Option<Arc<SnapcastClientHandle>>>>;

pub struct StreamSession {
    /// Handed to the audio output so it can pull time-synchronized samples.
    pub handle: Arc<SnapcastClientHandle>,
    stop_tx: Option<oneshot::Sender<()>>,
    task: JoinHandle<()>,
}

impl StreamSession {
    /// Connects to the snapserver. Returns Ok only once the client reports `Connected`.
    pub async fn start(
        host: String,
        port: u16,
        connect_timeout: Duration,
    ) -> Result<Self, String> {
        let (handle, mut event_rx) = SnapcastClientHandle::spawn(host, port);

        let wait_for_connect = async {
            while let Some(ev) = event_rx.recv().await {
                match ev {
                    ClientEvent::Connected { .. } => return Ok(()),
                    ClientEvent::Disconnected { reason } => {
                        return Err(format!("disconnected: {reason}"))
                    }
                    _ => {}
                }
            }
            Err("client stopped before connecting".to_string())
        };

        match tokio::time::timeout(connect_timeout, wait_for_connect).await {
            Ok(Ok(())) => {}
            Ok(Err(e)) => {
                handle.stop().await;
                return Err(e);
            }
            Err(_) => {
                handle.stop().await;
                return Err("timed out connecting to the snapserver".into());
            }
        }

        let handle = Arc::new(handle);
        let task_handle = Arc::clone(&handle);
        let (stop_tx, mut stop_rx) = oneshot::channel();

        let task = tokio::spawn(async move {
            loop {
                tokio::select! {
                    _ = &mut stop_rx => break,
                    ev = event_rx.recv() => { if ev.is_none() { break; } }
                }
            }
            task_handle.stop().await;
        });

        Ok(Self { handle, stop_tx: Some(stop_tx), task })
    }

    pub async fn stop(mut self) {
        if let Some(tx) = self.stop_tx.take() {
            let _ = tx.send(());
        }
        let _ = self.task.await;
    }
}