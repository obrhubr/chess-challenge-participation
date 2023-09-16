using ChessChallenge.API;
using System;

public class MyBot_v3_0 : IChessBot
{
    // The value of: null, pawn, knight, bishop, rook, queen, king
    private static readonly Double[] PieceValues = { 0, 1.0, 3, 3, 4.5, 8, 0 };
    // Initialise value with this number
    private static readonly Double Max = 99999;

    // Add a stop flag for unlimited mode
    private bool _stop;
    public bool Stop { get => _stop; set => _stop = value; }

    private static Double Evaluate(Board board)
    {
        if (board.IsInCheckmate()) return 1000 * (board.IsWhiteToMove ? -1 : 1);

        PieceList[] pieces = board.GetAllPieceLists();
        Double score = 0;

        foreach (PieceList plist in pieces)
        {
            score += PieceValues[(int)plist.TypeOfPieceInList] * plist.Count * (plist.IsWhitePieceList ? 1 : -1);
        }

        // negative if black is better, positive if white is better
        return score;
    }

    private static (Double, Move) Minimax(Board board, int depth, bool maximisingPlayer, double alpha, double beta)
    {
        if (depth  == 0) return (Evaluate(board), new Move());

        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 0) return (Evaluate(board), new Move());

        Move bestMove = moves[0];
        Double value = maximisingPlayer ? -Max : Max;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            var moveEvaluation = Minimax(board, depth-1, !maximisingPlayer, alpha, beta);
            board.UndoMove(move);

            if (maximisingPlayer)
            {
                if (moveEvaluation.Item1 > value)
                {
                    bestMove = move;
                    value = moveEvaluation.Item1;
                }
                if (value >= beta) return (value, bestMove);
                if (value > alpha) alpha = value;
            } else
            {
                if (moveEvaluation.Item1 < value)
                {
                    bestMove = move;
                    value = moveEvaluation.Item1;
                }
                if (value <= alpha) return (value, bestMove);
                if (value < beta) beta = value;
            }
        }

        return (value, bestMove);
    }

    public Move Think(Board board, Timer timer)
    {
        (Double, Move) m = Minimax(board, 4, board.IsWhiteToMove, -Max, Max);
        return m.Item2;
    }
}