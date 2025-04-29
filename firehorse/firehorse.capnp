@0xcd442beccb7770f5;

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

struct Command {
  union {
    # Make a move on a random connection
    move :group {
      id @0 :UInt32;
      fromX @1 :UInt16;
      fromY @2 :UInt16;
      toX @3 :UInt16;
      toY @4 :UInt16;
      moveType @5 :MoveType;
    }

    # Query a position on a random connection
    watch :group {
      x @6 :UInt16;
      y @7 :UInt16;
    }
  }
}