using System.Runtime.InteropServices;
using Chess;

namespace Firehorse;

public class Scraper : IDisposable {
    private readonly Connection connection;
    private readonly (int, int)[] chunks;
    private int chunkIdx;

    public Scraper(string username, (int, int)[] chunks) {
        this.connection = new Connection(username);
        this.chunks = chunks;
    }

    public async Task RunAsync(
        Func<ReadOnlyMemory<byte>, Task> dispatch,
        CancellationToken cancellationToken = default
    ) {
        var (x, y) = this.chunks[0];
        await this.connection.RunAsync(Handler, x, y, cancellationToken: cancellationToken);
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
        Func<ReadOnlyMemory<byte>, Task> dispatch,
        CancellationToken cancellationToken = default
    ) {
        // FIXME this format *also* sucks
        const int idCount = Program.SubscriptionSize * Program.SubscriptionSize;
        const int idCountBytes = idCount * 4;
        const int bytesCount = 2 + 2 + idCountBytes;

        var ids = new uint[Program.SubscriptionSize * Program.SubscriptionSize];
        foreach (var piece in snapshot.Pieces) {
            var normalizedDx = piece.Dx + Program.HalfSubscriptionSize;
            var normalizedDy = piece.Dy + Program.HalfSubscriptionSize;

            var idx = (normalizedDy * Program.SubscriptionSize) + normalizedDx;
            ids[idx] = piece.Piece.Id;
        }

        var bytes = new byte[bytesCount].AsMemory();
        BitConverter.GetBytes((ushort) snapshot.XCoord).CopyTo(bytes);
        BitConverter.GetBytes((ushort) snapshot.YCoord).CopyTo(bytes[2..]);
        MemoryMarshal.Cast<uint, byte>(ids).CopyTo(bytes[4..].Span);

        await dispatch(bytes);
        await this.CycleChunk(cancellationToken);
    }

    private async Task CycleChunk(CancellationToken cancellationToken = default) {
        this.chunkIdx++;
        if (this.chunkIdx > (this.chunks.Length - 1)) this.chunkIdx = 0;

        var (x, y) = this.chunks[this.chunkIdx];
        // Console.WriteLine($"going to {x},{y}");
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
