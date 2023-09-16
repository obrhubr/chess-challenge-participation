using ChessChallenge.API;
using ChessChallenge.Application;
using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.VisualBasic;
using System.IO;
using System.Reflection;

namespace Chess_Challenge.src.My_Bot
{
    internal class Training
    {
        static Random rnd = new Random();

        // Pawn bitboard
        static double[] MultiplierPawns = {
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
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
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
        };

        static double[] CreateRandomMultiplier()
        {
            List<double> multi = new();
            foreach (var i in Enumerable.Range(0, 64))
            {
                multi = multi.Append(rnd.NextDouble() * 20.0 - 10.0).ToList();
            }
            return multi.ToArray();
        }

        static Weights GetRandomWeights()
        {
            return new Weights(
                CreateRandomMultiplier(),
                CreateRandomMultiplier(),
                CreateRandomMultiplier(),
                CreateRandomMultiplier(),
                CreateRandomMultiplier(),
                CreateRandomMultiplier()
            );
        }

        static double[] PermuteMultiplier(double[] old)
        {
            List<double> multi = new();
            foreach (var i in Enumerable.Range(0, 64))
            {
                multi = multi.Append(Math.Min(10.0, old[i] + (rnd.NextDouble()-0.5))).ToList();
            }
            return multi.ToArray();
        }

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

        double Evaluate(Board board, double[] mP, double[] mN, double[] mB, double[] mR, double[] mQ, double[] mK)
        {
            PieceList[] pieces = board.GetAllPieceLists();
            double score = 0;

            foreach (PieceList plist in pieces)
            {
                score += MultiplyBitboards(board, plist.TypeOfPieceInList, plist.IsWhitePieceList, mP, mN, mB, mR, mQ, mK)
                    * (plist.IsWhitePieceList ? 1 : -1);
            }

            // negative if black is better, positive if white is better
            return score;
        }

        void printDiagram(ChessChallenge.Chess.Board board)
        {
            var b = new Board(board);
            Console.WriteLine(b.CreateDiagram());
        }

        public struct AnalysisFile
        {
            public AnalysisData[] positions { get; set; }
        }

        public struct AnalysisData
        {
            public string fen { get; set; }
            public double eval { get; set; }
        }

        public struct WeightsFile
        {
            public double Fitness { get; set; }
            public Weights Weights { get; set; }
        }

        public struct Weights
        {
            public Weights(double[] mP, double[] mN, double[] mB, double[] mR, double[] mQ, double[] mK)
            {
                MultiplierPawns = mP;
                MultiplierKnights = mN;
                MultiplierBishops = mB;
                MultiplierRooks = mR;
                MultiplierQueens = mQ;
                MultiplierKing = mK;
            }

            public double[] MultiplierPawns { get; set; }
            public double[] MultiplierKnights { get; set; }
            public double[] MultiplierBishops { get; set; }
            public double[] MultiplierRooks { get; set; }
            public double[] MultiplierQueens { get; set; }
            public double[] MultiplierKing { get; set; }
        }

        public void Export(int index, double fitness, Weights w)
        {
            WeightsFile data = new()
            {
                Fitness = fitness,
                Weights = w
            };

            string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { IncludeFields = true });
            File.WriteAllText("../../../../stats/Training/" + "TRAINING_" + DateAndTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + "_" + index + ".json", jsonString);
        }

        public Weights Import()
        {
            //return GetRandomWeights();
            var directory = new DirectoryInfo("../../../../stats/Training/");
            var file = directory.GetFiles().OrderByDescending(f => f.LastWriteTime).Where(x => x.Name.Contains("TRAINING")).FirstOrDefault();
            if (file is null) {
                Console.WriteLine("Could not import Weights, initialising with random values!");
                return GetRandomWeights();
            }

            var jsonString = File.ReadAllText(file.FullName);
            WeightsFile? imported = JsonSerializer.Deserialize<WeightsFile>(jsonString, new JsonSerializerOptions { IncludeFields = true });
            if (imported is null)
            {
                Console.WriteLine("Could not import Weights, initialising with random values!");
                return GetRandomWeights();
            }

            return imported.Value.Weights;
        }

        public double Fitness(AnalysisFile fens, double[] mP, double[] mN, double[] mB, double[] mR, double[] mQ, double[] mK)
        {
            var board = new ChessChallenge.Chess.Board();
            var fitness = 0.0;
            foreach (var ad in fens.positions)
            {
                board.LoadPosition(ad.fen);
                var difference = ad.eval - Evaluate(new Board(board), mP, mN, mB, mR, mQ, mK);
                fitness += Math.Abs(difference);
            }
            return fitness;
        }

        public void UseGA()
        {
            var fensJson = FileHelper.ReadResourceFile("analysis.json");
            var fens = JsonSerializer.Deserialize<AnalysisFile>(fensJson);

            // Baseline:
            //Console.WriteLine("Baseline fitness: " + Fitness(fens, MultiplierPawns, MultiplierKnights, MultiplierBishops, MultiplierRooks, MultiplierQueens, MultiplierKing));

            Weights imported = Import();
            Console.WriteLine("Imported Weights.");
            MultiplierPawns = imported.MultiplierPawns;
            MultiplierKnights = imported.MultiplierKnights;
            MultiplierBishops = imported.MultiplierBishops;
            MultiplierRooks = imported.MultiplierRooks;
            MultiplierQueens = imported.MultiplierQueens;
            MultiplierKing = imported.MultiplierKing;

            var f = Fitness(fens, MultiplierPawns, MultiplierKnights, MultiplierBishops, MultiplierRooks, MultiplierQueens, MultiplierKing);
            Console.WriteLine("Fitness: " + f);

            foreach (var i in Enumerable.Range(0, 10000))
            {
                ConcurrentBag<(double, Weights)> multis = new();
                Parallel.For(0, 200,
                    i =>
                    {
                        // Permute
                        var mP = PermuteMultiplier(MultiplierPawns);
                        var mN = PermuteMultiplier(MultiplierKnights);
                        var mB = PermuteMultiplier(MultiplierBishops);
                        var mR = PermuteMultiplier(MultiplierRooks);
                        var mQ = PermuteMultiplier(MultiplierQueens);
                        var mK = PermuteMultiplier(MultiplierKing);
                        var f = Fitness(fens, mP, mN, mB, mR, mQ, mK);
                        multis.Add((f, new Weights(mP, mN, mB, mR, mQ, mK)));
                        Console.WriteLine("Permutation fitness, iteration " + i + " : " + f);
                    }
                );

                // Choose best
                var best = multis.ToList().OrderBy(x => x.Item1).ToList().First();
                if (best.Item1 < f)
                {
                    Console.WriteLine("New Best Fitness: " + best.Item1);

                    MultiplierPawns = best.Item2.MultiplierPawns;
                    MultiplierKnights = best.Item2.MultiplierKnights;
                    MultiplierBishops = best.Item2.MultiplierBishops;
                    MultiplierRooks = best.Item2.MultiplierRooks;
                    MultiplierQueens = best.Item2.MultiplierQueens;
                    MultiplierKing = best.Item2.MultiplierKing;

                    f = best.Item1;

                    Export(i, best.Item1, best.Item2);
                }
                else Console.WriteLine("Could not permute better weights (best: " + best.Item1 + "), recalculating...");
            }
        }
    }
}
