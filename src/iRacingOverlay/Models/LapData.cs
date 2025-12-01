namespace iRacingOverlay.Models;

public class LapData
{
    public int LapNumber { get; set; }
    public double LapTime { get; set; }
    public double Timestamp { get; set; }

    public LapData(int lapNumber, double lapTime, double timestamp)
    {
        LapNumber = lapNumber;
        LapTime = lapTime;
        Timestamp = timestamp;
    }
}

