using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameModel
{
    /// <summary>
    /// Implements a memory match game.
    /// </summary>
    class Game
    {
        private int _currentLevel;
        public int CurrentLevel
        {
            get { return _currentLevel; }
            set { _currentLevel = value; }
        }
        
    }

    /// <summary>
    /// Implements the game status.
    /// </summary>
    public struct GameStatus
    {
        private bool playerTurn;
    }
}
