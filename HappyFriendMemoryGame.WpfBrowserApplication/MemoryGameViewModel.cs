using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows;
using HappyFriendMemoryGame.ControlContent3D;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using HappyFriendMemoryGame.WpfBrowserApplication.ServiceReference1;
using HappyFriendMemoryGame.WcfService;
using System.ComponentModel;

namespace HappyFriendMemoryGame.WpfBrowserApplication
{
    [ValueConversion(typeof(int), typeof(string))]
    public class StatusMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int)
            {
                return string.Format("{0} Match {1} left to find", value, (int)value == 1 ? "" : "s");
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class VictoryTimeMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is TimeSpan)
            {
                TimeSpan time = (TimeSpan)value;
                return string.Format("Finished matching in {0} minutes, {1} seconds", time.Minutes, time.Seconds);
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CardCollectionViewModel : INotifyPropertyChanged
    {
        #region Instance Fields

        bool _showDetailedView;
        TimeSpan _timeSpent;
        GameBoardServiceClient client;
        private int _numCardsLeft;
        private int _missedMatches;
        readonly ObservableCollection<CardViewModel> _Cards;

        #endregion

        #region Constructor

        public CardCollectionViewModel()
        {
            CurrentLevel = 4;
            Difficulty = 5;
            _numCardsLeft = (CurrentLevel * CurrentLevel) / 2;
            client = new GameBoardServiceClient();
            GameBoard = client.getNewBoard(CurrentLevel);
            _Cards = new ObservableCollection<CardViewModel>();
            TurnCount = 1;

            string picName = null;
            int u = 0;
            int randomIdx = 1;
            List<int> numberList = new List<int>(Enumerable.Range(1, NumCardsLeft).ToList());

            foreach (Card card in GameBoard.Cards)
            {
                u++;
                if (u % 2 == 1)
                {
                    Random rndElement = new Random();
                    randomIdx = rndElement.Next(0, numberList.Count - 1);
                    picName = "snowflake" + numberList[randomIdx] + ".jpg";
                    numberList.RemoveAt(randomIdx);
                }
                CardViewModel cvm = new CardViewModel("PictureNumber" + randomIdx, picName);
                cvm.myXCoordinate = card.myXCoordinate;
                cvm.myYCoordinate = card.myYCoordinate;
                _Cards.Add(cvm);
            }
        }

        #endregion

        #region Properties

        public Board GameBoard { get; set; }
        public int TurnCount { get; private set; }
        public int CurrentLevel { get; private set; }
        public double Difficulty { get; set; }
        public int NumCardsLeft
        {
            get
            {
                return _numCardsLeft;
            }
            set
            {
                _numCardsLeft = value;
                NotifyPropertyChanged("NumCardsLeft");
            }
        }

        public bool ShowDetailedView
        {
            get { return _showDetailedView; }
            set { _showDetailedView = value; }
            /*
            {
                if (value == _showDetailedView)
                    return;

                _showDetailedView = value;

                foreach (CardViewModel Card in _Cards)
                    Card.VerifyCorrectSideIsInView(_showDetailedView);
            }*/
        }

        /// <summary>
        /// Whole time of game
        /// </summary>
        public TimeSpan TimeSpent
        {
            get { return _timeSpent; }
            set
            {
                _timeSpent = value;
                NotifyPropertyChanged("TimeSpent");
            }
        }

        /// <summary>
        /// Number of Matches missed
        /// </summary>
        public int MissedMatches
        {
            get { return _missedMatches; }
            set
            {
                _missedMatches = value;
                NotifyPropertyChanged("MissedMatches");
            }
        }

        public ObservableCollection<CardViewModel> Cards
        {
            get { return _Cards; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Assignment of card guess whether it is the first or second attempt
        /// </summary>
        /// <param name="x">x coordinate of the card clicked</param>
        /// <param name="y">y coordinate of the card clicked</param>
        public void setFirstOrSecondGuess(int x, int y)
        {
            foreach (Card card in GameBoard.Cards)
            {
                if (card.myXCoordinate == x && card.myYCoordinate == y)
                {
                    if (card.firstGuess == true) return;
                    if (TurnCount == 1)
                    {
                        TurnCount = 0;
                        card.firstGuess = true;
                        break;
                    }
                    else
                    {
                        TurnCount = 1;
                        card.secondGuess = true; break;
                    }
                }
            }
        }

        public void resetGuesses()
        {
            foreach (Card card in GameBoard.Cards)
            {
                card.firstGuess = false;
                card.secondGuess = false;
            }
        }

        /// <summary>
        /// Gives the first clicked card
        /// </summary>
        public KeyValuePair<int, int> giveFirstGuess()
        {
            Card c = (from f in GameBoard.Cards where f.firstGuess == true select f).FirstOrDefault();
            if (c == null) return new KeyValuePair<int, int>(-1, -1);
            return new KeyValuePair<int, int>(c.myXCoordinate, c.myYCoordinate);
        }

        /// <summary>
        /// Gives the second clicked card
        /// </summary>
        public KeyValuePair<int, int> giveSecondGuess()
        {
            Card c = (from j in GameBoard.Cards where j.secondGuess == true select j).FirstOrDefault();
            if (c == null) return new KeyValuePair<int, int>(-1, -1);
            return new KeyValuePair<int, int>(c.myXCoordinate, c.myYCoordinate);
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

    }

    public class CardViewModel
    {
        public CardViewModel(string name, string imageFileName)
        {
            this.Name = name;
            this.ImageUri = new Uri("http://localhost:1309/images/" + imageFileName);
            this.myXCoordinate = 0;
            this.myYCoordinate = 0;
        }

        //public ContentControl3D CommandTarget { get; set; }
        public Uri ImageUri { get; private set; }
        public string Name { get; private set; }
        public int myXCoordinate { get; set; }
        public int myYCoordinate { get; set; }
        /*
                internal void VerifyCorrectSideIsInView(bool showDetailedView)
                {
                    if (this.CommandTarget == null)
                        return;

                    RotationDestination destination =
                        showDetailedView ?
                        RotationDestination.BackSide :
                        RotationDestination.FrontSide;

                    if (ContentControl3D.RotateCommand.CanExecute(destination, this.CommandTarget))
                        ContentControl3D.RotateCommand.Execute(destination, this.CommandTarget);
                }
         */
    }
}
