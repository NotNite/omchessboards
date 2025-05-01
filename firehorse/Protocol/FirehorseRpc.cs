using CapnpGen;
using Chess;

namespace Firehorse.Protocol;

// ReSharper disable AccessToDisposedClosure
public class FirehorseRpc(SharedChannels channels) : IFirehorse {
    public Task Listen(ICallback callback, CancellationToken cancellationToken = default) {
        // Separate task because we want it to outlive this function returning
        _ = Task.Run<Task>(async () => {
            try {
                using var callbackCts = new CancellationTokenSource();
                using var linked =
                    CancellationTokenSource.CreateLinkedTokenSource(callbackCts.Token, cancellationToken);

                await Util.WrapTasks(
                    callbackCts,
                    Util.PipeChannelIntoCallback(
                        channels.SnapshotRelay,
                        (data) => callback.OnSnapshot(data, linked.Token),
                        linked.Token
                    ),
                    Util.PipeChannelIntoCallback(
                        channels.MoveRelay,
                        (data) => callback.OnPiecesMoved(data, linked.Token),
                        linked.Token
                    ),
                    Util.PipeChannelIntoCallback(
                        channels.CaptureRelay,
                        (data) => callback.OnPiecesCaptured(data, linked.Token),
                        linked.Token
                    ),
                    Util.PipeChannelIntoCallback(
                        channels.AdoptRelay,
                        (data) => callback.OnPiecesAdopted(data, linked.Token),
                        linked.Token
                    )
                );
            } catch (Exception) {
                // Console.WriteLine(e);
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public async Task<(bool, IReadOnlyList<MoveResult>, ushort)> MoveSequential(
        IReadOnlyList<Move> moves, CancellationToken cancellationToken = default
    ) {
        var results = new List<MoveResult>();

        // TODO: possible perf benefit here by sending the next move before the previous result is received
        for (var i = 0; i < moves.Count; i++) {
            try {
                using var cts = new CancellationTokenSource();
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                cts.CancelAfter(TimeSpan.FromSeconds(1));

                var move = moves[i];
                var gameMove = Util.ConvertMoveToGame(move);
                var moveQueue = move.PieceIsWhite ? channels.WhiteMoveQueue : channels.BlackMoveQueue;

                var result = await moveQueue.SubmitAndWait(gameMove).WaitAsync(linked.Token);
                results.Add(Util.ConvertMoveResultToFirehorse(result));
            } catch (Exception e) {
                // This will throw for InvalidMoveException, but we also want other failures (e.g. unreliable net) to
                // bail, since we won't know if the move went through or not

                Console.WriteLine(e);
                return (false, results, (ushort) i);
            }
        }

        return (true, results, 0);
    }

    public async Task<(bool, IReadOnlyList<MoveResult>, IReadOnlyList<ushort>)> MoveParallel(
        IReadOnlyList<Move> moves, CancellationToken cancellationToken = default
    ) {
        var attempts = moves.Select((move) => {
            var task = Task.Run(async () => {
                using var cts = new CancellationTokenSource();
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                cts.CancelAfter(TimeSpan.FromSeconds(1));

                var gameMove = Util.ConvertMoveToGame(move);
                var moveQueue = move.PieceIsWhite ? channels.WhiteMoveQueue : channels.BlackMoveQueue;

                return await moveQueue.SubmitAndWait(gameMove).WaitAsync(linked.Token);
            }, cancellationToken);

            return task.ContinueWith<ServerValidMove?>(t => {
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
        var results = result
                     .Select(r => r is not null ? Util.ConvertMoveResultToFirehorse(r) : new MoveResult())
                     .ToList();
        var failed = result.Where(x => x is null).Select((_, i) => (ushort) i).ToList();

        return (success, results, failed);
    }

    public Task Queue(ushort x, ushort y, CancellationToken cancellationToken = default) {
        channels.PositionQueue.Submit((x, y));
        return Task.CompletedTask;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
}
