using System.Threading.Channels;
using CapnpGen;
using Chess;
using PieceType = CapnpGen.PieceType;

namespace Firehorse.Chess;

public class Scraper(
    Connection connection,
    ConstantScraperQueue<(uint, uint)> positionQueue,
    AsyncScraperQueue<ClientMove, ServerValidMove> whiteMoveQueue,
    AsyncScraperQueue<ClientMove, ServerValidMove> blackMoveQueue,
    ChannelWriter<Snapshot> snapshotWriter
) : IDisposable {
    public async Task RunSnapshotsAsync(CancellationToken cancellationToken = default) {
        while (!cancellationToken.IsCancellationRequested) {
            var (x, y) = positionQueue.Get();

            ServerStateSnapshot snapshot;
            try {
                snapshot = await connection.GetSnapshotAsync(x, y, cancellationToken);
            } catch {
                // Re-submit work if we failed to get it ourselves
                positionQueue.Submit((x, y));
                throw;
            }

            snapshotWriter.TryWrite(Util.ConvertSnapshotToFirehorse(snapshot));
        }
    }

    public async Task RunMovesAsync(CancellationToken cancellationToken = default) {
        var isWhite = await connection.IsWhite.Task.WaitAsync(cancellationToken);
        var moveQueue = isWhite ? whiteMoveQueue : blackMoveQueue;

        while (!cancellationToken.IsCancellationRequested) {
            var (data, tcs) = await moveQueue.GetAsync(cancellationToken);

            ServerValidMove result;
            try {
                result = await connection.MakeMoveAsync(data, cancellationToken);
            } catch (InvalidMoveException move) {
                tcs.SetException(move);
                continue;
            } catch {
                moveQueue.Submit((data, tcs));
                throw;
            }

            tcs.SetResult(result);
        }
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
}
