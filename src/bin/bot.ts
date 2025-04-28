import { decode } from "../lib.ts";
import { ServerMessage } from "../proto/network.ts";

// TODO: investigate /api that gets polled by the web UI

const url = new URL("wss://onemillionchessboards.com/ws");
url.searchParams.append("x", "0");
url.searchParams.append("y", "0");
url.searchParams.append("colorPref", "black");

const ws = new WebSocket(url);

function handle(message: ServerMessage) {
  const payload = message.payload!; // why is this nullable

  switch (payload.$case) {
    case "initialState": {
      console.log(payload.value);
      break;
    }

    default: {
      console.log("Unknown data:", payload.$case, payload.value);
    }
  }
}

ws.addEventListener("open", () => {
  console.log("Opened!");
});

ws.addEventListener("close", () => {
  console.log("Closed :(");
});

ws.addEventListener("error", (err) => {
  console.error(err);
});

ws.addEventListener("message", async (data) => {
  const message = await decode(data, ServerMessage);
  handle(message);
});
