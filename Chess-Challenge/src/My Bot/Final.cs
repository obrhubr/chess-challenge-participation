using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// Reduce size and submit final version
class Final
{
    // The value of: null, pawn, knight, bishop, rook, queen, king
    double[] PieceValues = { 0, 1.0, 3, 3, 4.5, 8, 0 };
    // Initialise value with this number
    double Max = 99999;
    int thinkingTime = 1000;
    // Transposition table
    struct TranspositionNode
    {
        public int Depth;
        public int Flag;
        public double Evaluation;
        public Move Move;
        public bool Mate;
        public int Ply;
    }
    Dictionary<ulong, TranspositionNode> transpositionTable = new();

    double Evaluate(Board board)
    {
        // Evaluate Checkmate with the score assigned to mate (should be impossible to reach with normal evals)
        if (board.IsInCheckmate()) return (4000 - board.PlyCount) * (board.IsWhiteToMove ? -1 : 1);

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
            score += board.GetLegalMoves().Count() * 1 / 70 * (board.IsWhiteToMove ? 1 : -1);
            board.UndoSkipTurn();
            score += board.GetLegalMoves().Count() * 1 / 70 * (board.IsWhiteToMove ? 1 : -1);
        }

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
                if (((BitboardHelper.GetPawnAttacks(pawn.Square, board.IsWhiteToMove) >> move.TargetSquare.Index) & 1) != 0) return -10;
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
        else return 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
    }

    double QuiescenceSearch(Timer timer, Board board, double alpha, double beta)
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

    (double, Move) Minimax(Timer timer, Board board, int depth, int maxDepth, double alpha, double beta, Move lastBestMove, int numExtensions)
    {
        if (board.PlyCount > 0 && depth < maxDepth && (board.IsRepeatedPosition() || board.FiftyMoveCounter >= 100)) return (-Max, new Move());

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
            if (numExtensions < 16 && (board.IsInCheck() || (move.MovePieceType == PieceType.Pawn && (move.TargetSquare.Rank == 1 || move.TargetSquare.Rank == 6))))
            {
                numExtensions++;
                searchDepth++;
            }
            // If the move is later in the move list search less deeply
            else if (!move.IsCapture && depth >= 3 && i >= 3) searchDepth--;
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
        if (!tNode.Mate && Math.Abs(value) > (4000 - 1000) && Math.Abs(value) < Max)
        {
            tNode.Mate = true;
            tNode.Evaluation = 4000 * (board.IsWhiteToMove ? -1 : 1) * -1;
            tNode.Ply = board.PlyCount;
        }
        transpositionTable[board.ZobristKey] = tNode;

        return (value, bestMove);
    }

    public Move Think(Board board, Timer timer)
    {
        // Calculate thinking time: totalTime * endGameReserve / avgMovesUntilEndgameStarts + increment
        var multi = board.GetAllPieceLists().Select(x => x.Count).Aggregate((x, y) => x + y) > 20 ? 0.01 : 0.05;
        thinkingTime =  (int)(timer.GameStartTimeMilliseconds * multi) + timer.IncrementMilliseconds;
        // Emergency stop to prevent time outs if thinkingTime is bigger than the remaining time
        thinkingTime = (thinkingTime - timer.IncrementMilliseconds) * 2 > timer.MillisecondsRemaining ? timer.MillisecondsRemaining/4 : thinkingTime;

        Move bestMove = new();
        int depth = 4;

        while (timer.MillisecondsElapsedThisTurn < (thinkingTime + 2))
        {
            if (depth >= 20) break;
            var (_, move) = Minimax(timer, board, depth, depth, -Max, Max, bestMove, 16);
            if (!move.IsNull) bestMove = move;

            depth++;
        }

        return bestMove;
    }
}