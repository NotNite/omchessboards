using System.Net;
using System.Security.Cryptography;
using System.Threading.Channels;
using CapnpGen;
using Chess;
using MoveType = Chess.MoveType;
using PieceType = CapnpGen.PieceType;

namespace Firehorse;

/// <summary>Basic scraper that sweeps a set of positions.</summary>
public class Scraper(
    IWebProxy? proxy,
    PositionQueue queue,
    Action<Snapshot> dispatch,
    ChannelReader<Command.READER> commandReader
) : IDisposable {
    private readonly ChessConnection connection = new(proxy);
    private (uint, uint, TaskCompletionSource<ServerStateSnapshot>)? pendingSnapshot;

    public async Task ConnectAsync(
        CancellationToken cancellationToken = default
    ) {
        var (x, y, _) = this.CreatePendingSnapshot();
        await this.connection.ConnectAsync(x, y, cancellationToken: cancellationToken);
    }

    public async Task RunAsync(
        CancellationToken cancellationToken = default
    ) {
        var cts = new CancellationTokenSource();
        var token = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

        List<Task> tasks = [
            Task.Run(() => this.HandleQueue(token.Token), token.Token),
            Task.Run(() => this.HandleMessages(token.Token), token.Token),
            Task.Run(() => this.HandleCommands(token.Token), token.Token),
        ];

        // ensure all tasks fail if a single task fails
        await Task.WhenAll(tasks.Select(async (t) => {
            try {
                await t;
            } catch (Exception e) {
                // Console.WriteLine(e);
                await cts.CancelAsync();
            }
        }));
    }

    private (uint, uint, TaskCompletionSource<ServerStateSnapshot>) CreatePendingSnapshot() {
        var (x, y) = queue.GetNextPosition();
        var tcs = new TaskCompletionSource<ServerStateSnapshot>();
        return (this.pendingSnapshot = (x, y, tcs)).Value;
    }

    private async Task HandleQueue(CancellationToken cancellationToken = default) {
        while (!cancellationToken.IsCancellationRequested) {
            // this would have already been set by the initial connection or previous loop
            var (_, _, tcs) = this.pendingSnapshot!.Value;

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(1));
            var combined = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

            var snapshot = await tcs.Task.WaitAsync(combined.Token);

            // convert from protobuf to capn proto
            var pieces = new Piece[snapshot.Pieces.Count];
            for (var i = 0; i < pieces.Length; i++) {
                var theirPiece = snapshot.Pieces[i];
                var ourPiece = new Piece() {
                    Id = theirPiece.Piece.Id,
                    Dx = (sbyte) theirPiece.Dx,
                    Dy = (sbyte) theirPiece.Dy,
                    Type = (PieceType) theirPiece.Piece.Type,
                    IsWhite = theirPiece.Piece.IsWhite
                };
                pieces[i] = ourPiece;
            }

            dispatch(new Snapshot() {
                X = (ushort) snapshot.XCoord,
                Y = (ushort) snapshot.YCoord,
                Pieces = pieces
            });

            // get new work to queue
            var @new = this.CreatePendingSnapshot();
            await this.connection.SendMessageAsync(new ClientMessage() {
                Subscribe = new ClientSubscribe() {
                    CenterX = @new.Item1,
                    CenterY = @new.Item2
                }
            }, cancellationToken);
        }
    }

    private async Task HandleMessages(CancellationToken cancellationToken = default) {
        await this.connection.RunAsync((message) => {
            if (this.pendingSnapshot is not var (x, y, tcs)) return;
            if (message.PayloadCase is not ServerMessage.PayloadOneofCase.InitialState
                && message.PayloadCase is not ServerMessage.PayloadOneofCase.Snapshot) {
                return;
            }

            var snapshot = message.PayloadCase is ServerMessage.PayloadOneofCase.InitialState
                ? message.InitialState.Snapshot
                : message.Snapshot;
            if (snapshot.XCoord != x || snapshot.YCoord != y) return;

            tcs.SetResult(snapshot);
        }, cancellationToken: cancellationToken);
    }

    private async Task HandleCommands(CancellationToken cancellationToken = default) {
        await foreach (var command in commandReader.ReadAllAsync(cancellationToken)) {
            switch (command.which) {
                case Command.WHICH.Watch: {
                    queue.SubmitWork((command.Watch.X, command.Watch.Y));
                    break;
                }

                case Command.WHICH.Move: {
                    await this.connection.SendMessageAsync(new ClientMessage() {
                        Move = new ClientMove() {
                            PieceId = command.Move.Id,
                            FromX = command.Move.FromX,
                            FromY = command.Move.FromY,
                            ToX = command.Move.ToX,
                            ToY = command.Move.ToY,
                            MoveType = (MoveType) command.Move.MoveType
                        }
                    }, cancellationToken);

                    break;
                }
            }
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default) {
        await this.connection.DisconnectAsync(cancellationToken);
    }

    public void Dispose() {
        // Re-submit failed work
        if (this.pendingSnapshot is var (x, y, _)) queue.SubmitWork((x, y));

        this.connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
