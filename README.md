# omchessboards

insert witty description here

## usage/contributing

TypeScript and Deno. please format with [dprint](https://dprint.dev/). a proxy for inspecting network traffic is available (`deno run proxy` then add the `userscripts/proxy.user.js`).

## protocol notes

- WebSocket for live updates/commands + some HTTP JSON APIs that are polled for status
- protobuf 3, messages are dumped in `network.proto`
- some messages are zstd-compressed (detected by the first four bytes matching zstd magic)
- map is 8000x8000 spaces (1000x1000 boards with 8 spaces per board)

### connection basics

`wss://onemillionchessboards.com/ws`, query parameters are `x`/`y` (numbers) and `colorPref` (`white` or `black`)

on connecting, the server will immediately send a `ServerInitialState`. clients should send a `ClientPing` every 10 seconds and they'll get a `ServerPong` back (looks like not sending heartbeats gets you disconnected).

when scrolling around, the client sends `ClientSubscribe` to update the viewport and receive new events for that area. the initial location is determined by the query parameters in the websocket URL.

### moving

send a `ClientMove` with a random `moveToken`. the server will reply with a `ServerValidMove` or `ServerInvalidMove` depending on if the move went through. moves must be valid in chess rules (which I don't know shit about chess rules so glhf)

### snapshot and updates

server sends `ServerInitialState` when connecting for the first time. server sends `ServerMovesAndCaptures` when new moves/captures happen (from you and other players). when scrolling into new areas (TODO: how far?) server sends `ServerStateSnapshot` containing all pieces in the viewport.

TODO: `ServerAdoption` and `ServerBulkCapture`
