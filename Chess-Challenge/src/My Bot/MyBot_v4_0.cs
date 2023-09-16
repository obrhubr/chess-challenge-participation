using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// Add Iterative Deepening
public class MyBot_v4_0 : IChessBot
{
    // The value of: null, pawn, knight, bishop, rook, queen, king
    private static readonly Double[] PieceValues = { 0, 1.0, 3, 3, 4.5, 8, 0 };
    // Initialise value with this number
    private static readonly Double Max = 99999;
    private static readonly int minDepth = 2;
    private static readonly int exitDepth = 20;
    private static readonly long thinkingTime = 500;

    // Add a stop flag for unlimited mode
    private bool _stop;
    public bool Stop { get => _stop; set => _stop = value; }

    private static Double Evaluate(Board board)
    {
        if (board.IsInCheckmate()) return (1000 - board.PlyCount) * (board.IsWhiteToMove ? -1 : 1);

        PieceList[] pieces = board.GetAllPieceLists();
        Double score = 0;

        foreach (PieceList plist in pieces)
        {
            score += PieceValues[(int)plist.TypeOfPieceInList] * plist.Count * (plist.IsWhitePieceList ? 1 : -1);
        }

        // negative if black is better, positive if white is better
        return score;
    }

    private static (Double, Move) Minimax(Timer timer, Board board, int depth, int maxDepth, double alpha, double beta)
    {
        if (board.PlyCount > 0 && depth < maxDepth)
        {
            if (board.IsRepeatedPosition()) return (beta, new Move());
            if (board.FiftyMoveCounter >= 100) return (beta, new Move());
        }

        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 0 || depth == 0) return ((board.IsWhiteToMove ? 1 : -1) * Evaluate(board), new Move());

        Move bestMove = moves[0];
        Double value = alpha;

        foreach (Move move in moves)
        {
            if (timer.MillisecondsElapsedThisTurn > (thinkingTime + 1)) return (value, bestMove);

            board.MakeMove(move);
            var (moveEvaluation, _) = Minimax(timer, board, depth - 1, maxDepth, -beta, -value);
            moveEvaluation *= -1;
            board.UndoMove(move);

            if (moveEvaluation > value)
            {
                value = moveEvaluation;
                bestMove = move;
            }
            if (value >= beta) return (value, bestMove);
        }

        return (value, bestMove);
    }

    private static Move IterativeDeepening(Timer timer, Board board)
    {
        Move bestMove = new();
        double bestValue = -Max;
        int depth = minDepth;

        while (timer.MillisecondsElapsedThisTurn < (thinkingTime + 2))
        {
            if (depth >= exitDepth) break;

            var (value, move) = Minimax(timer, board, depth, depth, -Max, Max);
            Console.WriteLine("Depth: " + depth + " - Output: (val: " + value + ", move: " + move.ToString() + "), best: (val: " + bestValue + ", move: " + bestMove.ToString() + ")");
            if (timer.MillisecondsElapsedThisTurn < thinkingTime)
            {
                bestMove = move;
                bestValue = value;
            }

            depth++;
        }

        return bestMove;
    }

    public Move Think(Board board, Timer timer)
    {
        Move m = IterativeDeepening(timer, board);
        return m;
    }
}