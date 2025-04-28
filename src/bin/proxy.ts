import { decode, encode } from "../lib.ts";
import { ClientMessage, MessageFns, ServerMessage } from "../proto/network.ts";

function waitForConnect(socket: WebSocket) {
  return new Promise<void>((resolve, reject) => {
    const openListener = () => {
      socket.removeEventListener("open", openListener);
      resolve();
    };

    const errorListener = (e: Event) => {
      socket.removeEventListener("error", errorListener);
      console.error(e);
      reject(e);
    };

    socket.addEventListener("open", openListener);
    socket.addEventListener("error", errorListener);
  });
}

function waitForSend<T>(socket: WebSocket, promise: Promise<void>, message: T, fns: MessageFns<T>) {
  const bytes = encode(message, fns);
  promise.then(() => socket.send(bytes));
}

// https://docs.deno.com/examples/http_server_websocket/
Deno.serve((req) => {
  if (req.headers.get("upgrade") != "websocket") {
    return new Response(null, { status: 501 });
  }

  // copy query params
  const reqUrl = new URL(req.url);
  const proxyUrl = new URL("wss://onemillionchessboards.com/ws");
  for (const [key, value] of reqUrl.searchParams.entries()) {
    proxyUrl.searchParams.append(key, value);
  }

  const { socket, response } = Deno.upgradeWebSocket(req);
  const socketConnect = waitForConnect(socket);

  const upstream = new WebSocket(proxyUrl);
  const upstreamConnect = waitForConnect(upstream);

  upstream.addEventListener("message", async (event) => {
    const message = await decode(event, ServerMessage);
    console.log("server -> client", message);
    waitForSend(socket, socketConnect, message, ServerMessage);
  });

  socket.addEventListener("message", async (event) => {
    const message = await decode(event, ClientMessage);
    console.log("client -> server", message);
    waitForSend(upstream, upstreamConnect, message, ClientMessage);
  });

  function handleError(e: Event) {
    console.error(e);

    upstream.close();
    socket.close();
  }

  upstream.addEventListener("error", handleError);
  socket.addEventListener("error", handleError);

  return response;
});
