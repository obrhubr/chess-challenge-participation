using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;

// Add statistics
public class MyBot_v6_3_4 : IChessBot
{
    // The value of: null, pawn, knight, bishop, rook, queen, king
    private static readonly Double[] PieceValues = { 0, 1.0, 3, 3, 4.5, 8, 0 };
    // Initialise value with this number
    private static readonly Double Max = 99999;
    private static readonly int minDepth = 4;
    private static readonly int exitDepth = 20;
    private static readonly long thinkingTime = 7000;
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
    private bool _stop = false;
    public bool unlimited = false;
    public bool Stop { get => _stop; set { _stop = value; unlimited = true; } }

    // Statistics
    public struct Stats
    {
        public int nodesTraversed { get; set; }
        public int hashUsage { get; set; }
        public int hashUsageFull { get; set; }
        public List<int> hashUsageFullDepths { get; set; }
        public int hashMillis { get; set; }
        public int depth { get; set; }
        public string bestMove { get; set; }
        public double evaluation { get; set; }
        public int time { get; set; }
        public string pgn { get; set; }
        public string fen { get; set; }
        public List<string> sequence { get; set; }
        public string sequencePGN { get; set; }
    }
    public struct StatsFile
    {
        public List<Stats> stats { get; set; }
    }
    public Stats stats = new();

    public Double Evaluate(Board board)
    {
        // Evaluate Checkmate with the score assigned to mate (should be impossible to reach with normal evals)
        if (board.IsInCheckmate()) return (mateValue - board.PlyCount) * (board.IsWhiteToMove ? -1 : 1);

        // Sum the value of every piece currently on the board
        PieceList[] pieces = board.GetAllPieceLists();
        Double score = 0;

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

    private (Double, List<Move>) Minimax(Timer timer, Board board, int depth, int maxDepth, double alpha, double beta, Move lastBestMove)
    {
        // Update statistics
        stats.nodesTraversed++;

        if (board.PlyCount > 0 && depth < maxDepth)
        {
            if (board.IsRepeatedPosition()) return (-Max, new List<Move>());
            if (board.FiftyMoveCounter >= 100) return (-Max, new List<Move>());
        }

        var alphaOrig = alpha;

        TranspositionNode tNode = new();
        var fetchedNode = transpositionTable.TryGetValue(board.ZobristKey, out tNode);
        if (fetchedNode && tNode.Depth >= depth && tNode.Depth != maxDepth)
        {
            // Update statistics
            stats.hashUsage++;

            if (tNode.Mate) tNode.Evaluation -= Math.Sign(tNode.Evaluation) * tNode.Ply;
            if (tNode.Flag == 0) { stats.hashUsageFull++; stats.hashUsageFullDepths[depth]++; return (tNode.Evaluation, new List<Move> { tNode.Move }); }
            else if (tNode.Flag == 1) alpha = Math.Max(alpha, tNode.Evaluation);
            else if (tNode.Flag == 2) beta = Math.Min(beta, tNode.Evaluation);
            // The move sequence might be shorter than the depth because the move was fetched from the transposition table
            if (alpha >= beta) { stats.hashUsageFull++; stats.hashUsageFullDepths[depth]++; return (tNode.Evaluation, new List<Move> { tNode.Move }); };
        }

        Move[] movesUnordered = board.GetLegalMoves();
        if (movesUnordered.Length == 0 || depth == 0) return ((board.IsWhiteToMove ? 1 : -1) * Evaluate(board), new List<Move>());

        // TODO: I believe this ordering is concurrent and causes concurrency issues with the transpositionTable
        Move[] moves;
        if (!lastBestMove.IsNull) moves = movesUnordered.OrderByDescending(m => EvaluateMove(board, m, depth)).ToList().Prepend(lastBestMove).ToArray();
        else moves = movesUnordered.OrderByDescending(m => EvaluateMove(board, m, depth)).ToArray();

        Move bestMove = moves[0];
        List<Move> bestSearchedMoves = new();
        double bestEvaluation = alpha;
        Double value = alpha;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            var (moveEvaluation, searchedMoves) = Minimax(timer, board, depth - 1, maxDepth, -beta, -value, new Move());
            board.UndoMove(move);

            // Return from the function if the time has elapsed
            if ((timer.MillisecondsElapsedThisTurn > (thinkingTime + 1) && !unlimited) || _stop)
            {
                bestSearchedMoves.Add(bestMove);
                return (value, bestSearchedMoves);
            }

            // If this move triggers the repetition draw or 50 move rule draw, do not save it as a move
            if (moveEvaluation == -Max) continue;

            // Negamax inversion
            moveEvaluation *= -1;

            if (moveEvaluation > value)
            {
                value = moveEvaluation;
                bestMove = move;
                bestSearchedMoves = searchedMoves;
            }

            if (value >= beta) break;
        }

        // Add the move that was found to the best searched moves to keep a record of move sequences 
        bestSearchedMoves.Add(bestMove);

        // Do not store in Transposition table if the evaluation was due to repetitions or 50 move rule
        if (value == -Max || value == Max) return (value, bestSearchedMoves);

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

        return (value, bestSearchedMoves);
    }

