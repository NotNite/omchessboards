using CapnpGen;
using Chess;
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

    public static Snapshot ConvertSnapshotToFirehorse(ServerStateSnapshot snapshot) {
        return new Snapshot() {
            X = (ushort) snapshot.XCoord,
            Y = (ushort) snapshot.YCoord,
            Pieces = snapshot.Pieces.Select(ConvertPieceToFirehorse).ToList()
        };
    }

    public static Piece ConvertPieceToFirehorse(PieceDataForSnapshot piece) {
        return new Piece() {
            Id = piece.Piece.Id,
            Dx = (sbyte) piece.Dx,
            Dy = (sbyte) piece.Dy,
            Type = (PieceType) piece.Piece.Type,
            IsWhite = piece.Piece.IsWhite
        };
    }

    public static ClientMove ConvertMoveToGame(Move move) {
        return new ClientMove() {
            PieceId = move.Id,
            FromX = move.FromX,
            FromY = move.FromY,
            ToX = move.ToX,
            ToY = move.ToY,
            MoveType = (MoveType) move.MoveType
        };
    }
}
