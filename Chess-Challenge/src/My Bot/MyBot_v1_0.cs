using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot_v1_0 : IChessBot
{

    // Add a stop flag for unlimited mode
    private bool _stop;
    public bool Stop { get => _stop; set => _stop = value; }

    public (Double, Double) evaluate(PieceList[] pieces)
    {
        Double sumW = 0;
        Double sumB = 0;

        foreach (PieceList plist in pieces)
        {
            Double sum = 0;
            if (plist.TypeOfPieceInList == PieceType.Pawn) {
                sum += 1 * plist.Count;
            } else if (plist.TypeOfPieceInList == PieceType.Rook)
            {
                sum += 4.5 * plist.Count;
            }
            else if (plist.TypeOfPieceInList == PieceType.Knight)
            {
                sum += 3 * plist.Count;
            }
            else if (plist.TypeOfPieceInList == PieceType.Bishop)
            {
                sum += 3 * plist.Count;
            }
            else if (plist.TypeOfPieceInList == PieceType.Queen)
            {
                sum += 8 * plist.Count;
            }

            if (plist.IsWhitePieceList)
            {
                sumW += sum;
            }
            else
            {
                sumB += sum;
            }
        }

        return (sumW, sumB);
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        List<(Move, double)> eval = new List<(Move, double)>();

        foreach (Move m in moves)
        {
            board.MakeMove(m);
            Move[] moves2 = board.GetLegalMoves();

            foreach (Move m2 in moves2)
            {
                board.MakeMove(m2);
                
                PieceList[] pieces = board.GetAllPieceLists();
                (Double, Double) values = evaluate(pieces);

                // IsWhiteToMove == True -> Means we are black
                // Score is ours - his -> bigger is better 
                Double score;
                if (board.IsWhiteToMove)
                {
                    score = values.Item2 - values.Item1;
                }
                else
                {
                    score = values.Item1 - values.Item2;
                }
                eval.Add((m, score));

                board.UndoMove(m2);
            }

            board.UndoMove(m);
        }

        var moveListGrouped = eval.GroupBy(m => m.Item1).Select(grp => grp.ToList()).ToList();
        var moveListsOrdered = moveListGrouped.Select(grp => grp.OrderBy(m => m.Item2).ToList()).ToList();
        var chosenMove = moveListsOrdered.OrderBy(grp => grp.First().Item2).First().Last();

        return chosenMove.Item1;
    }
}