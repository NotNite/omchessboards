using System.Net;
using System.Net.WebSockets;
using System.Web;
using Chess;
using ZstdSharp;
using Google.Protobuf;

namespace Firehorse;

public class Connection : IDisposable {
    private static readonly byte[] ZstdMagic = [
        40,
        181,
        47,
        253
    ];

    private readonly ClientWebSocket ws;
    private readonly Decompressor zstd;
    private readonly Timer timer;

    public Connection(string username) {
        var proxy = new WebProxy("socks5://rose.host.katie.cat:1234") {
            Credentials = new NetworkCredential(username, "meow")
        };

        this.ws = new ClientWebSocket();
        this.ws.Options.Proxy = proxy;

        this.zstd = new Decompressor();

        this.timer = new Timer(this.OnTimer);
    }

    public async Task RunAsync(
        Func<ServerMessage, Task> handler,
        int x = 0, int y = 0, bool isWhite = false,
        CancellationToken cancellationToken = default
    ) {
        var uri = new UriBuilder("wss://onemillionchessboards.com/ws");

        // System.Web shit API moment
        var query = HttpUtility.ParseQueryString(string.Empty);
        query.Add("x", x.ToString());
        query.Add("y", y.ToString());
        query.Add("colorPref", isWhite ? "white" : "black");
        uri.Query = query.ToString();

        await this.ws.ConnectAsync(uri.Uri, cancellationToken);
        this.timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(10));

        var bytes = new byte[65536].AsMemory();
        while (this.ws.State is WebSocketState.Open && !cancellationToken.IsCancellationRequested) {
            var result = await this.ws.ReceiveAsync(bytes, cancellationToken);
            if (result.MessageType is WebSocketMessageType.Close) break;
            if (!result.EndOfMessage) throw new Exception("help message too big " + result.Count);

            var span = bytes[..result.Count].Span;
            var isZstd = result.Count > 4 && span[..4].SequenceEqual(ZstdMagic);

            if (isZstd) span = this.zstd.Unwrap(span);
            var message = ServerMessage.Parser.ParseFrom(span);

            // FIXME this sucks
            try {
                await handler(message);
            } catch {
                // Console.WriteLine(e);
            }
        }

        await this.ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
    }

    public async Task SendMessageAsync(ClientMessage message, CancellationToken cancellationToken = default) {
        if (this.ws.State is not WebSocketState.Open) return;

        var size = message.CalculateSize();
        var buf = new byte[size].AsMemory();
        message.WriteTo(buf.Span);

        await this.ws.SendAsync(buf,
            WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage,
            cancellationToken);
    }

    private void OnTimer(object? state) {
        _ = this.SendMessageAsync(new ClientMessage() {
            Ping = new ClientPing()
        });
    }

    public void Dispose() {
        this.timer.Dispose();
        this.zstd.Dispose();
        this.ws.Dispose();
        GC.SuppressFinalize(this);
    }
}
