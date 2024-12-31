using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace Chezzz
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            _status = new Progress<string>(message => Status.Text = message);
            _scoreBorder = new[] { ScoreBorder1, ScoreBorder2, ScoreBorder3 };
            _wdlBorder = new[] { WdlBorder1, WdlBorder2, WdlBorder3 };
            _bestMoveBorder = new[] { BestMoveBorder1, BestMoveBorder2, BestMoveBorder3 };
            _moveBorder = new[] { MoveBorder1, MoveBorder2, MoveBorder3 };
            _scoreText = new[] { ScoreText1, ScoreText2, ScoreText3 };
            _wdlText = new[] { WdlText1, WdlText2, WdlText3 };
            _bestMoveText = new[] { BestMoveText1, BestMoveText2, BestMoveText3 };
            _moveText = new[] { MoveText1, MoveText2, MoveText3 };
        }

        private void GotoPlatform()
        {
            if (Platform.SelectedItem is ComboBoxItem selectedItem) {
                var selectedPlatform = selectedItem.Content.ToString();
                if (!string.IsNullOrEmpty(selectedPlatform)) {
                    var url = $"https://{selectedPlatform}/";
                    if (WebBrowser != null) {
                        WebBrowser.Source = new Uri(url);
                    }
                }
            }
        }

        private void Platform_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GotoPlatform();
        }

        private async void Advice_OnClick(object sender, RoutedEventArgs e)
        {
            _stockfishPath = ConfigurationManager.AppSettings["StockFishPath"];
            _elo = ConfigurationManager.AppSettings["Elo"];
            _depth = ConfigurationManager.AppSettings["Depth"];
            _threads = ConfigurationManager.AppSettings["Threads"];

            Advice.IsEnabled = false;
            await AdviceAsync();
            Advice.IsEnabled = true;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            WindowLoaded();
        }
    }
}