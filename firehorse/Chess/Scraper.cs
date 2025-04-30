using System.Threading.Channels;
using CapnpGen;
using Chess;
using PieceType = CapnpGen.PieceType;

namespace Firehorse.Chess;

public class Scraper(
    Connection connection,
    ScraperPositionQueue queue,
    ChannelWriter<Snapshot> writer
) : IDisposable {
    public async Task RunAsync(CancellationToken cancellationToken = default) {
        while (!cancellationToken.IsCancellationRequested) {
            var (x, y) = queue.GetNextPosition();

            ServerStateSnapshot snapshot;
            try {
                snapshot = await connection.GetSnapshotAsync(x, y, cancellationToken);
            } catch {
                // Re-submit work if we failed to get it ourselves
                queue.SubmitWork((x, y));
                throw;
            }

            // convert to our protocol
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

            writer.TryWrite(new Snapshot() {
                X = (ushort) snapshot.XCoord,
                Y = (ushort) snapshot.YCoord,
                Pieces = pieces
            });
        }
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
}
