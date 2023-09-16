using ChessChallenge.Application;
using ChessChallenge.Example;
using static ChessChallenge.Application.ChallengeController;

namespace Chess_Challenge.src.My_Bot
{
    internal class VersionHelper
    {
        // Get the Latest and Previous bots (that should play against each other)
        public static ChessPlayer getLatest(PlayerType type, int GameDurationMilliseconds)
        {
            return new ChessPlayer(new MyBot_v7_0_1(), type, GameDurationMilliseconds);
        }

        public static ChessPlayer getPrevious(PlayerType type, int GameDurationMilliseconds)
        {
            return new ChessPlayer(new MyBot_v8_0_0(), type, GameDurationMilliseconds);
        }

        // Get the file name for which to count the tokens
        public static string getLatestFilename()
        {
            return "Final.cs";
        }

        // Return bot to display current evaluation on the GUI
        public static MyBot_v6_3_3 getLatestEval()
        {
            return new MyBot_v6_3_3();
        }
    }
}
