using System.Configuration;
using System.Windows;

namespace Chezzz
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            _stockfishPath = ConfigurationManager.AppSettings["StockFishPath"];
            _elo = ConfigurationManager.AppSettings["Elo"];
            _timeout = ConfigurationManager.AppSettings["Timeout"];
            _threads = ConfigurationManager.AppSettings["Threads"];
        }

        private async void Advice_OnClick(object sender, RoutedEventArgs e)
        {
            await AdviceAsync();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            WindowLoaded();
        }
    }
}