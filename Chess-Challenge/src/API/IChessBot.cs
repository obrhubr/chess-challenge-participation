
using System;

namespace ChessChallenge.API
{
    public interface IChessBot
    {
        public bool Stop { get; set; }
        Move Think(Board board, Timer timer);
    }
}
