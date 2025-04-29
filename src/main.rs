mod protos;
mod proxy;
mod util;
mod network;
mod worker;

use crate::protos::network::{PieceDataForMove, PieceDataForSnapshot, PieceDataShared};
use anyhow::{anyhow, Context};
use futures::prelude::*;
use futures::stream::FuturesUnordered;
use proxy::Proxy;
use std::borrow::Cow;
use std::net::SocketAddr;
use std::sync::Arc;
use std::time::Duration;
use tokio::net::TcpStream;
use tokio::sync::mpsc;
use tokio::task::JoinError;
use tokio::time::sleep;
use tokio::{select, spawn};
use tokio_rustls::client::TlsStream;
use tokio_rustls::rustls::pki_types::ServerName;
use tokio_rustls::rustls::{ClientConfig, RootCertStore};
use tokio_rustls::TlsConnector;
use tokio_websockets::ClientBuilder;
use tracing::{error, info};
use url::Url;
use crate::worker::SharedWorkerData;

pub const TARGET_SERVER_NAME: &'static str = "onemillionchessboards.com";
pub const TARGET_DOMAIN_PORT: &'static str = "onemillionchessboards.com:443";

// pub const PROXY_ADDRESS: &'static str = "rose.host.katie.cat:1234";
// pub const PROXY_PASSWORD: &'static str = "meow";
// pub const PROXY_SUFFIX_LEN: usize = 48;

pub const PROXY_ADDRESS: &'static str = "109.236.82.42:9999";
pub const PROXY_PASSWORD: &'static str = "LBTOEcWKTJJl1tff";
pub const PROXY_SUFFIX_LEN: usize = 48;

/// Configuration data shared between all websocket tasks.
pub struct WebSocketData {
    server_name: ServerName<'static>,
    connector: TlsConnector,
    target: SocketAddr,
    proxy: Proxy,
}

impl WebSocketData {
    /// Creates a web socket stream using a randomly generated proxy.
    pub async fn create_websocket(&self, x: u32, y: u32) -> anyhow::Result<WebSocketStream> {
        let stream = self
            .proxy
            .connect(self.target)
            .await
            .context("connecting to target")?;

        let stream = self
            .connector
            .connect(self.server_name.clone(), stream)
            .await
            .context("tls setup")?;

        let uri = Url::parse_with_params(
            "wss://onemillionchessboards.com/ws",
            [
                ("x", Cow::from(x.to_string())),
                ("y", Cow::from(y.to_string())),
                ("colorPref", Cow::from("white")),
            ]
                .into_iter(),
        )
            .context("parsing websocket url")?;

        let (ws, _response) = ClientBuilder::new()
            .uri(uri.as_str())
            .expect("uri.as_str() returned invalid URI")
            .connect_on(stream)
            .await
            .context("websocket setup")?;

        Ok(ws)
    }
}

type WebSocketStream = tokio_websockets::WebSocketStream<TlsStream<TcpStream>>;

async fn setup_proxy() -> anyhow::Result<Proxy> {
    let proxy_address = tokio::net::lookup_host(PROXY_ADDRESS)
        .await
        .context("resolving IP")?
        .next()
        .ok_or_else(|| anyhow!("resolving IP"))?;

    let proxy = Proxy::new(proxy_address, PROXY_PASSWORD, PROXY_SUFFIX_LEN);
    Ok(proxy)
}

/// Represents an update to a piece.
#[derive(Debug)]
pub struct PieceUpdate {
    x: u32,
    y: u32,
    piece: PieceDataShared,
}

impl PieceUpdate {
    pub fn from_snapshot(
        x: u32,
        y: u32,
        snapshot: PieceDataForSnapshot,
    ) -> anyhow::Result<PieceUpdate> {
        let x = x
            .checked_add_signed(snapshot.dx)
            .ok_or_else(|| anyhow!("server sent invalid snapshot dx"))?;
        let y = y
            .checked_add_signed(snapshot.dy)
            .ok_or_else(|| anyhow!("server sent invalid snapshot dy"))?;
        let piece = snapshot.piece.get_or_default().clone();

        Ok(PieceUpdate { x, y, piece })
    }
}

impl From<PieceDataForMove> for PieceUpdate {
    fn from(data: PieceDataForMove) -> Self {
        PieceUpdate {
            x: data.x,
            y: data.y,
            piece: data.piece.get_or_default().clone(),
        }
    }
}

async fn create_ws_data() -> anyhow::Result<WebSocketData> {
    let mut certs = RootCertStore::empty();
    certs.extend(webpki_roots::TLS_SERVER_ROOTS.iter().cloned());

    let tls_config = ClientConfig::builder()
        .with_root_certificates(certs)
        .with_no_client_auth();

    let connector = TlsConnector::from(Arc::new(tls_config));
    let server_name = ServerName::try_from(TARGET_SERVER_NAME).context("invalid target")?;

    let target_address = tokio::net::lookup_host(TARGET_DOMAIN_PORT)
        .await
        .context("resolving target")?
        .next()
        .ok_or_else(|| anyhow!("target has no IP"))?;

    let proxy = setup_proxy().await.context("setting up proxy")?;

    Ok(WebSocketData {
        server_name,
        connector,
        target: target_address,
        proxy,
    })
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    tracing_subscriber::fmt::init();

    let ws_data = Arc::new(create_ws_data().await?);

    let worker_data = SharedWorkerData::new(Duration::from_millis(2000));

    let (tasks_tx, mut tasks_rx) = mpsc::channel(1);
    tasks_tx.send(spawn(async move {
        sleep(Duration::from_secs(100000)).await;
        Ok(())
    })).await?;

    let watcher = spawn(async move {
        let mut tasks = FuturesUnordered::new();

        loop {
            select! {
                task = tasks_rx.recv(), if !tasks_rx.is_closed() => {
                    if let Some(task) = task {
                        tasks.push(task)
                    }
                },
                option = tasks.next(), if !tasks.is_empty() => {
                    let Some(result): Option<Result<anyhow::Result<()>, JoinError>> = option else { continue };

                    if let Err(err) = result.context("task panicked").and_then(|r| r) {
                        error!(?err, "task error");
                    }
                },
                else => {
                    info!("all tasks finished");
                    break
                },
            }
        }
    });

    let count = 250;
    for i in 0..count {
        let x = i * 96;
        let y = 0;

        let task = spawn({
            let worker_data = worker_data.clone();
            let ws_data = ws_data.clone();

            async move {
                let ws = ws_data.create_websocket(x, y).await.context("creating websocket")?;
                let task = worker::start(worker_data.clone(), ws, x, y).await;
                task.await.context("task panicked")?.context("worker task")?;
                Ok(())
            }
        });

        tasks_tx.send(task).await?;
    }

    info!("opened {} sockets", count);

    watcher.await?;
    Ok(())
}