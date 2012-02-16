using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace HappyFriendMemoryGame.WcfService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IGameBoardService
    {
        [OperationContract]
        string GetData(int value);

        [OperationContract]
        CompositeType GetDataUsingDataContract(CompositeType composite);

        [OperationContract]
        Board getNewBoard(int value);

        // TODO: Add your service operations here
    }


    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    [DataContract]
    public class CompositeType
    {
        bool boolValue = true;
        string stringValue = "Hello ";

        [DataMember]
        public bool BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        [DataMember]
        public string StringValue
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
    }

    [DataContract]
    public class Card
    {
        private int _myXCoordinate;
        [DataMember]
        public int myXCoordinate
        {
            get { return _myXCoordinate; }
            set { _myXCoordinate = value; }
        }
        private int _myYCoordinate;
        [DataMember]
        public int myYCoordinate
        {
            get { return _myYCoordinate; }
            set { _myYCoordinate = value; }
        }
        private int _partnerXCoordinate;
        [DataMember]
        public int partnerXCoordinate
        {
            get { return _partnerXCoordinate; }
            set { _partnerXCoordinate = value; }
        }
        private int _partnerYCoordinate;
        [DataMember]
        public int partnerYCoordinate
        {
            get { return _partnerYCoordinate; }
            set { _partnerYCoordinate = value; }
        }
        private bool _firstGuess;
        [DataMember]
        public bool firstGuess
        {
            get { return _firstGuess; }
            set { _firstGuess = value; }
        }
        private bool _secondGuess;
        [DataMember]
        public bool secondGuess
        {
            get { return _secondGuess; }
            set { _secondGuess = value; }
        }
    }

    [DataContract]
    public class Board
    {
        /// <summary>
        /// Array of 64 cards, from left to right, top to bottom.
        /// </summary>
        private List<Card> _cards = new List<Card>();

        [DataMember]
        public List<Card> Cards
        {
            get { return _cards; }
            set { _cards = value; }
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="squares"></param>
        /// <param name="status"></param>
        public Board(int currentLevel)
        {
            int numberOfCards = (currentLevel * currentLevel) / 2;
            _cards.Clear();
            Card tp;
            for (int i = 0; i < numberOfCards; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    do
                    {
                        tp = new Card()
                        {
                            myXCoordinate = F(currentLevel),
                            myYCoordinate = F(currentLevel)
                        };

                        if (!CardAlreadyExists(tp)) break;

                    } while (true);


                    _cards.Add(tp);
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

        /*
        public List<Card> getCards()
        {
            return _Cards;
        }
        */
        void EstablishMatchLink()
        {
            int e = _cards.Count - 1, n = e - 1;
            _cards[n].partnerXCoordinate = _cards[e].myXCoordinate;
            _cards[n].partnerYCoordinate = _cards[e].myYCoordinate;
            _cards[e].partnerXCoordinate = _cards[n].myXCoordinate;
            _cards[e].partnerYCoordinate = _cards[n].myYCoordinate;
        }

        bool CardAlreadyExists(Card p)
        {
            foreach (Card v in _cards)
            {
                if (v.myXCoordinate == p.myXCoordinate && v.myYCoordinate == p.myYCoordinate) return true;
            }
            return false;
        }

        public bool MatchCheck()
        {
            Card p1 = (from f in _cards where f.firstGuess == true select f).FirstOrDefault();
            Card p2 = (from j in _cards where j.secondGuess == true select j).FirstOrDefault();
            if (p1 == null || p2 == null) return false;
            if (p1.myXCoordinate == p2.partnerXCoordinate && p1.myYCoordinate == p2.partnerYCoordinate) return true;
            return false;
        }
    }
}
