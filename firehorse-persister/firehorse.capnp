@0xcd442beccb7770f5;

# copied here because capnpc is dumb as shit

enum PieceType {
  pawn @0;
  knight @1;
  bishop @2;
  rook @3;
  queen @4;
  king @5;
  promotedPawn @6;
}

enum MoveType {
  normal @0;
  castle @1;
  enPassant @2;
}

struct PieceData {
  id @0 :UInt32;
  type @1 :PieceType;
  isWhite @2 :Bool;
  moveCount @3 :UInt32;
  captureCount @4 :UInt32;
  # note: there's a ton of other metadata in PieceDataShared that isn't here
}

struct SnapshotPiece {
  dx @0 :Int8;
  dy @1 :Int8;
  data @2 :PieceData;
}

struct Snapshot {
  x @0 :UInt16;
  y @1 :UInt16;
  pieces @2 :List(SnapshotPiece);
}

# move that we're submitting to the game server
struct Move {
  id @0 :UInt32;
  fromX @1 :UInt16;
  fromY @2 :UInt16;
  toX @3 :UInt16;
  toY @4 :UInt16;
  moveType @5 :MoveType;
  # note: game doesn't require this, this is to determine what connection to use
  pieceIsWhite @6 :Bool;
}

# this serves the purpose of a move and capture combined, capturedPieceId is 0 for moves
struct MoveResult {
  seqnum @0 :UInt64;
  capturedPieceId @1 :UInt32;
}

# move made by another player
struct RemoteMove {
  seqnum @0 :UInt64;
  x @1 :UInt16;
  y @2 :UInt16;
  data @3 :PieceData;
}

# note: these will also not fire for every piece, as we can't watch the entire board at once
# (these are just "if we're lucky to see them in real time" basically)
# also, ServerMovesAndCaptures is split into two functions here, and ServerBulkCapture is flattened
interface Callback {
  # Called when a connection receives a snapshot.
  onSnapshot @0 (snapshot :Snapshot) -> ();

  # Called when a connection receives remote moves.
  onPiecesMoved @1 (moves :List(RemoteMove)) -> ();

  # Called when a connection receives captures.
  onPiecesCaptured @2 (captures :List(MoveResult)) -> ();

  # Called when a connection receives adoptions.
  onPiecesAdopted @3 (ids :List(UInt32)) -> ();
}

interface Firehorse {
  # Begin listening for events on the provided callback.
  listen @0 (callback :Callback) -> ();

  # Execute a list of moves one by one.
  # If a move fails, `failedAt` contains the index of the move that failed in the list.
  # `results` may not be the same size as `moves`, and it will be truncated if a move fails.
  moveSequential @1 (moves :List(Move)) -> (success :Bool, results :List(MoveResult), failedAt :UInt16);

  # Execute a list of moves at once, without any guaranteed ordering.
  # If any moves fail, `failed` contains the indexes of the failed moves in the list.
  # `results` is guaranteed to always be the same size as `moves`, and failed moves will have dummy data.
  moveParallel @2 (moves :List(Move)) -> (success :Bool, results :List(MoveResult), failed :List(UInt16));

  # Queue the given position to be queried for a snapshot.
  # The position will only be queried once.
  queue @3 (x :UInt16, y :UInt16) -> ();
}
