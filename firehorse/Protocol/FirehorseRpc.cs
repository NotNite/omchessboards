using CapnpGen;
using Chess;
using Firehorse.Chess;
using MoveType = Chess.MoveType;

namespace Firehorse.Protocol;

// ReSharper disable AccessToDisposedClosure
public class FirehorseRpc(
    ChannelRelay<Snapshot> snapshotRelay,
    ConstantScraperQueue<(uint, uint)> positionQueue,
    AsyncScraperQueue<ClientMove, ServerValidMove> whiteMoveQueue,
    AsyncScraperQueue<ClientMove, ServerValidMove> blackMoveQueue
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

    public async Task<(bool, IReadOnlyList<uint>, ushort)> MoveSequential(
        IReadOnlyList<Move> moves, CancellationToken cancellationToken = default
    ) {
        var captured = new List<uint>();

        for (var i = 0; i < moves.Count; i++) {
            try {
                using var cts = new CancellationTokenSource();
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                cts.CancelAfter(TimeSpan.FromSeconds(1));

                var move = moves[i];
                var gameMove = Util.ConvertMoveToGame(move);
                var moveQueue = move.PieceIsWhite ? whiteMoveQueue : blackMoveQueue;

                var result = await moveQueue.SubmitAndWait(gameMove).WaitAsync(linked.Token);
                if (result.CapturedPieceId != 0) captured.Add(result.CapturedPieceId);
            } catch (Exception e) {
                // This will throw for InvalidMoveException, but we also want other failures (e.g. unreliable net) to
                // bail, since we won't know if the move went through or not

                Console.WriteLine(e);
                return (false, captured, (ushort) i);
            }
        }

        return (true, captured, 0);
    }

    public async Task<(bool, IReadOnlyList<uint>, IReadOnlyList<ushort>)> MoveParallel(
        IReadOnlyList<Move> moves, CancellationToken cancellationToken = default
    ) {
        var attempts = moves.Select((move) => {
            var task = Task.Run(async () => {
                using var cts = new CancellationTokenSource();
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                cts.CancelAfter(TimeSpan.FromSeconds(1));

                var gameMove = Util.ConvertMoveToGame(move);
                var moveQueue = move.PieceIsWhite ? whiteMoveQueue : blackMoveQueue;

                var result = await moveQueue.SubmitAndWait(gameMove).WaitAsync(linked.Token);
                return result.CapturedPieceId;
            }, cancellationToken);

            return task.ContinueWith<uint?>(t => {
                if (t.IsCompletedSuccessfully) {
                    return t.Result;
                } else {
                    Console.WriteLine(t.Exception!.InnerException);
                    return null;
                }
            }, cancellationToken);
        });
        var result = await Task.WhenAll(attempts);

        var success = result.All(x => x is not null);
        var captured = result.Where(x => x > 0).Select(x => x!.Value).ToList();
        var failed = result.Where(x => x is null).Select((_, i) => (ushort) i).ToList();

        return (success, captured, failed);
    }

    public Task Queue(ushort x, ushort y, CancellationToken cancellationToken = default) {
        positionQueue.Submit((x, y));
        return Task.CompletedTask;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
}
