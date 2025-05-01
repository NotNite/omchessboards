use async_compression::tokio::{bufread::ZstdDecoder, write::ZstdEncoder};
use capnp::capability::Promise;
use capnp_rpc::{RpcSystem, rpc_twoparty_capnp::Side, twoparty::VatNetwork};
use chrono::{DateTime, NaiveDate, NaiveDateTime, Utc};
use dashmap::DashMap;
use firehorse_capnp::PieceType;
use flume::{Receiver, Sender};
use futures::{
    AsyncReadExt, TryFutureExt,
    io::{BufReader, BufWriter},
    try_join,
};
use image::RgbImage;
use rayon::iter::{IntoParallelRefIterator, ParallelIterator};
use redis::Commands;
use std::{
    collections::HashMap,
    hash::Hash,
    time::{Duration, SystemTime, UNIX_EPOCH},
};
use tokio::net::TcpStream;
use tokio_util::compat::{
    FuturesAsyncReadCompatExt, FuturesAsyncWriteCompatExt, TokioAsyncReadCompatExt,
    TokioAsyncWriteCompatExt,
};

use std::env;

use sqlx::{Database, FromRow, Postgres, Row};
use sqlx::{
    Pool,
    postgres::{PgPoolOptions, PgRow},
};

pub mod firehorse_capnp {
    include!(concat!(env!("OUT_DIR"), "/firehorse_capnp.rs"));
}

#[derive(PartialEq, Eq, Debug, Hash, Clone, Copy)]
struct Position(u16, u16);

struct SerializedPiece {
    id: u32,
    position: Position,
    r#type: PieceType,
    is_white: bool,
    move_count: u32,
    capture_count: u32,
    move_info: Option<SerializedMoveInfo>,
}

struct SerializedMoveInfo {
    seqnum: u64,
    x: u16,
    y: u16,
}

struct SerializedCapturedPiece {
    seqnum: u64,
    captured_piece_id: u32,
}

struct SerializedAuditAction {
    // pieces
    pieces: Vec<SerializedPiece>,
    // weird piece things
    captured_pieces: Vec<SerializedCapturedPiece>,
    adopted_piece_ids: Vec<u32>,
}

#[derive(Clone)]
struct Callback {
    tx: Sender<(Position, SerializedAuditAction)>,
}

