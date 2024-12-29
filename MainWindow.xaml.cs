using System.Configuration;
using System.Windows;

namespace Chezzz
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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