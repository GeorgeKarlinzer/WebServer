using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class Client
    {
        public int Score { get; set; }
        public bool IsWon { get; set; }
        public List<string> guessedLetters;

        public Client(int score, bool isWon, List<string> guessedLetters)
        {
            Score = score;
            IsWon = isWon;
            this.guessedLetters = new(guessedLetters);
        }
    }
}
