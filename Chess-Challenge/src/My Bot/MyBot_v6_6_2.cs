using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// Add Quiescence Search with early exit
public class MyBot_v6_6_2 : IChessBot
{
    // The value of: null, pawn, knight, bishop, rook, queen, king
    private static readonly double[] PieceValues = { 0, 1.0, 3, 3, 4.5, 8, 0 };
    // Initialise value with this number
    private static readonly double Max = 99999;
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
        public double Evaluation { get; set; }
        public Move Move { get; set; }
        public bool Mate { get; set; }
        public int Ply { get; set; }
    }

    // Add a stop flag for unlimited mode
    private bool _stop;
    public bool Stop { get => _stop; set => _stop = value; }

    public double Evaluate(Board board)
    {
        // Evaluate Checkmate with the score assigned to mate (should be impossible to reach with normal evals)
        if (board.IsInCheckmate()) return (mateValue - board.PlyCount) * (board.IsWhiteToMove ? -1 : 1);

        // Sum the value of every piece currently on the board
        PieceList[] pieces = board.GetAllPieceLists();
        double score = 0;

        foreach (PieceList plist in pieces)
        {
            // Multiply the pawns value by their ranks
            if (plist.TypeOfPieceInList == PieceType.Pawn)
            {
                foreach (Piece pawn in plist)
                {
                    // Multiply the pawns value and 1.1 to the power of it's rank with the color of the pawn
                    score += PieceValues[(int)pawn.PieceType] *
                        Math.Pow(1.01, plist.IsWhitePieceList ? pawn.Square.Rank : 7 - pawn.Square.Rank) *
                        (plist.IsWhitePieceList ? 1 : -1);
                }
                // Skip this turn to prevent double points for pawns
                continue;
            }

            score += PieceValues[(int)plist.TypeOfPieceInList] * plist.Count * (plist.IsWhitePieceList ? 1 : -1);
        }

        // Count the number of possible moves that are left for black and white
        // Skip a Turn and get possible moves for the other color
        var skipped = board.TrySkipTurn();
        if (skipped)
        {
            // Get for the next color
            foreach (Move m in board.GetLegalMoves())
            {
                if (m.MovePieceType == PieceType.Queen) continue;
                // if the move ends in the middle 16 squares, it counts *1.15
                var isMiddle =
                    (m.TargetSquare.Index >= 18 && m.TargetSquare.Index <= 21) ||
                    (m.TargetSquare.Index >= 26 && m.TargetSquare.Index <= 29) ||
                    (m.TargetSquare.Index >= 34 && m.TargetSquare.Index <= 37) ||
                    (m.TargetSquare.Index >= 42 && m.TargetSquare.Index <= 45);
                score += (isMiddle ? 1.10 : 1) / 70 * (board.IsWhiteToMove ? 1 : -1);
            }
            board.UndoSkipTurn();

            // Get for the previous color
            // Divide by the maximum amount of moves possible in a chess game: 218 - this always gives a value between 0-1
            foreach (Move m in board.GetLegalMoves())
            {
                if (m.MovePieceType == PieceType.Queen) continue;
                // if the move ends in the middle 16 squares, it counts *1.15
                var isMiddle =
                    (m.TargetSquare.Index >= 18 && m.TargetSquare.Index <= 21) ||
                    (m.TargetSquare.Index >= 26 && m.TargetSquare.Index <= 29) ||
                    (m.TargetSquare.Index >= 34 && m.TargetSquare.Index <= 37) ||
                    (m.TargetSquare.Index >= 42 && m.TargetSquare.Index <= 45);
                score += (isMiddle ? 1.10 : 1) / 70 * (board.IsWhiteToMove ? 1 : -1);
            }
        }

        // negative if black is better, positive if white is better
        return score;
    }

    public static bool ContainsSquare(ulong bitboard, int square)
    {
        return ((bitboard >> square) & 1) != 0;
    }

    private static double EvaluateMove(Board board, Move move, double depth)
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

    private double QuiescenceSearch(ChessChallenge.API.Timer timer, Board board, double alpha, double beta)
    {
        var eval = (board.IsWhiteToMove ? 1 : -1) * Evaluate(board);
        if (eval >= beta) return beta;

        // Delta Pruning
        if (eval < alpha - PieceValues[(int)PieceType.Queen]) return alpha;

        if (eval > alpha) alpha = eval;

        var moves = board.GetLegalMoves(true);
        moves = moves.OrderByDescending(m => EvaluateMove(board, m, Max)).ToArray();
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            eval = -QuiescenceSearch(timer, board, -beta, -alpha);
            board.UndoMove(move);

            if (timer.MillisecondsElapsedThisTurn > (thinkingTime + 1)) return alpha;

            if (eval >= beta) return beta;
            if (eval > alpha) alpha = eval;
        }
        return alpha;
    }

    private (double, Move) Minimax(ChessChallenge.API.Timer timer, Board board, int depth, int maxDepth, double alpha, double beta, Move lastBestMove, int numExtensions)
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
        if (movesUnordered.Length == 0 || depth == 0) return (QuiescenceSearch(timer, board, alpha, beta), new Move());

        Move[] moves;
        if (!lastBestMove.IsNull) moves = movesUnordered.OrderByDescending(m => EvaluateMove(board, m, depth)).ToList().Prepend(lastBestMove).ToArray();
        else moves = movesUnordered.OrderByDescending(m => EvaluateMove(board, m, depth)).ToArray();

        Move bestMove = moves[0];
        double bestEvaluation = alpha;
        double value = alpha;

        var i = 0;
        foreach (Move move in moves)
        {
            // Set default search depth
            var searchDepth = depth - 1;

            board.MakeMove(move);

            // If the move puts the board in check or it would put a pawn on the last rank search deeper
            if (numExtensions < 16 &&
                (board.IsInCheck() ||
               (move.MovePieceType == PieceType.Pawn && (move.TargetSquare.Rank == 1 || move.TargetSquare.Rank == 6))))
            {
                numExtensions++;
                searchDepth++;
            }
            // If the move is later in the move list search less deeply
            else if (!move.IsCapture && depth >= 3 && i >= 3)
                searchDepth--;
            var (moveEvaluation, _) = Minimax(timer, board, searchDepth, maxDepth, -beta, -value, new Move(), numExtensions);

            // If the move seems interesting in spite of reducing the search depth, search in full
            if (searchDepth < (depth - 1) && (-1 * moveEvaluation) > alpha) (moveEvaluation, _) = Minimax(timer, board, depth - 1, maxDepth, -beta, -value, new Move(), numExtensions);

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
            i++;
        }

        // Do not store in Transposition table if the evaluation was due to repetitions or 50 move rule
        if (value == -Max || value == Max) return (value, bestMove);

        tNode.Evaluation = value;
        tNode.Depth = depth;
        tNode.Move = bestMove;
        if (value <= alphaOrig) tNode.Flag = 2;
        else if (value >= beta) tNode.Flag = 1;
        else tNode.Flag = 0;
        if (!tNode.Mate && Math.Abs(value) > (mateValue - 1000) && Math.Abs(value) < Max)
        {
            tNode.Mate = true;
            tNode.Evaluation = mateValue * (board.IsWhiteToMove ? -1 : 1) * -1;
            tNode.Ply = board.PlyCount;
        }
        transpositionTable[board.ZobristKey] = tNode;

        return (value, bestMove);
    }

    private Move IterativeDeepening(ChessChallenge.API.Timer timer, Board board)
    {
        Move bestMove = new();
        double bestValue = -Max;
        int depth = minDepth;

        while (timer.MillisecondsElapsedThisTurn < (thinkingTime + 2))
        {
            if (depth >= exitDepth) break;
            var (value, move) = Minimax(timer, board, depth, depth, -Max, Max, bestMove, 16);
            //Console.WriteLine("Depth: " + depth + " - Output: (val: " + value + ", move: " + move.ToString() + "), best: (val: " + bestValue + ", move: " + bestMove.ToString() + ")");
            if (!move.IsNull)
            {
                bestMove = move;
                bestValue = value;
            }

            depth++;
        }

        return bestMove;
    }

    public Move Think(Board board, ChessChallenge.API.Timer timer)
    {
        Move m = IterativeDeepening(timer, board);
        if ((transpositionTable.Count * entrySize / ttSize) > 0.95) transpositionTable.Clear();
        return m;
    }
}