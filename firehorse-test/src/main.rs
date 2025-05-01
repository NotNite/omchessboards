use async_compression::tokio::{bufread::ZstdDecoder, write::ZstdEncoder};
use capnp::capability::Promise;
use capnp_rpc::{RpcSystem, rpc_twoparty_capnp::Side, twoparty::VatNetwork};
use flume::{Receiver, Sender};
use futures::{
    AsyncReadExt, TryFutureExt,
    io::{BufReader, BufWriter},
    try_join,
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

// I'm sure there's a way to From<> this but I'm more rustier than this programming language's name
fn worlds_most_dogshit_type_award<T>(result: eyre::Result<T>) -> Promise<T, capnp::Error> {
    match result {
        Ok(value) => Promise::ok(value),
        Err(e) => Promise::err(capnp::Error::failed(e.to_string())),
    }
}

impl Callback {
    fn handle_snapshot<'a>(
        &mut self,
        params: firehorse_capnp::callback::OnSnapshotParams,
    ) -> eyre::Result<()> {
        let snapshot = params.get()?.get_snapshot()?;
        self.tx.send((snapshot.get_x(), snapshot.get_y()))?;
        Ok(())
    }

    fn handle_pieces_moved<'a>(
        &mut self,
        params: firehorse_capnp::callback::OnPiecesMovedParams,
    ) -> eyre::Result<()> {
        let moves = params.get()?.get_moves()?;
        for r#move in moves {
            let data = r#move.get_data()?;
            println!(
                "piece {} moved at {} {}",
                data.get_id(),
                r#move.get_x(),
                r#move.get_y()
            );
        }

        Ok(())
    }

    fn handle_pieces_captured<'a>(
        &mut self,
        params: firehorse_capnp::callback::OnPiecesCapturedParams,
    ) -> eyre::Result<()> {
        let _captures = params.get()?.get_captures()?;
        // TODO
        Ok(())
    }

    fn handle_pieces_adopted<'a>(
        &mut self,
        params: firehorse_capnp::callback::OnPiecesAdoptedParams,
    ) -> eyre::Result<()> {
        let _ids = params.get()?.get_ids()?;
        // TODO
        Ok(())
    }
}

impl firehorse_capnp::callback::Server for Callback {
    fn on_snapshot(
        &mut self,
        params: firehorse_capnp::callback::OnSnapshotParams,
        _: firehorse_capnp::callback::OnSnapshotResults,
    ) -> capnp::capability::Promise<(), capnp::Error> {
        worlds_most_dogshit_type_award(self.handle_snapshot(params))
    }

    fn on_pieces_moved(
        &mut self,
        params: firehorse_capnp::callback::OnPiecesMovedParams,
        _: firehorse_capnp::callback::OnPiecesMovedResults,
    ) -> capnp::capability::Promise<(), capnp::Error> {
        worlds_most_dogshit_type_award(self.handle_pieces_moved(params))
    }

    fn on_pieces_captured(
        &mut self,
        params: firehorse_capnp::callback::OnPiecesCapturedParams,
        _: firehorse_capnp::callback::OnPiecesCapturedResults,
    ) -> capnp::capability::Promise<(), capnp::Error> {
        worlds_most_dogshit_type_award(self.handle_pieces_captured(params))
    }

    fn on_pieces_adopted(
        &mut self,
        params: firehorse_capnp::callback::OnPiecesAdoptedParams,
        _: firehorse_capnp::callback::OnPiecesAdoptedResults,
    ) -> capnp::capability::Promise<(), capnp::Error> {
        worlds_most_dogshit_type_award(self.handle_pieces_adopted(params))
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
                println!("indexed board in {}", elapsed.as_secs_f32());
            }

            if size % 100 == 0 {
                println!("{} positions left", size);
            }
        }
    }

    Ok(())
}

#[tokio::main]
async fn main() -> eyre::Result<()> {
    let stream = TcpStream::connect("127.0.0.1:42069").await?;
    println!("connected");

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
    println!("obtained client");

    let (tx, rx) = flume::unbounded();

    let callback = Callback::new(tx);
    let callback_client = capnp_rpc::new_client(callback);

    let mut listen = client.listen_request();
    listen.get().set_callback(callback_client);

    try_join!(
        poll(rx),
        system.map_err(|e| e.into()),
        listen.send().promise.map_err(|e| e.into()),
    )?;

    Ok(())
}

/*let mut move_sequential = client.move_sequential_request();
let mut move_sequential_moves = move_sequential.get().init_moves(4);
for i in 0..move_sequential_moves.len() {
    let mut r#move = move_sequential_moves.reborrow().get(i as u32);

    let id = 14634993;
    let x = 3656;
    let y = 2745;

    r#move.set_id(id);
    r#move.set_move_type(firehorse_capnp::MoveType::Normal);
    r#move.set_piece_is_white(false);

    r#move.set_from_x(x);
    r#move.set_to_x(x);

    r#move.set_from_y(y + i as u16);
    r#move.set_to_y(y + i as u16 + 1);
}

let mut move_parallel = client.move_parallel_request();
let mut move_parallel_moves = move_parallel.get().init_moves(8);
for i in 0..move_parallel_moves.len() {
    let mut r#move = move_parallel_moves.reborrow().get(i as u32);

    let id = 14602993 + i;
    let x = 3648 + i as u16;
    let y = 2745;

    r#move.set_id(id);
    r#move.set_move_type(firehorse_capnp::MoveType::Normal);
    r#move.set_piece_is_white(false);

    r#move.set_from_x(x);
    r#move.set_to_x(x);

    r#move.set_from_y(y);
    r#move.set_to_y(y + 1);
}*/

/*move_sequential
    .send()
    .promise
    .map_ok(|d| {
        if let Ok(d) = d.get() {
            if d.get_success() {
                println!("move_sequential success!");
            } else {
                println!("move_sequential failed at {} :(", d.get_failed_at());
            }
        }
    })
    .map_err(|e| e.into()),
move_parallel
    .send()
    .promise
    .map_ok(|d| {
        if let Ok(d) = d.get() {
            if d.get_success() {
                println!("move_parallel success!");
            } else {
                let failed = if let Ok(failed) = d.get_failed() {
                    failed.iter().map(|i| i).collect::<Vec<_>>()
                } else {
                    Vec::new()
                };

                println!("move_parallel failed {:?} :(", failed);
            }
        }
    })
    .map_err(|e| e.into()),*/
