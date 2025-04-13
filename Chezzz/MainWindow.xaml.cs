using Chezzz.Properties;
using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chezzz;

public partial class MainWindow
{
    private const int POSITIVE_MATE = 10000;
    private const int NEGATIVE_MATE = -10000;

    private readonly IProgress<string> _status;
    private readonly string? _stockfishPath;
    private bool _isWhite;
    private string _chessBoardTag;

    private readonly SortedList<int, Move> _moves = [];
    private int _selectedIndex;

    private string _svg;
    private string _style;
    private string _opponentArrow;

    private const string OPACITY = "0.7";

    private readonly IntSetting _requiredTime;
    private char _strategy = 'w';

    private readonly SortedDictionary<string, string> _openings = [];

    private const string ARROW_PREFIX = "chezzz";

    public MainWindow()
    {
        InitializeComponent();

        _chessBoardTag = "wc-chess-board";

        _requiredTime = new IntSetting(
            nameof(Settings.Default.RequiredTime), 
            [
                250, 500, 750, 1000, 1500, 2000, 2500, 3000, 4000, 5000, 6000, 7000, 8000, 9000
            ]);

        _strategy = Settings.Default.Strategy[0];
        if (_strategy != 'w' && _strategy != 'd' && _strategy != 'l') {
            _strategy = 'w';
        }

        _status = new Progress<string>(message => Status.Text = message);
        _stockfishPath = ConfigurationManager.AppSettings["StockFishPath"];
        if (_stockfishPath is null) {
            _status.Report("StockFishPath not found");
        }
        else if (!File.Exists(_stockfishPath)) {
            _status.Report($"{_stockfishPath} not found");
        }

        _svg = string.Empty;
        _style = string.Empty;
        _opponentArrow = string.Empty;
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

                _chessBoardTag = selectedPlatform switch {
                    AppConsts.CHESS => "wc-chess-board",
                    AppConsts.LICHESS => "cg-board",
                    _ => _chessBoardTag
                };
            }
        }
    }

    private void Platform_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        GotoPlatform();
    }

    private void Advice_OnClick(object sender, RoutedEventArgs e)
    {
        GoAdvice();
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        await WindowLoadedAsync();
    }

    private void DecreaseTime_OnClick(object sender, RoutedEventArgs e)
    {
        ChangeRequiredTime(-1);
    }

    private void IncreaseTime_OnClick(object sender, RoutedEventArgs e)
    {
        ChangeRequiredTime(+1);
    }

    private void Strategy_OnClick(object sender, RoutedEventArgs e)
    {
        ChangeStrategy();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1) {
            GoAdvice();
        }
    }
}