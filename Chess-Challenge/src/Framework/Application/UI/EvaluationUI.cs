using Raylib_cs;
using System;
using Chess_Challenge.src.My_Bot;

namespace ChessChallenge.Application
{
    public static class EvaluationUI
    {
        static readonly Color black = new(0, 0, 0, 255);
        static readonly Color white = new(255, 255, 255, 255);
        static readonly Color background = new Color(40, 40, 40, 255);

        public static void Draw(ChallengeController controller)
        {
            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            int width = UIHelper.ScaleInt(60);
            int heightOffset = UIHelper.ScaleInt(60);
            int fontSize = UIHelper.ScaleInt(35);

            // Bg

            Raylib.DrawRectangle(screenWidth - width - 3, heightOffset - 3, screenWidth + 6, (screenHeight - heightOffset * 2) + 6, Color.GREEN);
            Raylib.DrawRectangle(screenWidth - width, heightOffset, screenWidth, screenHeight - heightOffset * 2, white);
            // Bar
            API.Board botBoard = new(controller.board);
            double eval = Chess_Challenge.src.My_Bot.VersionHelper.getLatestEval().Evaluate(botBoard);
            double evaluation = Math.Min(10, Math.Max(-10, eval));
            double multiplier = 1.0 - (evaluation + 10.0) / 20.0;
            Raylib.DrawRectangle(screenWidth - width, heightOffset, screenWidth, (int)((screenHeight - heightOffset * 2) * multiplier), black);

            var textPos = new System.Numerics.Vector2(screenWidth - width / 2, screenHeight / 2);
            string text = $"{Math.Round(evaluation, 2)}";
            UIHelper.DrawText(text, textPos, fontSize, 1, Color.RED, UIHelper.AlignH.Centre);
        }
    }
}