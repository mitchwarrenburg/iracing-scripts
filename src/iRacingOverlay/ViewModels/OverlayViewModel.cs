using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using iRacingOverlay.Models;
using Microsoft.Extensions.Options;

namespace iRacingOverlay.ViewModels;

public partial class OverlayViewModel : ObservableObject
{
    private readonly AppSettings _appSettings;
    private readonly OverlaySettings _overlaySettings;

    [ObservableProperty]
    private string _statusText = "Waiting for iRacing...";

    [ObservableProperty]
    private string _playerText = "Waiting for race...";

    [ObservableProperty]
    private ObservableCollection<OpponentViewModel> _opponentsAhead = new();

    [ObservableProperty]
    private ObservableCollection<OpponentViewModel> _opponentsBehind = new();

    public OverlaySettings OverlaySettings => _overlaySettings;
    public Brush BackgroundBrush { get; }
    public Brush TextBrush { get; }
    public Brush StatusBrush { get; private set; }

    private readonly Brush _fasterBrush;
    private readonly Brush _slowerBrush;
    private readonly Brush _neutralBrush;

    public OverlayViewModel(
        IOptions<AppSettings> appSettings,
        IOptions<OverlaySettings> overlaySettings)
    {
        _appSettings = appSettings.Value;
        _overlaySettings = overlaySettings.Value;

        BackgroundBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(_overlaySettings.BackgroundColor)!;
        TextBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(_overlaySettings.TextColor)!;
        _fasterBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(_overlaySettings.ColorFaster)!;
        _slowerBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(_overlaySettings.ColorSlower)!;
        _neutralBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(_overlaySettings.ColorNeutral)!;
        StatusBrush = _neutralBrush;
    }

    public void UpdateRaceData(
        DriverData? player,
        System.Collections.Generic.List<OpponentInfo> opponentsAhead,
        System.Collections.Generic.List<OpponentInfo> opponentsBehind,
        bool isRaceActive)
    {
        if (!isRaceActive)
        {
            StatusText = "Not in race session";
            StatusBrush = _neutralBrush;
            PlayerText = "Waiting for race...";
            OpponentsAhead.Clear();
            OpponentsBehind.Clear();
            return;
        }

        if (player == null)
        {
            StatusText = "No player data";
            StatusBrush = _slowerBrush;
            OpponentsAhead.Clear();
            OpponentsBehind.Clear();
            return;
        }

        StatusText = "Race Active";
        StatusBrush = _fasterBrush;

        // Update player text
        var playerPace = player.GetWeightedAveragePace(_appSettings.LapsToConsider, _appSettings.WeightDecayFactor);
        PlayerText = playerPace.HasValue
            ? $"YOU - P{player.PositionInClass} | Pace: {playerPace.Value:F2}s"
            : $"YOU - P{player.PositionInClass}";

        // Update opponents ahead
        OpponentsAhead.Clear();
        foreach (var opp in opponentsAhead)
        {
            OpponentsAhead.Add(CreateOpponentViewModel(opp, isAhead: true));
        }

        // Update opponents behind
        OpponentsBehind.Clear();
        foreach (var opp in opponentsBehind)
        {
            OpponentsBehind.Add(CreateOpponentViewModel(opp, isAhead: false));
        }
    }

    private OpponentViewModel CreateOpponentViewModel(OpponentInfo oppInfo, bool isAhead)
    {
        var opp = oppInfo.DriverData;
        var nameText = $"P{opp.PositionInClass} - {opp.Name}";

        string catchText;
        if (oppInfo.LapsToCatch.HasValue && oppInfo.LapsToCatch.Value > 0)
        {
            var lapsStr = oppInfo.LapsToCatch.Value.ToString($"F{_appSettings.LapsPrecision}");
            catchText = isAhead ? $"Catch in: {lapsStr} laps" : $"Catches in: {lapsStr} laps";
        }
        else
        {
            catchText = isAhead
                ? (oppInfo.PaceAdvantage <= 0 ? "Won't catch" : "N/A")
                : (oppInfo.PaceAdvantage >= 0 ? "Won't catch" : "N/A");
        }

        var paceAdvStr = $"{Math.Abs(oppInfo.PaceAdvantage):F3}s/lap";
        var paceText = oppInfo.PaceAdvantage > 0 ? $"+{paceAdvStr}" :
                       oppInfo.PaceAdvantage < 0 ? $"-{paceAdvStr}" : "±0.000s/lap";
        var timeGapStr = $"Gap: {oppInfo.TimeDelta:F2}s";
        var detailsText = $"{paceText} | {timeGapStr}";

        // Determine color
        Brush colorBrush;
        if (Math.Abs(oppInfo.PaceAdvantage) < 0.001)
        {
            colorBrush = _neutralBrush;
        }
        else
        {
            // For both ahead and behind: green if player is faster (positive advantage)
            colorBrush = oppInfo.PaceAdvantage > 0 ? _fasterBrush : _slowerBrush;
        }

        return new OpponentViewModel
        {
            NameText = nameText,
            CatchText = catchText,
            DetailsText = detailsText,
            ColorBrush = colorBrush,
            FontSizeDriver = _overlaySettings.FontSizeDriver,
            FontSizeSmall = _overlaySettings.FontSizeSmall
        };
    }
}

public class OpponentViewModel
{
    public string NameText { get; set; } = string.Empty;
    public string CatchText { get; set; } = string.Empty;
    public string DetailsText { get; set; } = string.Empty;
    public Brush ColorBrush { get; set; } = Brushes.White;
    public int FontSizeDriver { get; set; }
    public int FontSizeSmall { get; set; }
}

