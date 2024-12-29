using System.ComponentModel;
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
            _depth = ConfigurationManager.AppSettings["Depth"];
            _threads = ConfigurationManager.AppSettings["Threads"];
            _cancellationTokenSource = new CancellationTokenSource();
        } 

        private void Advice_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_isRunning) {
                StartBackgroundProcess();
            }
            else {
                StopBackgroundProcess();
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            WindowLoaded();
        }

        private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            StopBackgroundProcess();
        }
    }
}