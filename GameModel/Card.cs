using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameModel
{
    public class Card
    {
        public int myXCoordinate { get; set; }
        public int myYCoordinate { get; set; }
        public int partnerXCoordinate { get; set; }
        public int partnerYCoordinate { get; set; }
        public bool firstGuess { get; set; }
        public bool secondGuess { get; set; }
    }
}
