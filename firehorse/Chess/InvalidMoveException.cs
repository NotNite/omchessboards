using Chess;

namespace Firehorse.Chess;

public class InvalidMoveException(ClientMove move) : Exception("Invalid move") {
    public ClientMove Move => move;
}
