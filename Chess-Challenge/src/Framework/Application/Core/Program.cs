using Raylib_cs;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System;
using System.Collections.Concurrent;
using Chess_Challenge.src.My_Bot;
using Microsoft.VisualBasic;
using ChessChallenge.Bot;
using ChessChallenge.Chess;

namespace ChessChallenge.Application
{
    static class Program
    {
        const bool hideRaylibLogs = true;
        static Camera2D cam;

        public static void Main(string[] args)
        {
            //BenchmarkParallel(args);
            //Benchmark(args);
            Graphical();
            //UCI();
            //Training(args);
            //CheckMates(args);
            //GetEvaluation();
            //TimeSim();
        }

        public static void TimeSim()
        {
            var Times = new List<(int, int)> { 
                (60000, 0), // Bullet 1+0 
                (60000, 1000), // Bullet 1+1
                (60000*2, 1000), // Bullet 2+1
                (60000*3, 0), // Blitz 3+0
                (60000*3, 1000*2), // Blitz 3+2
                (60000*5, 0), // Blitz 5+0
                (60000*5, 1000*3), // Blitz 5+3
            };
            var csv = "";

            foreach (var (startTime, increase) in Times)
            {
                var thinkingTime = 0;
                var piecesLeft = 36;
                var remaining = startTime;
                var i = 0;

                var records = new List<int>();

                while (remaining > 0)
                {
                    if (piecesLeft == 0) break;

                    // Calculate thinking time: totalTime * endGameReserve / avgMovesUntilEndgameStarts + increment
                    thinkingTime = piecesLeft > 20 ? ((int)(startTime * 0.01) + increase) : ((int)(startTime * 0.05) + increase);
                    // Emergency stop to prevent time outs if thinkingTime is bigger than the remaining time
                    thinkingTime = (thinkingTime - increase) * 2 > remaining ? (remaining / 4 > 200 ? remaining / 4 : 200) : thinkingTime;

                    records.Add(thinkingTime);

                    // Update times
                    if (piecesLeft > 20) piecesLeft -= i % 3 == 0 ? 1 : 0;
                    else piecesLeft -= i % 5 == 0 ? 1 : 0;

                    remaining += increase;
                    remaining -= thinkingTime;
                    i++;
                }

                csv += String.Join(", ", records.ToArray()) + "\n";
            }

            File.WriteAllText("../../../../thinking-time/data.csv", csv);
        }

        public static void GetEvaluation()
        {
            var fen = "1k6/P3n3/5p2/1KR4P/P7/2N5/5r2/8 b - - 0 67";
            var chessBoard = new Chess.Board();
            chessBoard.LoadPosition(fen);
            var board = new API.Board(chessBoard);
            Console.WriteLine("Evaluation: " + VersionHelper.getLatestEval().Evaluate(board) +
                "Best Move: " + VersionHelper.getLatestEval().Think(board, new API.Timer(10000)).ToString());
        }

        public static void UCI()
        {
            var engine = new UciEngine();
            var message = string.Empty;
            while (message != "quit")
            {
                message = Console.ReadLine();
                if (!string.IsNullOrEmpty(message)) engine.ReceiveCommand(message);
            }
        }

        public static void Training(string[] args)
        {
            var trainer = new Training();

            Stopwatch totalTime = Stopwatch.StartNew();
            var begin = totalTime.ElapsedMilliseconds;

            trainer.UseGA();

            ConsoleHelper.Log("Finished execution in " + (totalTime.ElapsedMilliseconds - begin).ToString() + " ms.");
        }

