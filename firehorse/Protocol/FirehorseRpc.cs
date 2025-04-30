using CapnpGen;
using Chess;
using Firehorse.Chess;
using MoveType = Chess.MoveType;

namespace Firehorse.Protocol;

// ReSharper disable AccessToDisposedClosure
public class FirehorseRpc(
    ChannelRelay<Snapshot> snapshotRelay,
    ScraperPositionQueue queue,
    ConnectionManager connectionManager
) : IFirehorse {
    public Task Listen(ICallback callback, CancellationToken cancellationToken = default) {
        // Separate task because we want it to outlive this function returning
        _ = Task.Run<Task>(async () => {
            try {
                using var callbackCts = new CancellationTokenSource();
                using var linked =
                    CancellationTokenSource.CreateLinkedTokenSource(callbackCts.Token, cancellationToken);

                await Util.WrapTasks(
                    callbackCts,
                    Task.Run(async () => {
                        using var reader = snapshotRelay.CreateReader();
                        await foreach (var data in reader.ReadAllAsync(linked.Token)) {
                            await callback.OnSnapshot(data, linked.Token);
                        }
                    }, linked.Token)
                );
            } catch (Exception) {
                // Console.WriteLine(e);
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public async Task<(bool, ushort)> MoveSequential(
        IReadOnlyList<Move> moves, CancellationToken cancellationToken = default
    ) {
        for (var i = 0; i < moves.Count; i++) {
            try {
                var move = moves[i];
                var conn = connectionManager.GetRandomConnection();

                await conn.MakeMoveAsync(new ClientMove() {
                    PieceId = move.Id,
                    FromX = move.FromX,
                    FromY = move.FromY,
                    ToX = move.ToX,
                    ToY = move.ToY,
                    MoveType = (MoveType) move.MoveType
                }, cancellationToken);
            } catch (Exception) {
                // Console.WriteLine(e);
                return (false, (ushort) i);
            }
        }

        return (true, 0);
    }

    public async Task<(bool, IReadOnlyList<ushort>)> MoveParallel(
        IReadOnlyList<Move> moves, CancellationToken cancellationToken = default
    ) {
        var attempts = moves.Select((move) => Task.Run(async () => {
            var conn = connectionManager.GetRandomConnection();
            await conn.MakeMoveAsync(new ClientMove() {
                PieceId = move.Id,
                FromX = move.FromX,
                FromY = move.FromY,
                ToX = move.ToX,
                ToY = move.ToY,
                MoveType = (MoveType) move.MoveType
            }, cancellationToken);
        }, cancellationToken).ContinueWith(t => {
            if (t.IsCompletedSuccessfully) {
                return true;
            } else {
                // Console.WriteLine(t.Exception);
                return false;
            }
        }, cancellationToken));

        var result = await Task.WhenAll(attempts);
        var success = result.All(x => x);
        var failed = result.Where(x => !x).Select((_, i) => (ushort) i).ToList();
        return (success, failed);
    }

    public Task Queue(ushort x, ushort y, CancellationToken cancellationToken = default) {
        queue.SubmitWork((x, y));
        return Task.CompletedTask;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
}
