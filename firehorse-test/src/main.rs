use async_compression::tokio::{bufread::ZstdDecoder, write::ZstdEncoder};
use capnp::capability::Promise;
use capnp_rpc::{RpcSystem, pry, rpc_twoparty_capnp::Side, twoparty::VatNetwork};
use flume::{Receiver, Sender};
use futures::{
    AsyncReadExt, TryFutureExt,
    future::try_join3,
    io::{BufReader, BufWriter},
};
use std::time::{SystemTime, UNIX_EPOCH};
use tokio::net::TcpStream;
use tokio_util::compat::{
    FuturesAsyncReadCompatExt, FuturesAsyncWriteCompatExt, TokioAsyncReadCompatExt,
    TokioAsyncWriteCompatExt,
};

pub mod firehorse_capnp {
    include!(concat!(env!("OUT_DIR"), "/firehorse_capnp.rs"));
}

#[derive(Clone)]
pub struct Callback {
    tx: Sender<(u16, u16)>,
}

impl Callback {
    pub fn new(tx: Sender<(u16, u16)>) -> Self {
        Callback { tx }
    }
}

impl firehorse_capnp::callback::Server for Callback {
    fn on_snapshot(
        &mut self,
        params: firehorse_capnp::callback::OnSnapshotParams,
        _: firehorse_capnp::callback::OnSnapshotResults,
    ) -> capnp::capability::Promise<(), capnp::Error> {
        let snapshot = pry!(pry!(params.get()).get_snapshot());
        self.tx.send((snapshot.get_x(), snapshot.get_y())).ok();
        Promise::ok(())
    }
}

async fn poll(rx: Receiver<(u16, u16)>) -> eyre::Result<()> {
    const BOARD_SIZE: usize = 8;
    const BOARDS_PER_AXIS: usize = 1000;
    const MAP_SIZE: usize = BOARDS_PER_AXIS * BOARD_SIZE;

    const SUBSCRIPTION_SIZE: usize = 96;
    const HALF_SUBSCRIPTION_SIZE: usize = SUBSCRIPTION_SIZE / 2;
    const END: usize = MAP_SIZE - HALF_SUBSCRIPTION_SIZE;

    let mut positions = Vec::new();

    for y in (HALF_SUBSCRIPTION_SIZE..END).step_by(SUBSCRIPTION_SIZE) {
        for x in (HALF_SUBSCRIPTION_SIZE..END).step_by(SUBSCRIPTION_SIZE) {
            positions.push((x as u16, y as u16));
        }
    }

    let start = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .expect("time went backwards");

    while let Ok((x, y)) = rx.recv_async().await {
        if let Some(idx) = positions.iter().position(|p| p.0 == x && p.1 == y) {
            positions.remove(idx);

            let size = positions.len();
            if size == 0 {
                let now = SystemTime::now()
                    .duration_since(UNIX_EPOCH)
                    .expect("time went backwards");

                let elapsed = now - start;
                println!("{}", elapsed.as_secs_f32());
            }

            if size % 100 == 0 {
                println!("{}", size);
            }
        }
    }

    Ok(())
}

#[tokio::main]
async fn main() -> eyre::Result<()> {
    let stream = TcpStream::connect("127.0.0.1:42069").await?;

    let (reader, writer) = tokio_util::compat::TokioAsyncReadCompatExt::compat(stream).split();

    // please do not ask
    let reader = BufReader::new(reader);
    let reader = ZstdDecoder::new(FuturesAsyncReadCompatExt::compat(reader));
    let reader = BufReader::new(TokioAsyncReadCompatExt::compat(reader));

    let writer = BufWriter::new(writer);
    let writer = ZstdEncoder::new(FuturesAsyncWriteCompatExt::compat_write(writer));
    let writer = BufWriter::new(TokioAsyncWriteCompatExt::compat_write(writer));

    let network = Box::new(VatNetwork::new(
        reader,
        writer,
        Side::Client,
        Default::default(),
    ));
    let mut system = RpcSystem::new(network, None);

    let client: firehorse_capnp::firehorse::Client = system.bootstrap(Side::Server);

    let (tx, rx) = flume::unbounded();

    let callback = Callback::new(tx);
    let callback_client = capnp_rpc::new_client(callback);

    let mut request = client.listen_request();
    request.get().set_callback(callback_client);

    try_join3(
        poll(rx),
        system.map_err(|e| e.into()),
        request.send().promise.map_err(|e| e.into()),
    )
    .await?;

    Ok(())
}
