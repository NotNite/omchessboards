using System.Buffers;
using System.Collections.Immutable;
using System.Net;
using System.Runtime.InteropServices;
using Capnp;
using CapnpGen;
using Chess;
using PieceType = CapnpGen.PieceType;

namespace Firehorse;

#pragma warning disable CS9113 // Parameter is unread.

public class Scraper(IWebProxy? proxy, int id, (int, int)[] chunks) : IDisposable {
    private readonly ChessConnection connection = new(proxy);
    private int chunkIdx;

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
        var msg = MessageBuilder.Create();
        var writer = msg.BuildRoot<Snapshot.WRITER>();

        var proto = new Snapshot() {
            X = (ushort)snapshot.XCoord,
            Y = (ushort) snapshot.YCoord,
            Pieces = snapshot.Pieces.Select(p => new Piece() {
                Id = p.Piece.Id,
                Dx= (sbyte)p.Dx,
                Dy = (sbyte)p.Dy,
                Type = (PieceType) p.Piece.Type,
                IsWhite = p.Piece.IsWhite
            }).ToImmutableList()
        };

        proto.serialize(writer);

        // this library is so bleh :(
        using var ms = new MemoryStream();
        using var pump = new FramePump(ms);
        pump.Send(msg.Frame);
        ms.Seek(0, SeekOrigin.Begin);
        var bytes = ms.ToArray().AsMemory();

        dispatch(bytes);

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
