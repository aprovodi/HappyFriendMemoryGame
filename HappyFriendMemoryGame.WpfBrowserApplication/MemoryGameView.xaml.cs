using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.IO.IsolatedStorage;
using System.Collections;
using HappyFriendMemoryGame.WcfService;
using HappyFriendMemoryGame.ControlContent3D;
using HappyFriendMemoryGame.WpfBrowserApplication.ServiceReference1;

namespace HappyFriendMemoryGame.WpfBrowserApplication
{     
    /// <summary>
    /// Interaction logic for MemoryGameView.xaml
    /// </summary>
    public partial class MemoryGameView : Page
    {
        #region Instance Fields

        Random _rnd;
        int numBonusTimes, overallGameScore, hit;
        DateTime startGameTime, finishGameTime, c, a;
        HorizontalAlignment _horAlign;
        VerticalAlignment _vertAlign;
        double _difficulty;
        bool PairChosen = false;
        CardCollectionViewModel ccvm;

        #endregion

        #region Constructor

        public MemoryGameView()
        {
            InitializeComponent();
            ccvm = new CardCollectionViewModel();
            base.DataContext = ccvm;
            this.WelcomeScreenControl.StartGameButtonClick += new RoutedEventHandler(StartGameButtonClick);
            _rnd = new Random();
            _horAlign = HorizontalAlignment.Center;
            _vertAlign = VerticalAlignment.Center;
            scoreTextBlock.Text = string.Format("Score: {0}", overallGameScore);
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Initialize the GUI components
        /// </summary>
        private void initialiseGUI(object sender, RoutedEventArgs e)
        {
            this.WelcomeScreenControl.Visibility = Visibility.Visible;
            this.FieldGrid.Visibility = Visibility.Hidden;
            this.StatBorder.Visibility = Visibility.Hidden;
            //this.VicScreenControl.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Starts new game and initializes certain UI elements.
        /// </summary>
        private void StartGameButtonClick(object sender, RoutedEventArgs e)
        {
            _difficulty = this.WelcomeScreenControl.Difficulty;
            this.WelcomeScreenControl.Visibility = Visibility.Hidden;
            this.FieldGrid.Visibility = Visibility.Visible;
            this.StatBorder.Visibility = Visibility.Visible;
            BuildGrid();
        }

        void ContentControl3D_Loaded(object sender, RoutedEventArgs e)
        {
            // Inject the ContentControl3D into the ViewModel object, so that it
            // knows which control the RotateCommand should target.
            ContentControl3D cc3D = sender as ContentControl3D;
            CardViewModel thing = cc3D.DataContext as CardViewModel;
            //if (thing != null)
            //    thing.CommandTarget = cc3D;
        }

        #endregion

        #region Commands

        /// <summary>
        /// NextTurn command, executed when NextTurn Button clicked for flipping last two Cards
        /// </summary>
        public static readonly RoutedCommand NextTurn = new RoutedCommand();
        private void NextTurnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.PairChosen;
            e.Handled = true;
        }

        private void NextTurnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (UIElement ui in FieldGrid.Children)
            {
                if (ui is ContentControl3D)
                {
                    RotationDestination destination = RotationDestination.FrontSide;
                    if (ContentControl3D.RotateCommand.CanExecute(destination, (ContentControl3D)ui))
                        ContentControl3D.RotateCommand.Execute(destination, (ContentControl3D)ui);
                }
            }

            foreach (UIElement ui in FieldGrid.Children)
            {
                ui.IsEnabled = true;
            }

            e.Handled = true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Allocation of all the cards on the grid.
        /// </summary>
        private void BuildGrid()
        {
            numBonusTimes = 0;
            ccvm.MissedMatches = 0;
            overallGameScore = 0;
            hit = 0;

            FieldGrid.Children.Clear();
            FieldGrid.ColumnDefinitions.Clear();
            FieldGrid.RowDefinitions.Clear();
            for (int i = 0; i < ccvm.CurrentLevel; i++)
            {
                FieldGrid.ColumnDefinitions.Add(new ColumnDefinition());
                FieldGrid.RowDefinitions.Add(new RowDefinition());
            }

            foreach (CardViewModel card in this.ccvm.Cards)
            {
                //TODO: Create well-structured DataTemplate for the Card in XAML file
                Border backb = new Border() { Background = Brushes.LightSeaGreen, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Width = 100, Height = 100 };
                Image im = new Image() { Source = new BitmapImage(card.ImageUri) };
                backb.Child = im;
                Border frontb = new Border() { Background = Brushes.LightSeaGreen, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Width = 100, Height = 100 };
                Button bu = new Button() { Style = (Style)FindResource("FrontButton") };
                bu.Click += new RoutedEventHandler(CardClickCheckMatch);
                frontb.Child = bu;
                ContentControl3D cardControl = new ContentControl3D() { Style = (Style)FindResource("PlayerCard"), BackContent = backb, Content = frontb };
                Grid.SetColumn(cardControl, card.myYCoordinate);
                Grid.SetRow(cardControl, card.myXCoordinate);
                Grid.SetColumn(bu, card.myYCoordinate);
                Grid.SetRow(bu, card.myXCoordinate);
                FieldGrid.Children.Add(cardControl);
            }

            startGameTime = DateTime.Now;
            c = DateTime.Now;
        }

        /// <summary>
        /// Handles the click events and initiates calculation policy
        /// when two Cards were flipped.
        /// </summary>
        /// <param name="s">Button clicked</param>
        private void CardClickCheckMatch(object s, RoutedEventArgs e)
        {
            Button b = (Button)s;
            if (s == null) return;

            int x = Grid.GetRow(b);
            int y = Grid.GetColumn(b);

            this.ccvm.setFirstOrSecondGuess(x, y);

            if (this.ccvm.TurnCount == 1)
            {
                UpdateScores(ccvm.GameBoard.MatchCheck());
                this.ccvm.resetGuesses();
            }
        }

        void UpdateScores(bool match)
        {
            KeyValuePair<int, int> p1 = this.ccvm.giveFirstGuess();
            KeyValuePair<int, int> p2 = this.ccvm.giveSecondGuess();

            if (p1.Key == -1 || p2.Key == -1) return;
            int x = 0, y = 0;
            if (!match)
            {
                numBonusTimes = 0;
                ccvm.MissedMatches++;
                ReportScore(match, p2.Key, p2.Value);
                this.PairChosen = true;
                foreach (UIElement ui in FieldGrid.Children)
                {
                    ui.IsEnabled = false;
                }
            }
            else
            {
                a = DateTime.Now;
                numBonusTimes++;
                ReportScore(true, p2.Key, p2.Value);
                this.ccvm.NumCardsLeft--;
                if (ccvm.NumCardsLeft == 0)
                {
                    win();
                }
            }

            foreach (UIElement ui in FieldGrid.Children)
            {
                y = Grid.GetColumn(ui);
                x = Grid.GetRow(ui);
                if ((x == p1.Key && y == p1.Value) || (x == p2.Key && y == p2.Value))
                {
                    if (ui is ContentControl3D)
                    {
                        if (match)
                            ui.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        /// <summary>
        /// Calculation policy
        /// </summary>
        /// <param name="match"></param>
        /// <param name="x">x coordinate of animated score</param>
        /// <param name="y">y coordinate of animated score</param>
        void ReportScore(bool match, int x, int y)
        {
            if (!match)
            {
                hit = (ccvm.CurrentLevel - 1) * -3;
                hit *= (int)((_difficulty) * 0.5);
                overallGameScore += hit;
            }
            else
            {
                TimeSpan ts = a - c;
                hit = (int)((1 / ts.TotalMilliseconds) * 22000);
                hit *= (int)_difficulty;
                if (numBonusTimes > 1)
                {
                    hit += (int)(Math.Pow(10, numBonusTimes - 1));
                }

                overallGameScore += hit;
                c = DateTime.Now;
            }

            scoreTextBlock.Text = string.Format("Score: {0:0,0}", overallGameScore);
            if (hit > 0)
            {
                ScaleTransform st = new ScaleTransform();
                scoreTextBlock.RenderTransform = st;
                scoreTextBlock.BeginAnimation(TextBlock.FontSizeProperty, new DoubleAnimation() { Duration = new Duration(TimeSpan.FromMilliseconds(300)), From = 12, To = 20 });
            }

            TextBlock tb = new TextBlock() { Text = string.Format(numBonusTimes > 1 ? "Bonus! {0}" : "{0}", hit), HorizontalAlignment = _horAlign, VerticalAlignment = _vertAlign, FontSize = 30, FontWeight = FontWeights.ExtraBold };
            Grid.SetColumn(tb, y);
            Grid.SetRow(tb, x);
            FieldGrid.Children.Add(tb);
            Storyboard sb = ss("Opacity", 1.0, 0.0, 2000, tb);
            sb.Begin();
            TranslateTransform tt = new TranslateTransform();
            tb.RenderTransform = tt;
            tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation() { Duration = new Duration(TimeSpan.FromMilliseconds(700)), From = 0.0, To = -40.0 });
        }

        void win()
        {
            finishGameTime = DateTime.Now;
            ccvm.TimeSpent = finishGameTime - startGameTime;

            //this.VicScreenControl.Visibility = Visibility.Visible;

            FieldGrid.Children.Clear();
            FieldGrid.ColumnDefinitions.Clear();
            FieldGrid.RowDefinitions.Clear();
            FieldGrid.RowDefinitions.Add(new RowDefinition());
            FieldGrid.ColumnDefinitions.Add(new ColumnDefinition());
            FieldGrid.ColumnDefinitions[0].MinWidth = 350;
            FieldGrid.RowDefinitions[0].MinHeight = 270;

            // TODO: Move this logic to VictoryScreenControl
            StackPanel s = new StackPanel() { HorizontalAlignment = _horAlign, VerticalAlignment = _vertAlign };
            List<TextBlock> tp = new List<TextBlock>();
            tp.Add(new TextBlock() { Text = "Winner!", FontSize = 30, HorizontalAlignment = _horAlign, Margin = new Thickness(10), Foreground = BackGradient(), FontFamily = new FontFamily("Verdana Bold Italic") });
            tp.Add(new TextBlock() { Text = string.Format("Finished matching in {0} minutes, {1} seconds", ccvm.TimeSpent.Minutes, ccvm.TimeSpent.Seconds), HorizontalAlignment = _horAlign, Margin = new Thickness(10) });
            tp.Add(new TextBlock() { Text = string.Format("Matches missed: {0}", ccvm.MissedMatches), HorizontalAlignment = _horAlign, Margin = new Thickness(5) });
            Button b = new Button() { Content = "Play again!", Margin = new Thickness(10), Background = Brushes.DodgerBlue, Cursor = Cursors.Hand };
            b.Click += new RoutedEventHandler(StartGameButtonClick);
            foreach (var t in tp)
            {
                s.Children.Add(t);
            }

            s.Children.Add(b);

            Border r = new Border() { HorizontalAlignment = _horAlign, VerticalAlignment = _vertAlign, Child = s, Background = Brushes.WhiteSmoke, CornerRadius = new CornerRadius(10), BorderBrush = Brushes.RoyalBlue, BorderThickness = new Thickness(5), Name = "x" };
            FieldGrid.Children.Add(r);
            Storyboard sb = ss("Opacity", 0.0, 1.0, 600, r);
            sb.Begin(r);
        }

        Storyboard ss(string xt, double f, double t, int ms, UIElement u)
        {
            Storyboard s = new Storyboard();
            DoubleAnimation da = new DoubleAnimation() { Duration = new Duration(TimeSpan.FromMilliseconds(ms)), From = f, To = t };
            s.Children.Add(da);
            Storyboard.SetTarget(da, u);
            Storyboard.SetTargetProperty(da, new PropertyPath(xt));
            return s;
        }

        RadialGradientBrush BackGradient()
        {
            RadialGradientBrush rgb
                = new RadialGradientBrush()
                {
                    GradientOrigin = new Point(_rnd.NextDouble(), _rnd.NextDouble()),
                    Center = new Point(_rnd.NextDouble(), _rnd.NextDouble()),
                    RadiusX = _rnd.NextDouble(),
                    RadiusY = _rnd.NextDouble()
                };

            // Beginning of gradient offset
            rgb.GradientStops.Add(
                new GradientStop(new Color()
                {
                    R = (byte)_rnd.Next(255),
                    G = (byte)_rnd.Next(255),
                    B = (byte)_rnd.Next(255),
                    A = 255
                }, 0.0
                    )
                );

            // Scrambled middle gradients
            for (int i = 0; i < _rnd.Next(5); i++)
            {
                rgb.GradientStops.Add(
                    new GradientStop(
                        new Color()
                        {
                            R = (byte)_rnd.Next(255),
                            G = (byte)_rnd.Next(255),
                            B = (byte)_rnd.Next(255),
                            A = 255
                        }
                            , _rnd.NextDouble()
                            )
                        );
            }

            // Ending of gradient offset
            rgb.GradientStops.Add(
                new GradientStop(
                    new Color()
                    {
                        R = (byte)_rnd.Next(255),
                        G = (byte)_rnd.Next(255),
                        B = (byte)_rnd.Next(255),
                        A = 255
                    }, 1.0
                    )
                );

            rgb.Freeze();

            return rgb;
        }

        #endregion
    }
}
