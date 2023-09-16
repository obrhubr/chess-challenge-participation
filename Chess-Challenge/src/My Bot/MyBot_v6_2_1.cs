using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// Try to optimize token size of 6_2_0
public class MyBot_v6_2_1 : IChessBot
{
    // The value of: null, pawn, knight, bishop, rook, queen, king
    double[] PieceValues = { 0, 1.0, 3, 3, 4.5, 8, 0 };

    // Initialise value with this number
    double Max = 99999;
    long thinkingTime = 700;

    // Transposition table
    Dictionary<ulong, TranspositionNode> transpositionTable = new();
    double ttSize = 209715200;
    double entrySize = System.Runtime.InteropServices.Marshal.SizeOf<TranspositionNode>();

    struct TranspositionNode
    {
        public int Depth { get; set; }
        public int Flag { get; set; }
        public double Evaluation { get; set; }
        public Move Move { get; set; }
    }

    // Add a stop flag for unlimited mode
    private bool _stop;
    public bool Stop { get => _stop; set => _stop = value; }

    double Evaluate(Board board)
    {
        if (board.IsInCheckmate()) return (1000 - board.PlyCount) * (board.IsWhiteToMove ? -1 : 1);

        PieceList[] pieces = board.GetAllPieceLists();
        double score = 0;

        foreach (PieceList plist in pieces) score += PieceValues[(int)plist.TypeOfPieceInList] * plist.Count * (plist.IsWhitePieceList ? 1 : -1);

        // negative if black is better, positive if white is better
        return score;
    }

    double EvaluateMove(Board board, Move move, double depth)
    {
        // If it's not a capture
        if (!move.IsCapture)
        {
            foreach (Piece pawn in board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove))
            {
                // Don't move to square attacked by opponents pawn
                if (((BitboardHelper.GetPawnAttacks(pawn.Square, board.IsWhiteToMove) >> move.TargetSquare.Index) & 1) != 0)
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

    (double, Move) Minimax(Timer timer, Board board, int depth, int maxDepth, double alpha, double beta, Move lastBestMove)
    {
        if (board.PlyCount > 0 && depth < maxDepth)
        {
            if (board.IsRepeatedPosition()) return (beta, new Move());
            if (board.FiftyMoveCounter >= 100) return (beta, new Move());
        }

        var alphaOrig = alpha;

        TranspositionNode tNode = new();
        var fetchedNode = transpositionTable.TryGetValue(board.ZobristKey, out tNode);
        if (fetchedNode && tNode.Depth >= depth && depth != maxDepth)
        {
            if (tNode.Flag == 0) return (tNode.Evaluation, tNode.Move);
            else if (tNode.Flag == 1) alpha = Math.Max(alpha, tNode.Evaluation);
            else if (tNode.Flag == 2) beta = Math.Min(beta, tNode.Evaluation);
            if (alpha >= beta) return (tNode.Evaluation, tNode.Move);
        }

        var movesUnordered = board.GetLegalMoves();
        if (movesUnordered.Length == 0 || depth == 0) return ((board.IsWhiteToMove ? 1 : -1) * Evaluate(board), new Move());

        var temp = movesUnordered.OrderByDescending(m => EvaluateMove(board, m, depth)).ToList();
        var moves = temp.ToArray();
        if (!lastBestMove.IsNull) moves = temp.Prepend(lastBestMove).ToArray();

        Move bestMove = moves[0];
        double value = alpha;

        foreach (Move move in moves)
        {

            board.MakeMove(move);
            var (moveEvaluation, _) = Minimax(timer, board, depth - 1, maxDepth, -beta, -value, new Move());
            moveEvaluation *= -1;
            board.UndoMove(move);

            if (timer.MillisecondsElapsedThisTurn > (thinkingTime + 1)) break;

            if (moveEvaluation > value)
            {
                value = moveEvaluation;
                bestMove = move;
            }

            if (value >= beta) return (value, bestMove);
        }

        tNode = new TranspositionNode()
        {
            Evaluation = value,
            Depth = depth,
            Move = bestMove,
            Flag = 0
        };
        if (value <= alphaOrig) tNode.Flag = 2;
        else if (value >= beta) tNode.Flag = 1;
        transpositionTable[board.ZobristKey] = tNode;

        return (value, bestMove);
    }

    Move IterativeDeepening(Timer timer, Board board)
    {
        Move bestMove = new();
        int depth = 4;

        while (timer.MillisecondsElapsedThisTurn < (thinkingTime + 2))
        {
            if (depth >= 20) break;

            var (value, move) = Minimax(timer, board, depth, depth, -Max, Max, bestMove);
            //Console.WriteLine("Depth: " + depth + " - Output: (val: " + value + ", move: " + move.ToString() + "), best: (val: " + bestValue + ", move: " + bestMove.ToString() + ")");
            if (!move.IsNull)
            {
                bestMove = move;
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