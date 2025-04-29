using System.Net;
using System.Runtime.InteropServices;
using Chess;

namespace Firehorse;

#pragma warning disable CS9113 // Parameter is unread.

public class Scraper(IWebProxy? proxy, int id, (int, int)[] chunks) : IDisposable {
    private readonly ChessConnection connection = new(proxy);
    private int chunkIdx;

    // FIXME this format sucks
    private const int IdCount = Program.SubscriptionSize * Program.SubscriptionSize;
    private const int DataSize = 2 + 2 + (IdCount * 4);
    private Memory<byte> data = new byte[DataSize].AsMemory();

    public async Task ConnectAsync(
        CancellationToken cancellationToken = default
    ) {
        var (x, y) = chunks[0];
        await this.connection.ConnectAsync(x, y, cancellationToken: cancellationToken);
    }

    public async Task RunAsync(
        Action<ReadOnlyMemory<byte>> dispatch,
        CancellationToken cancellationToken = default
    ) {
        await this.connection.RunAsync(Handler, cancellationToken: cancellationToken);
        return;

        async Task Handler(ServerMessage message) {
            switch (message.PayloadCase) {
                case ServerMessage.PayloadOneofCase.InitialState: {
                    await this.HandleSnapshot(message.InitialState.Snapshot, dispatch, cancellationToken);
                    break;
                }

                case ServerMessage.PayloadOneofCase.Snapshot: {
                    await this.HandleSnapshot(message.Snapshot, dispatch, cancellationToken);
                    break;
                }

                case ServerMessage.PayloadOneofCase.Pong: {
                    // do nothing
                    break;
                }

                default: {
                    // Console.WriteLine($"Unknown case: {message.PayloadCase}");
                    break;
                }
            }
        }
    }

    private async Task HandleSnapshot(
        ServerStateSnapshot snapshot,
        Action<ReadOnlyMemory<byte>> dispatch,
        CancellationToken cancellationToken = default
    ) {
        BitConverter.GetBytes((ushort) snapshot.XCoord).CopyTo(this.data);
        BitConverter.GetBytes((ushort) snapshot.YCoord).CopyTo(this.data[2..]);
        var ids = MemoryMarshal.Cast<byte, uint>(this.data[4..].Span);

        foreach (var piece in snapshot.Pieces) {
            var normalizedDx = piece.Dx + Program.HalfSubscriptionSize;
            var normalizedDy = piece.Dy + Program.HalfSubscriptionSize;
            var idx = (normalizedDy * Program.SubscriptionSize) + normalizedDx;
            ids[idx] = piece.Piece.Id;
        }

        dispatch(this.data);
        await this.CycleChunk(cancellationToken);
    }

    private async Task CycleChunk(CancellationToken cancellationToken = default) {
        this.chunkIdx++;
        if (this.chunkIdx >= chunks.Length) {
            // Console.WriteLine($"finished chunk work {id} :D");
            this.chunkIdx = 0;
        }

        var (x, y) = chunks[this.chunkIdx];
        // Console.WriteLine($"{id} going to {x},{y}");

        await this.connection.SendMessageAsync(new ClientMessage() {
            Subscribe = new ClientSubscribe() {
                CenterX = (uint) x,
                CenterY = (uint) y
            }
        }, cancellationToken);
    }

    public void Dispose() {
        this.connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
