using System.Collections.Immutable;
using System.Net;
using System.Net.WebSockets;
using System.Web;
using CapnpGen;
using Chess;
using Google.Protobuf;
using ZstdSharp;

namespace Firehorse.Chess;

public class Connection : IDisposable {
    private static readonly byte[] ZstdMagic = [
        40,
        181,
        47,
        253
    ];

    public bool Connected => this.ws.State is WebSocketState.Open;
    public readonly TaskCompletionSource<bool> IsWhite = new();

    private readonly SharedChannels channels;
    private readonly ClientWebSocket ws;
    private readonly Decompressor zstd;
    private readonly Timer timer;

    private readonly List<Func<ServerMessage, bool>> eventHandlers = [];
    private readonly SemaphoreSlim moveSemaphore = new(1);

    public Connection(IWebProxy? proxy, SharedChannels channels) {
        this.channels = channels;
        this.ws = new ClientWebSocket();
        this.ws.Options.Proxy = proxy;

        this.zstd = new Decompressor();
        this.timer = new Timer(this.OnTimer);
    }

    public async Task ConnectAsync(
        uint x = 0, uint y = 0, bool isWhite = false,
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
    }

    public async Task RunAsync(CancellationToken cancellationToken = default) {
        var bytes = new byte[65536].AsMemory();
        var zstdBytes = new byte[1024 * 1024].AsMemory();

        while (this.Connected && !cancellationToken.IsCancellationRequested) {
            var result = await this.ws.ReceiveAsync(bytes, cancellationToken);
            if (result.MessageType is WebSocketMessageType.Close) break;
            if (!result.EndOfMessage) throw new Exception("help message too big " + result.Count);

            var span = bytes[..result.Count].Span;
            var isZstd = result.Count > 4 && span[..4].SequenceEqual(ZstdMagic);

            // this allocs a shit ton by the way
            ServerMessage message;
            if (isZstd) {
                var len = this.zstd.Unwrap(span, zstdBytes.Span);
                message = ServerMessage.Parser.ParseFrom(zstdBytes[..len].Span);
            } else {
                message = ServerMessage.Parser.ParseFrom(span);
            }

            this.HandleMessage(message);
        }
    }

    private void HandleMessage(ServerMessage message) {
        // This is a weird place to have this here for abstraction but it's easier than dealing with events
        switch (message.PayloadCase) {
            case ServerMessage.PayloadOneofCase.InitialState: {
                this.IsWhite.SetResult(message.InitialState.PlayingWhite);
                break;
            }

            case ServerMessage.PayloadOneofCase.MovesAndCaptures: {
                var moves = message.MovesAndCaptures.Moves;
                if (moves.Count != 0) {
                    var result = moves
                                .Select(Util.ConvertMovePieceToFirehorse)
                                .ToImmutableList();
                    this.channels.MoveRelay.Writer.TryWrite(result);
                }

                var captures = message.MovesAndCaptures.Captures;
                if (captures.Count != 0) {
                    var result = captures
                                .Select(Util.ConvertPieceCaptureToFirehorse)
                                .ToImmutableList();
                    this.channels.CaptureRelay.Writer.TryWrite(result);
                }

                break;
            }

            case ServerMessage.PayloadOneofCase.BulkCapture: {
                var payload = message.BulkCapture;
                var result = payload.CapturedIds
                                    .Select(id => new MoveResult() {
                                         // yes I'm duplicating seqnum here every time. who gives a shit
                                         Seqnum = payload.Seqnum,
                                         CapturedPieceId = id
                                     })
                                    .ToImmutableList();
                this.channels.CaptureRelay.Writer.TryWrite(result);
                break;
            }

            case ServerMessage.PayloadOneofCase.Adoption: {
                this.channels.AdoptRelay.Writer.TryWrite(message.Adoption.AdoptedIds);
                break;
            }
        }

        // lock contention isn't real and can't hurt you
        lock (this.eventHandlers) {
            // this is technically O(n) iirc but it saves the .ToList() alloc so w/e
            for (var i = this.eventHandlers.Count - 1; i >= 0; i--) {
                var predicate = this.eventHandlers[i];
                if (predicate(message)) this.eventHandlers.RemoveAt(i);
            }
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default) {
        this.timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        if (this.Connected) await this.ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
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

    private Task<ServerMessage> WaitForMessageAsync(
        Func<ServerMessage, bool> predicate,
        CancellationToken cancellationToken = default
    ) {
        var task = new TaskCompletionSource<ServerMessage>();

        lock (this.eventHandlers) this.eventHandlers.Add(WrappedPredicate);

        // ReSharper disable once MethodSupportsCancellation
        return task.Task.WaitAsync(cancellationToken).ContinueWith(t => {
            if (!t.IsCompletedSuccessfully) {
                lock (this.eventHandlers) this.eventHandlers.Remove(WrappedPredicate);
            }

            return t;
        }).Unwrap();

        bool WrappedPredicate(ServerMessage msg) {
            if (predicate(msg)) {
                task.SetResult(msg);
                return true;
            } else {
                return false;
            }
        }
    }

    private async Task<ServerMessage> SendAndReceiveAsync(
        ClientMessage message,
        Func<ServerMessage, bool> predicate,
        CancellationToken cancellationToken = default
    ) {
        var task = this.WaitForMessageAsync(predicate, cancellationToken);
        await this.SendMessageAsync(message, cancellationToken);
        return await task;
    }

    public async Task<ServerValidMove> MakeMoveAsync(
        ClientMove move,
        CancellationToken cancellationToken = default
    ) {
        using var cts = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
        cts.CancelAfter(TimeSpan.FromSeconds(1));

        var moveToken = unchecked((uint) Random.Shared.Next());
        move.MoveToken = moveToken;

        await this.moveSemaphore.WaitAsync(linked.Token);
        try {
            var msg = await this.SendAndReceiveAsync(
                new ClientMessage() {
                    Move = move
                },
                (msg) =>
                    (msg.PayloadCase is ServerMessage.PayloadOneofCase.ValidMove
                     && msg.ValidMove.MoveToken == moveToken)
                    || (msg.PayloadCase is ServerMessage.PayloadOneofCase.InvalidMove
                        && msg.InvalidMove.MoveToken == moveToken),
                linked.Token
            );

            if (msg.PayloadCase is ServerMessage.PayloadOneofCase.InvalidMove) throw new InvalidMoveException(move);

            return msg.ValidMove;
        } finally {
            this.moveSemaphore.Release();
        }
    }

    public async Task<ServerStateSnapshot> GetSnapshotAsync(
        uint x, uint y, CancellationToken cancellationToken = default
    ) {
        using var cts = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
        cts.CancelAfter(TimeSpan.FromSeconds(1));

        return (await this.SendAndReceiveAsync(
            new ClientMessage() {
                Subscribe = new ClientSubscribe() {
                    CenterX = x,
                    CenterY = y
                }
            },
            (msg) => msg.PayloadCase is ServerMessage.PayloadOneofCase.Snapshot
                     && msg.Snapshot.XCoord == x
                     && msg.Snapshot.YCoord == y,
            linked.Token
        )).Snapshot;
    }

    private void OnTimer(object? state) {
        // TODO: cancel if no pongs I guess
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
