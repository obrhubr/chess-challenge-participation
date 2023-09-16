using ChessChallenge.Application;
using ChessChallenge.Example;
using static ChessChallenge.Application.ChallengeController;

namespace Chess_Challenge.src.My_Bot
{
    internal class VersionHelper
    {
        public static ChessPlayer getLatest(PlayerType type, int GameDurationMilliseconds)
        {
            return new ChessPlayer(new MyBot_v7_0_1(), type, GameDurationMilliseconds);
        }

        public static ChessPlayer getPrevious(PlayerType type, int GameDurationMilliseconds)
        {
            return new ChessPlayer(new MyBot_v8_0_0(), type, GameDurationMilliseconds);
        }

        public static string getLatestFilename()
        {
            return "MyBot_v8_0_1.cs";
        }

        public static MyBot_v6_3_3 getLatestEval()
        {
            return new MyBot_v6_3_3();
        }
    }
}
