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

    private readonly Models.Moves _moves = new();
    private readonly Models.Moves _opponentMoves = new();

    private string _svg;
    private string _style;
    private string _opponentArrow;

    private Models.Score? _currentScore = null;

    private readonly IntSetting _requiredTime;
    private readonly IntSetting _requiredScore;

    private readonly SortedDictionary<string, string> _openings = [];

    public MainWindow()
    {
        InitializeComponent();

        _chessBoardTag = "wc-chess-board";

        _requiredTime = new IntSetting(
            nameof(Settings.Default.RequiredTime), 
            [
                250, 500, 750, 1000, 1500, 2000, 2500, 3000, 4000, 5000, 6000, 7000, 8000, 9000
            ]);

        _requiredScore = new IntSetting(
            nameof(Settings.Default.RequiredScore),
            [
                NEGATIVE_MATE,
                -750, -500, -450, -400, -350, -300, -250, -200, -150, -100, -75, -50, -25,
                0,
                25, 50, 75, 100, 150, 200, 250, 300, 350, 400, 450, 500, 750,
                POSITIVE_MATE
            ]);

        _status = new Progress<string>(message => Status.Text = message);
        _stockfishPath = ConfigurationManager.AppSettings["StockFishPath"];
        if (_stockfishPath is null) {
            _status.Report("StockFishPath not found");
        }
        else if (!File.Exists(_stockfishPath)) {
            _status.Report($"{_stockfishPath} not found");
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362)) {
            _status.Report("WebBrowser is only supported on Windows 10.0.18362 and later");
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

    private void DecreaseScore_OnClick(object sender, RoutedEventArgs e)
    {
        ChangeRequiredScore(-1);
    }

    private void IncreaseScore_OnClick(object sender, RoutedEventArgs e)
    {
        ChangeRequiredScore(+1);
    }

    private void DecreaseTime_OnClick(object sender, RoutedEventArgs e)
    {
        ChangeRequiredTime(-1);
    }

    private void IncreaseTime_OnClick(object sender, RoutedEventArgs e)
    {
        ChangeRequiredTime(+1);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1) {
            GoAdvice();
        }
    }
}