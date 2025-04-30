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

struct Piece {
  id @0 :UInt32;
  dx @1 :Int8;
  dy @2 :Int8;
  type @3 :PieceType;
  isWhite @4 :Bool;
}

struct Snapshot {
  x @0 :UInt16;
  y @1 :UInt16;
  pieces @2 :List(Piece);
}

struct Move {
  id @0 :UInt32;
  fromX @1 :UInt16;
  fromY @2 :UInt16;
  toX @3 :UInt16;
  toY @4 :UInt16;
  moveType @5 :MoveType;
}

interface Callback {
  onSnapshot @0 (snapshot :Snapshot) -> ();
}

interface Firehorse {
  # Begin listening for events on the provided callback.
  listen @0 (callback: Callback) -> ();

  # Execute a list of moves one by one.
  # If a move fails, `failedAt` contains the index of the move that failed in the list.
  moveSequential @1 (moves: List(Move)) -> (success :Bool, failedAt :UInt16);

  # Execute a list of moves at once, without any guaranteed ordering.
  # If any moves fail, `failed` contains the indexes of the failed moves in the list.
  moveParallel @2 (moves: List(Move)) -> (success :Bool, failed :List(UInt16));

  # Queue the given position to be queried for a snapshot.
  queue @3 (x :UInt16, y :UInt16) -> ();
}
