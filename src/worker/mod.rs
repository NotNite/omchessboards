mod board;

use crate::network::parse_message;
use crate::protos::network::client_message::Payload as ClientPayload;
use crate::protos::network::server_message::{Payload as ServerPayload};
use crate::protos::network::{ClientMessage, ClientPing, ClientSubscribe, ServerMessage};
use crate::util::default;
use crate::worker::board::Task;
use crate::{network, WebSocketStream};
use anyhow::{bail, Context};
use board::BoardState;
use futures::stream::{SplitSink, SplitStream};
use futures::{SinkExt, StreamExt};
use std::ops::ControlFlow;
use std::sync::Arc;
use std::time::Duration;
use tokio::sync::mpsc;
use tokio::task::JoinHandle;
use tokio::time::{interval, sleep, timeout};
use tokio::{select, spawn};
use tokio_websockets::Message;
use tracing::{info, warn};

pub struct SharedWorkerData {
    /// Shared board state. Also handles task generation.
    board_state: BoardState,

    /// The timeout when waiting for snapshots.
    timeout: Duration,
}

impl SharedWorkerData {
    pub fn new(timeout: Duration) -> Arc<SharedWorkerData> {
        Arc::new(SharedWorkerData {
            board_state: BoardState::new(),
            timeout,
        })
    }
}

/// Wrapper that manages a single worker socket.
struct Worker {
    shared: Arc<SharedWorkerData>,
}

impl Worker {
    // grab a task from the top of the work queue
    // subscribe to it
    // wait for a reply, if we don't get one within a second then resubscribe up to 5 times
    //   if we still don't get a reply, abandon the task
    // once we get a reply, update the data for all squares

    async fn reader_half(
        &self,
        mut stream: SplitStream<WebSocketStream>,
        tx: mpsc::Sender<ServerPayload>,
    ) -> anyhow::Result<()> {
        while let Some(result) = stream.next().await {
            match result {
                Ok(ws_message) => {
                    let Some(message) = parse_message::<ServerMessage>(ws_message)
                        .context("parsing websocket message")?
                    else {
                        continue;
                    };

                    match message.payload {
                        Some(ServerPayload::Pong(_)) => {}
                        Some(payload) => {
                            let send_result = tx.send(payload).await;

                            if send_result.is_err() {
                                break;
                            }
                        }
                        None => {}
                    }
                }

                Err(e) => return Err(e).context("websocket read error"),
            }
        }

        Ok(())
    }

    async fn writer_half(
        &self,
        mut sink: SplitSink<WebSocketStream, Message>,
        mut rx: mpsc::Receiver<ClientPayload>,
    ) -> anyhow::Result<()> {
        let mut interval = interval(Duration::from_secs(10));

        loop {
            let payload = select! { biased;
                payload = rx.recv() => {
                    let Some(payload) = payload else {
                        return Ok(());
                    };

                    payload
                },
                _ = interval.tick() => {
                    ClientPayload::Ping(ClientPing {
                        ..default()
                    })
                },
            };

            let message = ClientMessage {
                payload: Some(payload),
                ..default()
            };

            let ws_message = network::write_message(message).context("serializing message")?;
            sink.send(ws_message)
                .await
                .context("writing to websocket")?;
        }
    }

    async fn handle_task(
        &self,
        rx: &mut mpsc::Receiver<ServerPayload>,
        task: Task<'_>,
    ) -> anyhow::Result<ControlFlow<()>> {
        loop {
            let result = timeout(self.shared.timeout, rx.recv()).await;

            match result {
                Ok(None) => return Ok(ControlFlow::Break(())),
                Ok(Some(ServerPayload::InitialState(state))) => {
                    match state.snapshot.0 {
                        None => bail!("state snapshot is missing contents"),
                        Some(snapshot) => {
                            let x = snapshot.xCoord;
                            let y = snapshot.yCoord;

                            self.shared.board_state.handle_snapshot(*snapshot);

                            if x == task.center_x() && y == task.center_y() {
                                task.finish();
                            } else {
                                continue;
                            }
                        }
                    }
                }

                Ok(Some(ServerPayload::Snapshot(snapshot))) => {
                    let x = snapshot.xCoord;
                    let y = snapshot.yCoord;

                    self.shared.board_state.handle_snapshot(snapshot);

                    if x == task.center_x() && y == task.center_y() {
                        task.finish();
                    } else {
                        continue;
                    }
                }

                Ok(Some(ServerPayload::MovesAndCaptures(moves_and_captures))) => {
                    self.shared.board_state.handle_moves_and_captures(moves_and_captures);
                    continue;
                }

                Ok(_) => {},

                Err(_) => {
                    // requeue
                    task.abandon();
                }
            }

            return Ok(ControlFlow::Continue(()))
        }
    }

    async fn orchestration_task(
        &self,
        mut rx: mpsc::Receiver<ServerPayload>,
        tx: mpsc::Sender<ClientPayload>,
        origin_x: u32,
        origin_y: u32,
    ) -> anyhow::Result<()> {
        let task = self.shared.board_state.task_at(origin_x, origin_y);
        let flow = self.handle_task(&mut rx, task).await?;

        if flow.is_break() {
            return Ok(());
        }

        loop {
            let task = self.shared.board_state.get_task().await;

            if tx.send(ClientPayload::Subscribe(ClientSubscribe {
                centerX: task.center_x(),
                centerY: task.center_y(),
                ..default()
            })).await.is_err() {
                break;
            }

            let flow = self.handle_task(&mut rx, task).await?;

            if flow.is_break() {
                break;
            }
        }

        Ok(())
    }

    pub async fn run(&self, ws: WebSocketStream, x: u32, y: u32) -> anyhow::Result<()> {
        let (sink, stream) = ws.split();
        let (reader_tx, reader_rx) = mpsc::channel(8);
        let (writer_tx, writer_rx) = mpsc::channel(8);

        select! { biased;
            res = self.reader_half(stream, reader_tx) => {
                info!("reader done");
                res.context("socket writer")?
            }

            res = self.writer_half(sink, writer_rx) => {
                info!("writer done");
                res.context("socket reader")?;
            }

            res = self.orchestration_task(reader_rx, writer_tx, x, y) => {
                info!("orchestrator done");
                res.context("orchestration task")?;
            }
        }

        info!("worker done");
        Ok(())
    }
}

pub async fn start(
    shared: Arc<SharedWorkerData>,
    ws: WebSocketStream,
    x: u32,
    y: u32,
) -> JoinHandle<anyhow::Result<()>> {
    let worker = Worker { shared };
    spawn(async move { worker.run(ws, x, y).await })
}
