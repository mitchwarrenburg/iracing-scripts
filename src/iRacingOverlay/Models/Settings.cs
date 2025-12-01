namespace iRacingOverlay.Models;

public class AppSettings
{
    public int LapsToConsider { get; set; } = 5;
    public double WeightDecayFactor { get; set; } = 0.7;
    public int NumOpponentsAhead { get; set; } = 3;
    public int NumOpponentsBehind { get; set; } = 3;
    public int UpdateIntervalMs { get; set; } = 100;
    public int LapsPrecision { get; set; } = 2;
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool AutoStartOnRace { get; set; } = true;
}

public class OverlaySettings
{
    public int Width { get; set; } = 400;
    public int Height { get; set; } = 350;
    public double Opacity { get; set; } = 0.9;
    public string BackgroundColor { get; set; } = "#1a1a1a";
    public string TextColor { get; set; } = "#ffffff";
    public string ColorFaster { get; set; } = "#00ff00";
    public string ColorSlower { get; set; } = "#ff0000";
    public string ColorNeutral { get; set; } = "#ffffff";
    public string FontFamily { get; set; } = "Segoe UI";
    public int FontSizeHeader { get; set; } = 14;
    public int FontSizeDriver { get; set; } = 11;
    public int FontSizeSmall { get; set; } = 9;
    public bool TopMost { get; set; } = true;
}

