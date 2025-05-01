use async_compression::tokio::{bufread::ZstdDecoder, write::ZstdEncoder};
use capnp::capability::Promise;
use capnp_rpc::{RpcSystem, rpc_twoparty_capnp::Side, twoparty::VatNetwork};
use firehorse_capnp::PieceType;
use flume::{Receiver, Sender};
use futures::{
    AsyncReadExt, TryFutureExt,
    io::{BufReader, BufWriter},
    try_join,
};
use image::RgbImage;
use std::time::{SystemTime, UNIX_EPOCH};
use tokio::net::TcpStream;
use tokio_util::compat::{
    FuturesAsyncReadCompatExt, FuturesAsyncWriteCompatExt, TokioAsyncReadCompatExt,
    TokioAsyncWriteCompatExt,
};

pub mod firehorse_capnp {
    include!(concat!(env!("OUT_DIR"), "/firehorse_capnp.rs"));
}

#[derive(PartialEq, Eq, Debug)]
struct Position(u16, u16);

struct SerializedPiece {
    position: Position,
    r#type: PieceType,
    is_white: bool,
}

#[derive(Clone)]
struct Callback {
    tx: Sender<(Position, Vec<SerializedPiece>)>,
}

impl Callback {
    pub fn new(tx: Sender<(Position, Vec<SerializedPiece>)>) -> Self {
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
        let position = Position(snapshot.get_x(), snapshot.get_y());

        let mut pieces = Vec::new();
        for piece in snapshot.get_pieces()? {
            let data = piece.get_data()?;
            let x = position.0 as i16 + piece.get_dx() as i16;
            let y = position.1 as i16 + piece.get_dy() as i16;

            pieces.push(SerializedPiece {
                position: Position(x as u16, y as u16),
                r#type: data.get_type()?,
                is_white: data.get_is_white(),
            });
        }

        self.tx.send((position, pieces))?;
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

fn create_axis() -> Vec<u16> {
    // basically exactly matches the logic in Util.cs
    let mut result = Vec::new();

    const BOARD_SIZE: u16 = 8;
    const BOARDS_PER_AXIS: u16 = 1000;
    const MAP_SIZE: u16 = BOARDS_PER_AXIS * BOARD_SIZE;

    const SUBSCRIPTION_SIZE: u16 = 94;
    const HALF_SUBSCRIPTION_SIZE: u16 = SUBSCRIPTION_SIZE / 2;
    const MAX_SUBSCRIPTION: u16 = MAP_SIZE - 1;

    let mut i = HALF_SUBSCRIPTION_SIZE;
    while i < MAP_SIZE {
        result.push(i);
        i += SUBSCRIPTION_SIZE;
    }

    result.push(MAX_SUBSCRIPTION);

    result
}

fn create_positions() -> Vec<Position> {
    let mut result = Vec::new();

    for y in create_axis() {
        for x in create_axis() {
            result.push(Position(x, y));
        }
    }

    result
}

async fn poll(rx: Receiver<(Position, Vec<SerializedPiece>)>) -> eyre::Result<()> {
    let mut white = RgbImage::new(8000, 8000);
    let mut black = RgbImage::new(8000, 8000);

    for pixel in white.pixels_mut().chain(black.pixels_mut()) {
        // color picked from the site
        pixel.0[0] = 61;
        pixel.0[1] = 75;
        pixel.0[2] = 95;
    }

    let mut positions = create_positions();

    let start = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .expect("time went backwards");

    while let Ok((pos, pieces)) = rx.recv_async().await {
        for piece in pieces {
            let image = if piece.is_white {
                &mut white
            } else {
                &mut black
            };

            // colors chosen at complete random
            let color = match piece.r#type {
                PieceType::Pawn => (0, 0, 0),
                PieceType::Knight => (255, 0, 0),
                PieceType::Bishop => (0, 255, 0),
                PieceType::Rook => (0, 0, 255),
                PieceType::Queen => (255, 0, 255),
                PieceType::King => (255, 255, 255),
                PieceType::PromotedPawn => (255, 255, 0),
            };

            let pixel = image.get_pixel_mut(piece.position.0 as u32, piece.position.1 as u32);
            pixel.0[0] = color.0;
            pixel.0[1] = color.1;
            pixel.0[2] = color.2;
        }

        if let Some(idx) = positions.iter().position(|p| *p == pos) {
            positions.remove(idx);

            let size = positions.len();
            if size == 0 {
                let now = SystemTime::now()
                    .duration_since(UNIX_EPOCH)
                    .expect("time went backwards");

                let elapsed = now - start;
                println!("indexed board in {}", elapsed.as_secs_f32());

                white.save("white.png")?;
                black.save("black.png")?;
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
