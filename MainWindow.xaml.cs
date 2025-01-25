using System.Configuration;
using System.IO;
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
            _stockfishPath = ConfigurationManager.AppSettings["StockFishPath"];
            if (!File.Exists(_stockfishPath)) {
                _status?.Report($"{_stockfishPath} not found");
            }
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
            if (!File.Exists(_stockfishPath)) {
                _status?.Report($"{_stockfishPath} not found");
            }

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