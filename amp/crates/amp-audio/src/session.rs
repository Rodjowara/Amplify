use std::sync::{Arc, Mutex};
use std::time::Duration;

use snapcast_client::ClientEvent;
use tokio::sync::oneshot;
use tokio::task::JoinHandle;

use crate::snapcast::{SnapcastClientHandle, SnapcastStreamConfig, SyncedStream};

pub type PlayerSlot = Arc<Mutex<Option<SyncedStream>>>;

pub struct StreamSession {
    pub synced: SyncedStream,
    stop_tx: Option<oneshot::Sender<()>>,
    task: JoinHandle<()>,
}

impl StreamSession {
    pub async fn start(
        config: SnapcastStreamConfig,
        connect_timeout: Duration,
    ) -> Result<Self, String> {
        let mut handle = SnapcastClientHandle::spawn(config);

        let wait_for_connect = async {
            while let Some(ev) = handle.event_rx.recv().await {
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

        let synced = handle.synced_stream();
        let (stop_tx, mut stop_rx) = oneshot::channel();

        let task = tokio::spawn(async move {
            loop {
                tokio::select! {
                    _ = &mut stop_rx => break,
                    ev = handle.event_rx.recv() => { if ev.is_none() { break; } }
                    frame = handle.audio_rx.recv() => { if frame.is_none() { break; } }
                }
            }
            handle.stop().await;
        });

        Ok(Self { synced, stop_tx: Some(stop_tx), task })
    }

    pub async fn stop(mut self) {
        if let Some(tx) = self.stop_tx.take() {
            let _ = tx.send(());
        }
        let _ = self.task.await;
    }
}