impl Callback {
    pub fn new(tx: Sender<(Position, SerializedAuditAction)>) -> Self {
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

        let mut audit_action = SerializedAuditAction {
            pieces: Vec::new(),
            captured_pieces: Vec::new(),
            adopted_piece_ids: Vec::new(),
        };

        for piece in snapshot.get_pieces()? {
            let data = piece.get_data()?;
            let x = position.0 as i16 + piece.get_dx() as i16;
            let y = position.1 as i16 + piece.get_dy() as i16;

            audit_action.pieces.push(SerializedPiece {
                id: data.get_id(),
                position: Position(x as u16, y as u16),
                r#type: data.get_type()?,
                is_white: data.get_is_white(),
                move_count: data.get_move_count(),
                capture_count: data.get_capture_count(),
                move_info: None,
            });
        }

        self.tx.send((position, audit_action))?;
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

async fn poll(
    rx: Receiver<(Position, SerializedAuditAction)>,
    db: Pool<Postgres>,
    redis: r2d2::Pool<redis::Client>,
) -> eyre::Result<()> {
    let mut seen_pieces: DashMap<Position, u32> = DashMap::new();

    // try to load all the seen pieces from redis
    let mut redis_con = redis.get()?;

    println!("Starting load of all seen pieces");

    let x_keys: Vec<String> = redis_con.scan_match("chess:seen:*").unwrap().collect();

    x_keys.par_iter().for_each(|x_hash_key| {
        let mut x_redis_con = redis.get().unwrap();

        let values: HashMap<String, u32> = redis::cmd("HGETALL")
            .arg(&x_hash_key)
            .query(&mut x_redis_con)
            .unwrap();

        for (y_key, piece_id) in values {
            // convert y_key to u16
            let y_key = y_key.parse::<u16>().unwrap();

            // convert chess:piece:1234 -> 1234 to get the x_key
            let x_key = x_hash_key
                .as_str()
                .split(":")
                .last()
                .unwrap()
                .parse::<u16>()
                .unwrap();

            seen_pieces.insert(Position(x_key, y_key), piece_id);
        }
    });
    println!("Loaded {} seen pieces", seen_pieces.len());

    while let Ok((pos, action)) = rx.recv_async().await {
        // HACK(erin): sleep more if there's only 1 item in the channel as a last ditch saving measure
        if rx.len() == 1 {
            println!("BREAKGLASS: sleeping for 1000ms");
            async_std::task::sleep(Duration::from_millis(1000)).await;
        }

        let mut piece_ids: Vec<i64> = vec![/* ... */];
        let mut piece_type: Vec<i64> = vec![/* ... */];
        let mut is_white: Vec<bool> = vec![/*... */];
        let mut move_count: Vec<i64> = vec![/*... */];
        let mut capture_count: Vec<i64> = vec![/*... */];
        let mut pos_x: Vec<i64> = vec![/*... */];
        let mut pos_y: Vec<i64> = vec![/*... */];
        let mut insert_pipe = redis::pipe();

        for piece in action.pieces {
            // check if we've seen the x/y before
            match seen_pieces.get_mut(&piece.position) {
                Some(seen_id) => {
                    // Ignore the piece if we've seen it before
                    if piece.id == *seen_id {
                        continue;
                    }

                    // Update the piece in the seen pieces if not
                    seen_pieces.alter(&piece.position, |_, _| piece.id);
                }
                None => {
                    // Add the piece to the seen pieces
                    seen_pieces.insert(piece.position, piece.id);
                }
            }

            // insert the piece if we haven't
            piece_ids.push(piece.id as i64);
            piece_type.push(piece.r#type as i64);
            is_white.push(piece.is_white);
            move_count.push(piece.move_count as i64);
            capture_count.push(piece.capture_count as i64);
            pos_x.push(piece.position.0 as i64);
            pos_y.push(piece.position.1 as i64);

            // append to the redis pipe insertion
            insert_pipe
                .hset(
                    format!("chess:seen:{}", piece.position.0),
                    piece.position.1,
                    piece.id,
                )
                .ignore();
        }

        // ignore empty piece_id sets
        if piece_ids.is_empty() {
            println!("No pieces were inserted, remaining {}!", rx.len());
            // HACK(erin@): sleeping seems to buy more time before the socket dies. this needs to be looked into :(
            async_std::task::sleep(Duration::from_millis(100)).await;
            continue;
        }

        // update sql first
        sqlx::query!(
            "
            INSERT INTO chess_pieces(piece_id, piece_type, is_white, move_count, capture_count, pos_x, pos_y)
            SELECT * FROM UNNEST($1::int8[], $2::int8[], $3::bool[], $4::int8[], $5::int8[], $6::int8[], $7::int8[])
            ",
            &piece_ids[..],
            &piece_type[..],
            &is_white[..],
            &move_count[..],
            &capture_count[..],
            &pos_x[..],
            &pos_y[..],
        ).execute(&db).await?;
        println!("inserted {} pieces", piece_ids.len());

        // update redis next
        // TODO: how the heck do i actually do this properly :(
        let _: () = insert_pipe.query(&mut redis_con)?;
    }

    Ok(())
}

#[tokio::main]
async fn main() -> eyre::Result<()> {
    // connect the db first
    let db_dsn = env::var("DATABASE_URL")?;
    let db = PgPoolOptions::new()
        .max_connections(5)
        .connect(&db_dsn)
        .await?;

    let redis_client = redis::Client::open("redis://127.0.0.1/").unwrap();
    let redis_pool = r2d2::Pool::builder().build(redis_client).unwrap();

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
        poll(rx, db, redis_pool),
        system.map_err(|e| e.into()),
        listen.send().promise.map_err(|e| e.into()),
    )?;

    Ok(())
}
