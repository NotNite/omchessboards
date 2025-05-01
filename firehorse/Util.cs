using System.Threading.Channels;
using Capnp.Rpc;
using CapnpGen;
using Chess;
using Firehorse.Protocol;
using Exception = System.Exception;
using MoveType = Chess.MoveType;
using PieceType = CapnpGen.PieceType;

namespace Firehorse;

public class Util {
    // https://stackoverflow.com/a/79386570
    public static Task WrapTasks(
        CancellationTokenSource cts,
        params IEnumerable<Task> tasks
    ) => Task.WhenAll(tasks.Select(task => {
        return task.ContinueWith(t => {
            if (t.IsFaulted) cts.Cancel();
            return t;
        }).Unwrap();
    }));

    public static async Task PipeChannelIntoCallback<T>(
        ChannelRelay<T> relay,
        Func<T, Task> submit,
        CancellationToken cancellationToken = default
    ) {
        using var reader = relay.CreateReader();
        await foreach (var data in reader.ReadAllAsync(cancellationToken)) {
            try {
                await submit(data);
            } catch (RpcException) {
                // ignored
            }
        }
    }

    public static Snapshot ConvertSnapshotToFirehorse(ServerStateSnapshot snapshot) => new() {
        X = (ushort) snapshot.XCoord,
        Y = (ushort) snapshot.YCoord,
        Pieces = snapshot.Pieces.Select(ConvertSnapshotPieceToFirehorse).ToList()
    };

    public static SnapshotPiece ConvertSnapshotPieceToFirehorse(PieceDataForSnapshot piece) => new() {
        Dx = (sbyte) piece.Dx,
        Dy = (sbyte) piece.Dy,
        Data = ConvertPieceToFirehorse(piece.Piece)
    };

    public static RemoteMove ConvertMovePieceToFirehorse(PieceDataForMove piece) => new() {
        Seqnum = piece.Seqnum,
        X = (ushort) piece.X,
        Y = (ushort) piece.Y,
        Data = ConvertPieceToFirehorse(piece.Piece)
    };

    public static PieceData ConvertPieceToFirehorse(PieceDataShared piece) => new() {
        Id = piece.Id,
        Type = (PieceType) piece.Type,
        IsWhite = piece.IsWhite,
        MoveCount = piece.MoveCount,
        CaptureCount = piece.CaptureCount
    };

    public static ClientMove ConvertMoveToGame(Move move) => new() {
        PieceId = move.Id,
        FromX = move.FromX,
        FromY = move.FromY,
        ToX = move.ToX,
        ToY = move.ToY,
        MoveType = (MoveType) move.MoveType
    };

    public static MoveResult ConvertMoveResultToFirehorse(ServerValidMove move) => new() {
        Seqnum = move.AsOfSeqnum,
        CapturedPieceId = move.CapturedPieceId
    };

    public static MoveResult ConvertPieceCaptureToFirehorse(PieceCapture capture) => new() {
        Seqnum = capture.Seqnum,
        CapturedPieceId = capture.CapturedPieceId
    };

    public static List<(ushort, ushort)> CreatePositions() {
        const int duplicate = 3; // add the work a few times to prevent constant refills

        var result = new List<(ushort, ushort)>();

        for (var i = 0; i < duplicate; i++) {
            foreach (var y in CreateAxisPositions()) {
                foreach (var x in CreateAxisPositions()) {
                    result.Add((x, y));
                }
            }
        }

        return result;
    }

    private static List<ushort> CreateAxisPositions() {
        var result = new List<ushort>();

        ushort i;
        for (i = Program.HalfSubscriptionSize; i < Program.MapSize; i += Program.SubscriptionSize) {
            result.Add(i);
        }

        // Very edge of the board won't be covered without this!
        result.Add(Program.MaxSubscription);

        return result;
    }
}
