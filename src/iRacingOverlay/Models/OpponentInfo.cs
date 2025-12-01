namespace iRacingOverlay.Models;

public class OpponentInfo
{
    public DriverData DriverData { get; set; } = null!;
    public double? LapsToCatch { get; set; }
    public bool IsAhead { get; set; }
    public double PaceAdvantage { get; set; }
    public double TimeDelta { get; set; }
    public double DistanceDelta { get; set; }
}

