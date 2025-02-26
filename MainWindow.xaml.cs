﻿using Chezzz.Properties;
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

    private readonly IProgress<string>? _status;
    private readonly string? _stockfishPath;
    private bool isWhite;
    private string ChessBoardTag;

    private readonly SortedList<int, Move> _moves = new();
    private readonly SortedSet<int> _selectedMoves = new();
    private int _selectedIndex;

    private readonly IntSetting _requiredTime;
    private readonly IntSetting _requiredScore;

    private readonly SortedDictionary<string, string> _openings = new();

    public MainWindow()
    {
        InitializeComponent();

        ChessBoardTag = "wc-chess-board";

        _requiredTime = new IntSetting(
            nameof(Settings.Default.RequiredTime), 
            new[] {
                250, 500, 750, 1000, 1500, 2000, 2500, 3000, 4000, 5000, 6000, 7000, 8000, 9000
            });

        _requiredScore = new IntSetting(
            nameof(Settings.Default.RequiredScore),
            new[] {
                NEGATIVE_MATE,
                -1000, -500, -450, -400, -350, -300, -250, -200, -150, -100, -75, -50, -25,
                0,
                25, 50, 75, 100, 150, 200, 250, 300, 350, 400, 450, 500, 1000,
                POSITIVE_MATE
            });

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

                ChessBoardTag = selectedPlatform switch {
                    AppConsts.CHESS => "wc-chess-board",
                    AppConsts.LICHESS => "cg-container",
                    _ => ChessBoardTag
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