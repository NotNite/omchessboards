use anyhow::{bail, Context};
use bytes::BytesMut;
use protobuf::Message as ProtobufMessage;
use std::io::{Cursor, Read};
use tokio_websockets::Message;
pub const ZSTD_MAGIC: [u8; 4] = [40, 181, 47, 253];

pub fn parse_message<T: ProtobufMessage>(message: Message) -> anyhow::Result<Option<T>> {
    if message.is_ping() {
        return Ok(None); // tungstenite handles pings
    }

    if !message.is_binary() {
        bail!("unexpected message type on websocket");
    }

    let mut buffer = BytesMut::from(message.into_payload());

    let decompressed_buffer = if buffer.len() >= 4 && &buffer[0..4] == ZSTD_MAGIC {
        let mut cursor = Cursor::new(&mut buffer);
        let mut decoder =
            zstd::Decoder::with_buffer(&mut cursor).context("failed to create decoder")?;

        let mut decompressed_buffer = Vec::new();
        decoder
            .read_to_end(&mut decompressed_buffer)
            .context("failed to decode zstd stream")?;

        decompressed_buffer
    } else {
        Vec::from(buffer)
    };

    let server_message = T::parse_from_bytes(&decompressed_buffer[..])
        .context("failed to parse message")?;

    Ok(Some(server_message))
}

pub fn write_message<T: ProtobufMessage>(message: T) -> anyhow::Result<Message> {
    let buffer = message.write_to_bytes()
        .context("failed to write message")?;

    // TODO: zstd

    Ok(Message::binary(buffer))
}