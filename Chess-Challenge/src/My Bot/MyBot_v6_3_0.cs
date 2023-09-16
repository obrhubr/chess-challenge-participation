using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

// Fix mate bug
public class MyBot_v6_3_0 : IChessBot
{
    // The value of: null, pawn, knight, bishop, rook, queen, king
    private static readonly Double[] PieceValues = { 0, 1.0, 3, 3, 4.5, 8, 0 };
    // Initialise value with this number
    private static readonly Double Max = 99999;
    private static readonly int minDepth = 4;
    private static readonly int exitDepth = 20;
    private static readonly long thinkingTime = 1000;
    private static readonly double mateValue = 4000;
    // Transposition table
    private static Dictionary<ulong, TranspositionNode> transpositionTable = new();
    double ttSize = 209715200;
    double entrySize = System.Runtime.InteropServices.Marshal.SizeOf<TranspositionNode>();
    public struct TranspositionNode
    {
        public int Depth { get; set; }
        public int Flag { get; set; }
        public Double Evaluation { get; set; }
        public Move Move { get; set; }
        public bool Mate { get; set; }
        public int Ply { get; set; }
    }

    // Add a stop flag for unlimited mode
    private bool _stop;
    public bool Stop { get => _stop; set => _stop = value; }

    public static Double Evaluate(Board board)
    {
        if (board.IsInCheckmate()) return (mateValue - board.PlyCount) * (board.IsWhiteToMove ? -1 : 1);

        PieceList[] pieces = board.GetAllPieceLists();
        Double score = 0;

        foreach (PieceList plist in pieces)
        {
            score += PieceValues[(int)plist.TypeOfPieceInList] * plist.Count * (plist.IsWhitePieceList ? 1 : -1);
        }

        // negative if black is better, positive if white is better
        return score;
    }

    public static bool ContainsSquare(ulong bitboard, int square)
    {
        return ((bitboard >> square) & 1) != 0;
    }

    private static Double EvaluateMove(Board board, Move move, Double depth)
    {
        // If it's not a capture
        if (!move.IsCapture)
        {
            foreach (Piece pawn in board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove))
            {
                // Don't move to square attacked by opponents pawn
                if (ContainsSquare(BitboardHelper.GetPawnAttacks(pawn.Square, board.IsWhiteToMove), move.TargetSquare.Index))
                {
                    return -10;
                }
            }
            // if hashed, score very high because computation is free
            board.MakeMove(move);
            var zobrist = board.ZobristKey;
            board.UndoMove(move);
            TranspositionNode tNode = new();
            if (transpositionTable.TryGetValue(zobrist, out tNode) && tNode.Depth >= depth) return 10000;

            return 0;
        }
        // If it's a capture
        else
        {
            // Multiply value of captured piece by K and substract piece value that is uesd to capture
            return 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
        }
    }

    private static (Double, Move) Minimax(Timer timer, Board board, int depth, int maxDepth, double alpha, double beta, Move lastBestMove)
    {
        if (board.PlyCount > 0 && depth < maxDepth)
        {
            if (board.IsRepeatedPosition()) return (-Max, new Move());
            if (board.FiftyMoveCounter >= 100) return (-Max, new Move());
        }

        var alphaOrig = alpha;

        TranspositionNode tNode = new();
        var fetchedNode = transpositionTable.TryGetValue(board.ZobristKey, out tNode);
        if (fetchedNode && tNode.Depth >= depth && tNode.Depth != maxDepth)
        {
            if (tNode.Mate) tNode.Evaluation -= Math.Sign(tNode.Evaluation) * tNode.Ply;
            if (tNode.Flag == 0) return (tNode.Evaluation, tNode.Move);
            else if (tNode.Flag == 1) alpha = Math.Max(alpha, tNode.Evaluation);
            else if (tNode.Flag == 2) beta = Math.Min(beta, tNode.Evaluation);
            if (alpha >= beta) return (tNode.Evaluation, tNode.Move);
        }

        Move[] movesUnordered = board.GetLegalMoves();
        if (movesUnordered.Length == 0 || depth == 0) return ((board.IsWhiteToMove ? 1 : -1) * Evaluate(board), new Move());

        Move[] moves;
        if (!lastBestMove.IsNull) moves = movesUnordered.OrderByDescending(m => EvaluateMove(board, m, depth)).ToList().Prepend(lastBestMove).ToArray();
        else moves = movesUnordered.OrderByDescending(m => EvaluateMove(board, m, depth)).ToArray();

        Move bestMove = moves[0];
        double bestEvaluation = alpha;
        Double value = alpha;

        foreach (Move move in moves)
        {

            board.MakeMove(move);
            var (moveEvaluation, _) = Minimax(timer, board, depth - 1, maxDepth, -beta, -value, new Move());
            board.UndoMove(move);

            // Return from the function if the time has elapsed
            if (timer.MillisecondsElapsedThisTurn > (thinkingTime + 1)) return (value, bestMove);

            // If this move triggers the repetition draw or 50 move rule draw, do not save it as a move
            if (moveEvaluation == -Max) continue;

            // Negamax inversion
            moveEvaluation *= -1;

            if (moveEvaluation > value)
            {
                value = moveEvaluation;
                bestMove = move;
            }

            if (value >= beta) break;
        }

        // Do not store in Transposition table if the evaluation was due to repetitions or 50 move rule
        if (value == -Max || value == Max) return (value, bestMove);

        tNode.Evaluation = value;
        tNode.Depth = depth;
        tNode.Move = bestMove;
        if (value <= alphaOrig) tNode.Flag = 2;
        else if (value >= beta) tNode.Flag = 1;
        else tNode.Flag = 0;
        if (Math.Abs(value) > (mateValue - 1000) && Math.Abs(value) < Max)
        {
            tNode.Mate = true;
            tNode.Evaluation = mateValue * (board.IsWhiteToMove ? -1 : 1) * -1;
        }
        tNode.Ply = board.PlyCount;
        transpositionTable[board.ZobristKey] = tNode;

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

            var (value, move) = Minimax(timer, board, depth, depth, -Max, Max, bestMove);
            Console.WriteLine("Depth: " + depth + " - Output: (val: " + value + ", move: " + move.ToString() + "), best: (val: " + bestValue + ", move: " + bestMove.ToString() + ")");
            if (!move.IsNull)
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
        if ((transpositionTable.Count * entrySize / ttSize) > 0.95) transpositionTable.Clear();
        return m;
    }
}