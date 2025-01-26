using Chezzz.Properties;
using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chezzz
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            _status = new Progress<string>(message => Status.Text = message);
            _stockfishPath = ConfigurationManager.AppSettings["StockFishPath"];
            if (!File.Exists(_stockfishPath)) {
                _status?.Report($"{_stockfishPath} not found");
            }

            _requiredScoreValue = Settings.Default.RequiredScoreValue;
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
            GoAdvice();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            WindowLoaded();
        }

        private void DecreaseScore_OnClick(object sender, RoutedEventArgs e)
        {
            ChangeRequiredScore(-1);
        }

        private void IncreaseScore_OnClick(object sender, RoutedEventArgs e)
        {
            ChangeRequiredScore(+1);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1) {
                GoAdvice();
            }
        }
    }
}