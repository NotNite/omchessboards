# omchessboards

insert witty description here

## contributing rules

please don't push onto other people's branches (and you should probably name your branches to be safe).

when contributing to the TypeScript parts, please format with [dprint](https://dprint.dev/). when contributing to the C# parts, please respect the .editorconfig. I don't really care if you don't though.

## bots & proxies

`src/` contains some stuff written in [Deno](https://deno.com/). clone this repository and install dependencies (`deno install`).

for the bot, run `deno run bot`. for the proxy, run `deno run proxy` and add `userscripts/proxy.user.js` to your favorite userscript manager in the browser.

## firehorse

C# relay server for the board. scrapes the board and outputs raw snapshots, no other processing/storage is done. connects a shit ton of clients (using a provided HTTP proxy).

outputs over TCP (compressed with zstd). format is Cap’n Proto, see `firehorse/firehorse.capnp` for the schema.

Protobuf and Cap'n Proto need to be installed on your system for the schema codegen to work properly.

configure via environment variables:

- `FIREHORSE_PROXY_URL`: optional proxy to connect to (e.g. `socks5://whatever` or `http://whatever`)
- `FIREHORSE_PROXY_USERNAME`: optional username for the proxy
- `FIREHORSE_PROXY_PASSWORD`: optional password for the proxy
- `FIREHORSE_NUM_CONNECTIONS`: number of scrapers to create
- `FIREHORSE_HOST`: host to listen on (e.g. `127.0.0.1:42069`)

## protocol notes

- WebSocket for live updates/commands + some HTTP JSON APIs that are polled for status (TODO: investigate HTTP APIs)
- WebSocket is Protobuf v3, messages are dumped in `network.proto`
- some messages are zstd-compressed (detected by the first four bytes matching zstd magic)
- map is 8000x8000 spaces (1000x1000 boards with 8 spaces per board)

### connection basics

`wss://onemillionchessboards.com/ws`, query parameters are `x`/`y` (numbers) and `colorPref` (`white` or `black`)

on connecting, the server will immediately send a `ServerInitialState`. clients should send a `ClientPing` every 10 seconds and they'll get a `ServerPong` back (looks like not sending heartbeats gets you disconnected).

when scrolling around, the client sends `ClientSubscribe` to update the viewport and receive new events for that area. the initial location is determined by the query parameters in the websocket URL.

### moving

send a `ClientMove` with a random `moveToken`. the server will reply with a `ServerValidMove` or `ServerInvalidMove` with the corresponding move token, depending on if the move went through.

notes:

- **you can move pieces in areas you aren't subscribed to**, which is exciting for botting purposes
- **the current position of the piece must be accurate**
- move token can be anything but it helps associate the result of moves, kinda like a seq in some networking
- moves must be valid in chess rules (which I don't know shit about chess rules so glhf)

### snapshot and updates

server sends `ServerInitialState` when connecting for the first time. server sends `ServerMovesAndCaptures` when new moves/captures happen (from you and other players). when scrolling into new areas (TODO: how far?) server sends `ServerStateSnapshot` containing all pieces in the viewport.

TODO: `ServerAdoption` and `ServerBulkCapture`
