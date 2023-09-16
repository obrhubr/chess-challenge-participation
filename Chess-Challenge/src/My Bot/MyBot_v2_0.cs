using ChessChallenge.API;
using System;

public class MyBot_v2_0 : IChessBot
{
    // The value of: null, pawn, knight, bishop, rook, queen, king
    Double[] pieceValues = { 0, 1.0, 3, 3, 4.5, 8, 0 };

    // Add a stop flag for unlimited mode
    private bool _stop;
    public bool Stop { get => _stop; set => _stop = value; }

    public Double evaluate(Board board)
    {
        if (board.IsInCheckmate()) return 100 * (board.IsWhiteToMove ? -1 : 1);

        PieceList[] pieces = board.GetAllPieceLists();
        Double score = 0;

        foreach (PieceList plist in pieces)
        {
            score += pieceValues[(int)plist.TypeOfPieceInList] * plist.Count * (plist.IsWhitePieceList ? 1 : -1);
        }

        // negative if black is better, positive if white is better
        return score;
    }

    public (Double, Move) minimax(Board board, int depth, bool maximisingPlayer)
    {
        if (depth  == 0)
        {
            return (evaluate(board), new Move());
        }

        Move[] moves = board.GetLegalMoves();
        //Console.WriteLine(moves.Length);

        if (moves.Length == 0)
        {
            return (evaluate(board), new Move());
        }

        Move bestMove = moves[0];
        Double value = maximisingPlayer ? -99999 : 99999;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            (Double, Move) moveEvaluation = minimax(board, depth-1, !maximisingPlayer);
            board.UndoMove(move);

            if (maximisingPlayer)
            {
                if (moveEvaluation.Item1 > value)
                {
                    bestMove = move;
                    value = moveEvaluation.Item1;
                }
            } else
            {
                if (moveEvaluation.Item1 < value)
                {
                    bestMove = move;
                    value = moveEvaluation.Item1;
                }
            }
        }

        return (value, bestMove);
    }

    public Move Think(Board board, Timer timer)
    {
        //Console.WriteLine(board.GetFenString());
        (Double, Move) m = minimax(board, 4, board.IsWhiteToMove);

        //Console.WriteLine(m.Item2.ToString());
        return m.Item2;
    }
}