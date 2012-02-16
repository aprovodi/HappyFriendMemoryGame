using System;
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
using System.ComponentModel;

namespace HappyFriendMemoryGame.WpfBrowserApplication
{
    /// <summary>
    /// Interaction logic for WelcomeScreenControl.xaml
    /// </summary>
    public partial class VictoryScreenControl : UserControl
    {

        #region Routed Events
        /// <summary>
        /// StartGameButtonClickEvent event, occurs when the user clicks the start game button
        /// </summary>
        public static readonly RoutedEvent StartGameButtonClickEvent = EventManager.RegisterRoutedEvent(
            "StartGameButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(WelcomeScreenControl));

        /// <summary>
        /// Expose the StartGameButtonClickEvent to external sources
        /// </summary>
        public event RoutedEventHandler StartGameButtonClick
        {
            add { AddHandler(StartGameButtonClickEvent, value); }
            remove { RemoveHandler(StartGameButtonClickEvent, value); }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Blank constructor
        /// </summary>
        public VictoryScreenControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// The Difficulty property is set to a slider
        /// value and the StartGameButtonClickEvent is raised 
        /// which is used by the parent <see cref="MemoryGameView">window</see>
        /// </summary>
        /// <param name="sender">StartGameButton</param>
        /// <param name="e">The event args</param>
        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(StartGameButtonClickEvent));
        }
        #endregion

    }
}