        public static void CheckMates(string[] args)
        {
            List<string> fens = new List<string>{
                "1k6/P3n3/5p2/1KR4P/P7/2N5/5r2/8 b - - 0 67",
                "6rk/pp2bp1p/4p3/2p1Pp2/2P1bPP1/7P/4Q1rN/2q1KR2 w - - 0 1",
                // 2 moves
                "3Q4/8/6k1/5p2/5B1p/4R2P/N4PP1/6K1 w - - 0 1",
                // 1 move
                "8/8/8/8/8/1K6/1NR5/k7 w - - 0 1",
                // 1 move
                "6k1/4Rppp/8/8/8/8/5PPP/6K1 w - - 0 1",
                // 3 moves
                "5rk1/1R2R1pp/8/8/8/8/8/1K6 w - - 0 1",
                // 2 moves
                "2r1r1k1/5ppp/8/8/Q7/8/5PPP/4R1K1 w - - 0 1",
                // Placeholder
                "6k1/4Rppp/8/8/8/8/5PPP/6K1 w - - 0 1"
            };

            BenchmarkController controller = new(fens.ToArray());

            controller.StartNewBotMatch(ChallengeController.PlayerType.MyBotLatest, ChallengeController.PlayerType.MyBotLatest);
            Stopwatch sw = Stopwatch.StartNew();
            var now = sw.ElapsedMilliseconds / 1000.0d;

            var gameNumber = 0;

            while (controller.CurrGameNumber < controller.TotalGameCount-1)
            {
                controller.Update((sw.ElapsedMilliseconds / 1000.0d) - now);
                now = sw.ElapsedMilliseconds / 1000.0d;
            }

            Console.WriteLine("Latest wins: " + controller.BotStatsA.NumWins + " Previous Wins: " + controller.BotStatsB.NumWins);
        }

        public static void Benchmark(string[] args)
        {
            Stopwatch totalTime = Stopwatch.StartNew();
            var begin = totalTime.ElapsedMilliseconds;

            var fens = FileHelper.ReadResourceFile("Fens.txt").Split('\n');

            const int numGames = 100;

            List<(BenchmarkController.BotMatchStats, BenchmarkController.BotMatchStats)> games = new();

            games.Add(runGames(fens[0..numGames].ToArray(),
                (
                    ChallengeController.PlayerType.MyBotLatest,
                    ChallengeController.PlayerType.MyBotPrevious
                ))
            );

            ConsoleHelper.Log("Finished execution in " + (totalTime.ElapsedMilliseconds - begin).ToString() + " ms.");

            ToJson(games.ToList(), numGames, (totalTime.ElapsedMilliseconds - begin));
        }

        public static void BenchmarkParallel(string[] args)
        {
            Stopwatch totalTime = Stopwatch.StartNew();
            var begin = totalTime.ElapsedMilliseconds;

            var fens = FileHelper.ReadResourceFile("Fens.txt").Split('\n');

            const int parallelGames = 1;
            const int gamesPerThread = 100;

            ConcurrentBag<(BenchmarkController.BotMatchStats, BenchmarkController.BotMatchStats)> games = new();

            Parallel.For(0, parallelGames, new ParallelOptions { MaxDegreeOfParallelism = parallelGames }, 
                i => games.Add(
                    runGames(fens[(i*gamesPerThread)..((i+1)*gamesPerThread)].ToArray(), 
                    (
                        ChallengeController.PlayerType.MyBotLatest,
                        ChallengeController.PlayerType.MyBotPrevious
                    ))
                )
            );

            ConsoleHelper.Log("Finished execution in " + (totalTime.ElapsedMilliseconds - begin).ToString() + " ms.");

            ToJson(games.ToList(), parallelGames * gamesPerThread, (totalTime.ElapsedMilliseconds - begin));
        }

        static void ToJson(List<(BenchmarkController.BotMatchStats, BenchmarkController.BotMatchStats)> runs, int NumGames, long RunTime)
        {
            string botNames = runs.First().Item1.BotName.ToString() + "_" + runs.First().Item2.BotName.ToString();

            // Create GameData object
            GameData gameData = new GameData
            {
                StatsA = new GameStats
                {
                    Name = runs.First().Item1.BotName
                },
                StatsB = new GameStats
                {
                    Name = runs.First().Item2.BotName
                },
                RunTime = RunTime,
                NumGames = NumGames
            };

            foreach (var stats in runs)
            {
                gameData.StatsA.NumWins += stats.Item1.NumWins;
                gameData.StatsA.NumDraws += stats.Item1.NumTimeouts;
                gameData.StatsA.NumTimeouts += stats.Item1.NumTimeouts;
                gameData.StatsA.NumLosses += stats.Item1.NumLosses;
                gameData.StatsA.NumIllegalMoves += stats.Item1.NumIllegalMoves;

                gameData.StatsB.NumWins += stats.Item2.NumWins;
                gameData.StatsB.NumDraws += stats.Item2.NumDraws;
                gameData.StatsB.NumTimeouts += stats.Item2.NumTimeouts;
                gameData.StatsB.NumLosses += stats.Item2.NumLosses;
                gameData.StatsB.NumIllegalMoves += stats.Item2.NumIllegalMoves;
            }

            string jsonString = JsonSerializer.Serialize(gameData);
            File.WriteAllText("../../../../stats/" + DateAndTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + "_" + botNames + ".json", jsonString);
        }

