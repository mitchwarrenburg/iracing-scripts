using System.Collections.Generic;
using System.Linq;

namespace iRacingOverlay.Models;

public class DriverData
{
    private readonly Queue<LapData> _lapHistory = new();
    private const int MaxLapHistory = 20;

    public int CarIdx { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CarClassId { get; set; }
    public int PositionInClass { get; set; }
    public int CurrentLap { get; set; }
    public double LastLapTime { get; set; }
    public double CurrentDistance { get; set; }
    public double CurrentSpeed { get; set; }
    public bool IsPlayer { get; set; }

    public IReadOnlyCollection<LapData> LapHistory => _lapHistory;

    public void AddLap(int lapNumber, double lapTime, double timestamp)
    {
        if (lapTime <= 0) return;

        if (_lapHistory.Count >= MaxLapHistory)
        {
            _lapHistory.Dequeue();
        }

        _lapHistory.Enqueue(new LapData(lapNumber, lapTime, timestamp));
        LastLapTime = lapTime;
    }

    public List<LapData> GetRecentLaps(int n)
    {
        return _lapHistory.TakeLast(n).ToList();
    }

    public double? GetWeightedAveragePace(int n, double decayFactor = 0.7)
    {
        var recentLaps = GetRecentLaps(n);

        if (recentLaps.Count == 0)
            return null;

        var weights = new List<double>();
        double totalWeight = 0.0;

        for (int i = 0; i < recentLaps.Count; i++)
        {
            var weight = Math.Pow(decayFactor, recentLaps.Count - 1 - i);
            weights.Add(weight);
            totalWeight += weight;
        }

        if (totalWeight == 0)
            return null;

        double weightedSum = recentLaps
            .Select((lap, index) => lap.LapTime * weights[index])
            .Sum();

        return weightedSum / totalWeight;
    }
}

