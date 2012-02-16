using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameModel
{
    class Board
    {
        /// <summary>
        /// Array of 64 cards, from left to right, top to bottom.
        /// </summary>
        private List<Card> _Cards = new List<Card>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="squares"></param>
        /// <param name="status"></param>
        public Board(int numberOfCards)
        {
            int fo = (numberOfCards * numberOfCards) / 2;
            _Cards.Clear();
            Card tp;
            for (int i = 0; i < fo; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    do
                    {
                        tp = new Card()
                        {
                            myXCoordinate = F(numberOfCards),
                            myYCoordinate = F(numberOfCards)
                        };

                        if (!CardAlreadyExists(tp)) break;

                    } while (true);


                    this._Cards.Add(tp);
                }

                EstablishMatchLink();
            }
        }

        static Random _r = new Random();
        static int F(int range)
        {
            // Use class-level Random so that when this
            // method is called many times, it still has
            // good Randoms.
            return _r.Next(range);
            // If this declared a local Random, it would
            // repeat itself.
        }

        List<Card> getCards()
        {
            return this._Cards;
        }

        void EstablishMatchLink()
        {
            int e = _Cards.Count - 1, n = e - 1;
            _Cards[n].partnerXCoordinate = _Cards[e].myXCoordinate;
            _Cards[n].partnerYCoordinate = _Cards[e].myYCoordinate;
            _Cards[e].partnerXCoordinate = _Cards[n].myXCoordinate;
            _Cards[e].partnerYCoordinate = _Cards[n].myYCoordinate;
        }

        bool CardAlreadyExists(Card p)
        {
            foreach (Card v in _Cards)
            {
                if (v.myXCoordinate == p.myXCoordinate && v.myYCoordinate == p.myYCoordinate) return true;
            }
            return false;
        }

        bool MatchCheck()
        {
            Card p1 = (from f in _Cards where f.firstGuess == true select f).FirstOrDefault();
            Card p2 = (from j in _Cards where j.secondGuess == true select j).FirstOrDefault();
            if (p1 == null || p2 == null) return false;
            if (p1.myXCoordinate == p2.partnerXCoordinate && p1.myYCoordinate == p2.partnerYCoordinate) return true;
            return false;
        }
    }
    public struct BoardStatus
    {
        private int moves;
        private int numCardsLeft;
    }
}
