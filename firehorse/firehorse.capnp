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