    private Move IterativeDeepening(Timer timer, Board board)
    {
        Move bestMove = new();
        double bestValue = -Max;
        int depth = minDepth;
        int previousTime = 0;

        while ((timer.MillisecondsElapsedThisTurn < (thinkingTime + 2) && !unlimited) || (!_stop && unlimited))
        {
            if (_stop) break;

            if (depth >= exitDepth) break;
            var (value, moves) = Minimax(timer, board, depth, depth, -Max, Max, bestMove);
            if (!moves.Last().IsNull)
            {
                bestMove = moves.Last();
                bestValue = value;
            }

            // Update stats and write to console
            Console.WriteLine("Depth: " + depth + " - Output: (val: " + value + ", move: " + moves.Last().ToString() + "), best: (val: " + bestValue + ", move: " + bestMove.ToString() + ")");
            WriteStats(board, moves, bestMove, depth, bestValue, timer.MillisecondsElapsedThisTurn - previousTime);

            depth++;
            previousTime = timer.MillisecondsElapsedThisTurn;
        }

        return bestMove;
    }

    void WriteStats(Board board, List<Move> moves, Move bestMove, int depth, double bestValue, int time)
    {
        // Set basic elements
        stats.depth = depth;
        stats.hashMillis = (int)(transpositionTable.Count * entrySize / ttSize * 1000);
        stats.bestMove = bestMove.ToString().Split("\u0027")[1];
        stats.evaluation = bestValue;
        stats.time = time;
        stats.fen = board.GetFenString();
        stats.pgn = ChessChallenge.Chess.PGNCreator.CreatePGN(board.GameMoveHistory.Select(x => new ChessChallenge.Chess.Move(x.StartSquare.Index, x.TargetSquare.Index)).ToArray());

        // Get move sequence
        moves = moves.ToList();
        moves.Reverse();
        stats.sequence = moves.Select(x => x.ToString().Split("\u0027")[1]).ToList();
        stats.sequencePGN = ChessChallenge.Chess.PGNCreator.CreatePGN(board.GameMoveHistory.ToList().Concat(moves).Select(x => new ChessChallenge.Chess.Move(x.StartSquare.Index, x.TargetSquare.Index)).ToArray());

        // Read from previous and Write to File
        // var fileName = "../../../../stats/bot/" + DateAndTime.Now.ToString("yyyy-MM-ddTHH-mm") + "_ply_" + board.PlyCount + "_color_" + (board.IsWhiteToMove ? "white" : "black") + ".json";
        var fileName = "../../../../monitoring/data.json";

        if (File.Exists(fileName))
        {
            var statsFile = JsonSerializer.Deserialize<StatsFile>(File.ReadAllText(fileName));
            if (statsFile.stats.Last().depth < stats.depth) statsFile.stats.Add(stats);
            else statsFile = new StatsFile { stats = new List<Stats> { stats } };
            string jsonString = JsonSerializer.Serialize(statsFile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fileName, jsonString);
        } else
        {
            var statsFile = new StatsFile { stats = new List<Stats> { stats } };
            string jsonString = JsonSerializer.Serialize(statsFile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fileName, jsonString);
        };

        // Reset stats
        stats.nodesTraversed = 0;
        stats.hashUsage = 0;
    }

    public Move Think(Board board, Timer timer)
    {
        // Restart statistics counting
        stats = new();
        stats.hashUsageFullDepths = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        Move m = IterativeDeepening(timer, board);
        if ((transpositionTable.Count * entrySize / ttSize) > 0.95) transpositionTable.Clear();
        return m;
    }
}