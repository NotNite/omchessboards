using Chess;

namespace Firehorse.Chess;

public class Scraper(
    Connection connection,
    SharedChannels channels
) : IDisposable {
    public async Task RunSnapshotsAsync(CancellationToken cancellationToken = default) {
        while (!cancellationToken.IsCancellationRequested) {
            var (x, y) = channels.PositionQueue.Get();

            ServerStateSnapshot snapshot;
            try {
                snapshot = await connection.GetSnapshotAsync(x, y, cancellationToken);
            } catch {
                // Re-submit work if we failed to get it ourselves
                channels.PositionQueue.Submit((x, y));
                throw;
            }

            channels.SnapshotRelay.Writer.TryWrite(Util.ConvertSnapshotToFirehorse(snapshot));
        }
    }

    public async Task RunMovesAsync(CancellationToken cancellationToken = default) {
        var isWhite = await connection.IsWhite.Task.WaitAsync(cancellationToken);
        var moveQueue = isWhite ? channels.WhiteMoveQueue : channels.BlackMoveQueue;

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