        public class GameData
        {
            public GameStats StatsA { get; set; }
            public GameStats StatsB { get; set; }
            public int NumGames { get; set; }
            public long RunTime { get; set; }
        }

        public class GameStats
        {
            public String Name { get; set; }
            public int NumWins { get; set; }
            public int NumDraws { get; set; }
            public int NumTimeouts { get; set; }
            public int NumLosses { get; set; }
            public int NumIllegalMoves { get; set; }
        }

        static (BenchmarkController.BotMatchStats, BenchmarkController.BotMatchStats) runGames(string[] fens, (ChallengeController.PlayerType, ChallengeController.PlayerType) bots)
        {
            BenchmarkController controller = new(fens);

            controller.StartNewBotMatch(bots.Item1, bots.Item2);
            Stopwatch sw = Stopwatch.StartNew();
            var now = sw.ElapsedMilliseconds / 1000.0d;

            while (controller.CurrGameNumber <= fens.Length)
            {
                controller.Update((sw.ElapsedMilliseconds / 1000.0d) - now);
                now = sw.ElapsedMilliseconds / 1000.0d;
            }

            return (controller.BotStatsA, controller.BotStatsB);
        }

        public static void Graphical()
        {
            Vector2 loadedWindowSize = GetSavedWindowSize();
            int screenWidth = (int)loadedWindowSize.X;
            int screenHeight = (int)loadedWindowSize.Y;

            if (hideRaylibLogs)
            {
                unsafe
                {
                    Raylib.SetTraceLogCallback(&LogCustom);
                }
            }

            Raylib.InitWindow(screenWidth, screenHeight, "Chess Coding Challenge");
            Raylib.SetTargetFPS(60);

            UpdateCamera(screenWidth, screenHeight);

            ChallengeController controller = new();

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(22, 22, 22, 255));
                Raylib.BeginMode2D(cam);

                controller.Update();
                controller.Draw();

                Raylib.EndMode2D();

                controller.DrawOverlay();

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();

            controller.Release();
            UIHelper.Release();
        }

        public static void SetWindowSize(Vector2 size)
        {
            Raylib.SetWindowSize((int)size.X, (int)size.Y);
            UpdateCamera((int)size.X, (int)size.Y);
            SaveWindowSize();
        }

        public static Vector2 ScreenToWorldPos(Vector2 screenPos) => Raylib.GetScreenToWorld2D(screenPos, cam);

        static void UpdateCamera(int screenWidth, int screenHeight)
        {
            cam = new Camera2D();
            cam.target = new Vector2(0, 15);
            cam.offset = new Vector2(screenWidth / 2f, screenHeight / 2f);
            cam.zoom = screenWidth / 1280f * 0.7f;
        }


        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static unsafe void LogCustom(int logLevel, sbyte* text, sbyte* args)
        {
        }

        static Vector2 GetSavedWindowSize()
        {
            if (File.Exists(FileHelper.PrefsFilePath))
            {
                string prefs = File.ReadAllText(FileHelper.PrefsFilePath);
                if (!string.IsNullOrEmpty(prefs))
                {
                    if (prefs[0] == '0')
                    {
                        return Settings.ScreenSizeSmall;
                    }
                    else if (prefs[0] == '1')
                    {
                        return Settings.ScreenSizeBig;
                    }
                }
            }
            return Settings.ScreenSizeSmall;
        }

        static void SaveWindowSize()
        {
            Directory.CreateDirectory(FileHelper.AppDataPath);
            bool isBigWindow = Raylib.GetScreenWidth() > Settings.ScreenSizeSmall.X;
            File.WriteAllText(FileHelper.PrefsFilePath, isBigWindow ? "1" : "0");
        }

      

    }


}