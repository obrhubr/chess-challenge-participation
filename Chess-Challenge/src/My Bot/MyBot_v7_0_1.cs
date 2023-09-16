﻿using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// Use the bitboards for evaluation
public class MyBot_v7_0_1 : IChessBot
{
    // The value of: null, pawn, knight, bishop, rook, queen, king
    private static readonly double[] PieceValues = { 0, 1.0, 3, 3, 4.5, 8, 0 };
    // Initialise value with this number
    private static readonly double Max = 99999;
    private static readonly int minDepth = 4;
    private static readonly int exitDepth = 20;
    private static int thinkingTime = 800;
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

    // Pawn bitboard
    static double[] MultiplierKing = {
        -1.2942124542124547, -0.22493253373313332, 0.07491972129657698, 0.43767316745124507, 0.2629354156499255, -0.5496249999999987, 0.17777232708489035, 0.26907618062736927,
        0.26778169014084474, 0.45228070175438556, 0.35736292428198424, 0.13772507260406597, -0.21302855309592622, -0.07920786516853989, -0.10980520811974605, 0.5625255648038062,
        7.5669298245614005, -0.8177777777777786, -0.5031679389312984, -0.6499816681943164, 0.04433040614709186, -0.171842918985776, 0.23964634146341396, 0.8039242053789731,
        -0.537307692307692, -0.842482269503546, -0.7596521739130437, 0.7587261146496821, 0.07525495750708215, 0.8891748768472914, -0.03407473309608548, 1.0177551020408167,
        -2.6812765957446802, -0.5445652173913039, 3.063284313725491, 0.23649193548387115, -0.5157518796992482, -0.3585953177257523, -1.2573493975903607, 1.7113461538461536,
        -1.5286363636363631, -2.0416260162601616, -1.7667567567567566, 1.7043333333333324, 0.28369369369369357, -0.38141592920353956, 0.8427142857142855, 3.834035087719298,
        -5.8264, 1.2565384615384616, 3.0115384615384615, -2.406140350877193, 8.360800000000001, -3.330289855072463, 2.3928947368421065, -3.405714285714286,
        -14.594999999999999, -8.025384615384615, -1.2225000000000001, -1.168, 12.378333333333332, -7.871818181818182, -9.027777777777779, 0.012800000000000011,
    };
    static double[] MultiplierQueens = {
        -0.23086642599277996, 1.049545454545454, -0.37645058448459057, 0.2585750586395794, 0.4820434782608688, -1.1779723502304138, -0.1501333333333334, 6.535441176470589,
        0.3084999999999999, 0.1647933884297524, 0.34828175756539614, 0.31230923329084, 0.18025159186209025, 0.018523316062176217, 0.9065079365079367, -2.644814814814816,
        0.28269961977186286, 0.45258606668473833, 0.7026287878787877, -0.13681401617250685, -0.0038411316648530847, 0.06445073612684028, -0.16314058355437683, 0.7289236790606655,
        0.43894602851323883, 0.8916179775280904, 0.790133206470028, 0.31509067357512965, 0.33237489397794723, 0.03751842751842771, -0.15898105813194, 0.5384188034188039,
        0.0439867109634553, -0.12073786407767002, -0.04677725118483412, 0.3053174603174601, 0.7148249619482485, 0.3621704180064309, 0.800057388809183, 0.5908408408408403,
        0.43124705882352915, -0.22748987854250996, -0.6533020637898692, 0.35990196078431363, -0.32122222222222224, -0.20740223463687152, 0.4138571428571438, -0.4869917355371903,
        -0.8734331337325345, -0.31645807259073844, -1.235847107438018, -1.2791338582677174, -1.226279069767442, 0.2618271604938271, 1.4734437086092722, -0.11329629629629617,
        0.986143790849673, 1.759018691588784, 0.1297575757575756, 0.12447916666666657, 1.7277439024390242, 0.7140384615384616, 0.32621848739495807, 1.5135840707964596,
    };
    static double[] MultiplierRooks = {
        0.19961335327301724, 0.2667353357446143, 0.06939425183657683, 0.15817080898401747, 0.14466804718875464, 0.2231947740501108, 0.45168088130774875, 0.21300152051272306,
        0.33942657342657356, -1.4616585365853663, -0.09693384223918586, -0.2378887303851641, -0.2826685198054205, -0.5933369923161362, -0.8328790786948173, 2.4795964125560523,
        0.30076660988075005, -0.7723868312757203, -0.5577264957264961, -0.35011015911872745, 0.17017526777020417, -0.5176545166402536, -0.047978863936591704, -1.004779299847793,
        -0.3267488789237672, 1.4780054644808742, 0.4448275862068965, 0.41644683714670294, 0.8131640058055161, -1.2040312499999999, -2.3442430703624746, -1.2084374999999992,
        1.5678672985781972, 0.26747747747747735, 0.17671186440677988, -0.020831918505942317, 0.11482932996207322, -0.07718503937007913, -0.48318713450292405, 0.31143925233645003,
        -0.9093801652892564, 0.5243290734824282, 0.2503225806451609, 1.1257780979827086, 0.530890410958904, -0.18032697547683965, -0.3639846743295019, 0.10121518987341746,
        -0.07636966824644546, 0.8133752417794965, -0.7387985436893199, 0.6772340425531909, 0.17504697986577192, 0.0984240150093806, -1.0410217391304348, 1.0348587570621466,
        -0.7574655647382919, -0.8557894736842105, -0.6158258258258268, 0.9616417910447758, 1.0432653061224486, 1.4465610859728508, -0.5192857142857145, -0.14703703703703688,
    };
    static double[] MultiplierBishops = {
        1.4410454545454536, 0.7128392484342385, 0.25338019886668456, -1.7231804281345562, 0.9644748858447488, 0.2344785229841731, 0.9698630136986305, -3.1220192307692307,
        -0.15961139896373044, 0.3084641811935365, 0.014754491017964272, 0.41992579833046273, 0.35172384365932735, 1.1764285714285703, 0.12964806668210213, 0.052737226277372345,
        -0.033632043448743675, -0.007714389534883623, 0.29508857395925636, 0.3810739191073917, 0.3133502860775589, -0.009576779026216927, 0.058992673992673945, -0.01857315598548985,
        1.146297093649086, -0.9861980830670912, 0.2158650584244266, 0.1588671875, 0.05681448632668114, 0.45429082581937835, 0.8060148975791437, 0.23575682382134064,
        3.1662046204620466, 0.3640715823466083, -0.057889570552146906, -0.76053861129137, 0.7540617384240453, -0.8457162726008339, 0.10658688656476281, 0.9635445205479449,
        0.21855457227138636, -1.3285665529010235, -0.2112022630834518, 0.17562277580071126, -0.1313705583756346, 0.864065359477124, 0.21771812080536898, 1.0502214930270704,
        -0.3177715877437324, 0.46149206349206323, 0.9725857519788915, -1.5999494949494948, 0.2066887417218542, 0.4302254098360653, -0.2684745762711865, -1.6913168724279835,
        -0.35144578313253033, 2.2032380952380946, 0.33270270270270363, 2.294437086092715, -4.56536231884058, 0.030518134715026114, -0.0825000000000003, 0.021901840490798102,
    };
    static double[] MultiplierKnights = {
        3.045762711864406, 0.22381297085712334, 0.2852849740932641, 0.7221753246753253, -0.39025454545454535, -0.48009580838323335, 0.3053203268173421, -3.113518518518519,
        1.811661971830985, 0.07442622950819743, -0.8305750350631134, 0.10707807863501495, 0.1714059280393081, -0.35145251396647986, -0.6321854304635758, 0.16787211740041905,
        -0.3002385964912275, 0.1813260869565218, 0.26756908152088854, -0.005977011494252264, -0.3991748003549243, 0.30568155465887514, 0.08325465838509317, -0.2825469168900808,
        0.3309025032938085, -0.6340972222222223, 0.0941656288916563, -0.04291645947289905, -0.37802512974597113, -0.3806772009029333, 0.44150093808630375, 1.1451089108910895,
        0.19248387096774186, -0.2749169054441257, 0.27890343698854425, 0.06878087649402395, 0.13505896805896836, -0.633847826086957, -0.29317243920412545, -0.45545741324921124,
        -3.2852799999999993, -0.14259938837920477, -0.2424233983286908, -1.0225878003696864, -0.17943014705882326, 0.08886679920477125, 0.6157057057057054, 0.7362944162436548,
        0.6652127659574467, -0.22608562691131504, 0.6674894514767934, 0.1545493562231762, -0.8226517571884989, 0.5237220843672455, -2.4387596899224793, 0.6780000000000004,
        1.7254512635379051, -0.4513636363636365, -1.669010989010989, -0.5074999999999996, 0.35749999999999993, 0.934246575342466, -4.130833333333334, 2.268304347826087,
    };
    static double[] MultiplierPawns = {
        0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
        0.25319048430035446, 0.2241713992705255, 0.11480554571225778, 0.23048788987628388, 0.21462678721615008, 0.1948479975701099, 0.19468907018208123, 0.2149837939193748,
        0.12009786237863168, 0.24881658938691015, 0.21963524306647347, -0.07524131874565572, 0.07414252832331893, 0.1298583884731931, 0.16795167262512647, 0.05940779656801748,
        0.053276031873937, 0.1400721833606191, 0.2335391713246365, 0.20801230435176363, 0.16937036422663831, -0.2899407634927604, 0.1525271512113623, 0.3625207363949001,
        -0.41186140459912995, 0.6713400725764644, 0.3184537572254331, 0.042118667527913885, -0.021339483722168394, -0.24224369747899246, 0.07459629384513669, 0.07073817292006519,
        -0.2736963190184048, -0.7192380952380952, -0.37003191489361786, 0.8804972375690597, 0.6315961538461546, -0.8079538904899136, -0.541402714932128, 0.21508333333333343,
        1.1839694656488542, -0.09294372294372352, 0.046056338028169365, 0.9560176991150435, 0.023319672131147728, 0.1507471264367818, -1.190532544378698, -0.26791798107255593,
        0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
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

        while (timer.MillisecondsElapsedThisTurn < (thinkingTime + 2) || depth <= minDepth)
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
        // Calculate thinking time: totalTime * endGameReserve / avgMovesUntilEndgameStarts + increment
        //thinkingTime = board.GetAllPieceLists().Select(x => x.Count).Aggregate((x, y) => x + y) > 20 ? ((int)(timer.GameStartTimeMilliseconds * 0.01) + timer.IncrementMilliseconds) : ((int)(timer.GameStartTimeMilliseconds * 0.05) + timer.IncrementMilliseconds);
        // Emergency stop to prevent time outs if thinkingTime is bigger than the remaining time
        //thinkingTime = (thinkingTime - timer.IncrementMilliseconds) * 2 > timer.MillisecondsRemaining ? timer.MillisecondsRemaining/4 : thinkingTime;

        Move m = IterativeDeepening(timer, board);
        if ((transpositionTable.Count * entrySize / ttSize) > 0.95) transpositionTable.Clear();
        return m;
    }
}