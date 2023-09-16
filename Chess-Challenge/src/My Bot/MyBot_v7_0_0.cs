using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// Add the tables
public class MyBot_v7_0 : IChessBot
{
    // The value of: null, pawn, knight, bishop, rook, queen, king
    private static readonly Double[] PieceValues = { 0, 1.0, 3, 3, 4.5, 8, 0 };
    // Initialise value with this number
    private static readonly Double Max = 99999;
    private static readonly int minDepth = 4;
    private static readonly int exitDepth = 20;
    private static readonly long thinkingTime = 500;
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


    // Pawn bitboard
    static double[] MultiplierPawns = {
    0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
    0.25480684651371577, 0.19219697650052214, 0.2623919449901734, 0.11610075110075149, 0.3883306203653432, 0.37898520540075015, 0.18813010553932316, 0.28387071286433796,
    0.14056351952545781, 0.10232678508410768, 0.3231020671834634, 0.08423574220707616, 0.08782538659793819, 0.052831245001332204, 0.6040540917328887, -0.0022919216646257973,
    0.3520863309352519, -0.05937903473716526, 0.3737154308617227, 0.2895630422887508, 0.1303397104822857, 0.11128937348563513, 0.023447082096933643, 0.22484526967285584,
    0.5316631799163177, 1.2393018480492823, -0.1325483091787449, 0.1154035567715456, 0.32470634095634066, 0.22235817575083483, -0.06311737089201876, -0.7707033997655338,
    -1.4391747572815536, 1.594898785425101, -2.3174603174603168, 0.6656379821958445, -0.08008680555555504, -2.3364965986394552, -2.6759942363112406, -1.1148263888888876,
    -3.6504545454545476, 0.40875000000000006, 2.664571428571429, 1.6936206896551718, 4.190425531914894, -5.229268292682928, -1.4963793103448275, 0.1245588235294111,
    0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
};

    // Bishop bitboard
    static double[] MultiplierBishops = {
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
        };

    // Knight bitboard
    static double[] MultiplierKnights = {
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
        };

    // Rook bitboard
    static double[] MultiplierRooks = {
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
        };

    // Queen bitboard
    static double[] MultiplierQueens = {
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
        };

    // King bitboard
    static double[] MultiplierKing = {
        1.3501372756071814, -0.23072912111936966, 0.5419792387543263, 0.22626272709123316, 0.28848923175827484, -0.057290116896918465, 0.506729118597138, 0.5251453260015712,
        0.42022245762711835, 0.23761589403973443, 0.10753886010362657, 0.08768055555555582, -0.08838957055214719, -0.954000000000001, 0.16843825665859552, 0.6660816944024202,
        0.7407881773399014, -0.403533834586467, 1.8196012269938653, -1.280841836734693, -1.2455133928571418, -0.5394666666666665, 1.007962085308057, -0.9516528925619837,
        0.02114754098360643, 2.2724175824175825, 0.7225641025641026, -2.742578947368422, -0.27812865497076034, 3.211201923076925, -0.4873429951690826, 2.334303797468355,
        5.675714285714285, 0.7640384615384617, 2.0717647058823525, 0.8390217391304347, 1.0569811320754716, -1.485961538461538, 0.7139534883720927, 4.626842105263158,
        10.176428571428572, -6.938437500000001, -1.4762857142857142, 0.9887499999999999, 4.617027027027026, 3.5648387096774177, 3.409117647058824, 6.1836363636363645,
        0.0, 0.03028571428571425, -5.404, -3.615, -1.4831818181818184, 3.8874999999999997, 1.6962962962962966, -5.535555555555557,
        0.0, 0.0, 5.3149999999999995, 51.505, 0.0, -3.19, -12.498181818181818, -6.016666666666667,
    };

    static double MultiplyBitboards(Board board, PieceType pt, Boolean color, double[] mP, double[] mN, double[] mB, double[] mR, double[] mQ, double[] mK)
    {
        double[] multiplier = mP;
        if (pt == PieceType.Bishop) multiplier = mB;
        else if (pt == PieceType.Knight) multiplier = mN;
        else if (pt == PieceType.Rook) multiplier = mR;
        else if (pt == PieceType.Queen) multiplier = mQ;
        else if (pt == PieceType.King) multiplier = mK;

        // For white, the pawns are from 8-15 and for black from ...-55
        var bitboard = board.GetPieceBitboard(pt, color);
        var score = 0.0;
        foreach (var i in Enumerable.Range(0, 64))
        {
            score += ((bitboard >> i) & 0x1) * multiplier[(color ? i : 63 - i)];
        }
        return score;
    }

    public static Double Evaluate(Board board)
    {
        if (board.IsInCheckmate()) return (mateValue - board.PlyCount) * (board.IsWhiteToMove ? -1 : 1);

        PieceList[] pieces = board.GetAllPieceLists();
        Double score = 0;

        foreach (PieceList plist in pieces)
        {
            score += PieceValues[(int)plist.TypeOfPieceInList] * plist.Count * (plist.IsWhitePieceList ? 1 : -1);
            score += MultiplyBitboards(board, plist.TypeOfPieceInList, plist.IsWhitePieceList, MultiplierPawns, MultiplierKnights, MultiplierBishops, MultiplierRooks, MultiplierQueens, MultiplierKing)
                * (plist.IsWhitePieceList ? 1 : -1);
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