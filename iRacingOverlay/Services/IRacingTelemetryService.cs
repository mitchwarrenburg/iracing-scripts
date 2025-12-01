using System;
using System.Collections.Generic;
using System.Linq;
using iRacingOverlay.Models;
using iRacingSdkWrapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace iRacingOverlay.Services;

public class IRacingTelemetryService
{
    private readonly ILogger<IRacingTelemetryService> _logger;
    private readonly AppSettings _settings;
    private readonly SdkWrapper _sdk;
    private readonly Dictionary<int, DriverData> _drivers = new();
    private readonly Dictionary<int, int> _lastLapCheck = new();
    private int? _playerCarIdx;
    private double _sessionTime;
    private double _trackLength;

    public bool IsConnected { get; private set; }
    public event EventHandler? ConnectionStateChanged;
    public event EventHandler<RaceDataUpdatedEventArgs>? RaceDataUpdated;

    public IRacingTelemetryService(
        ILogger<IRacingTelemetryService> logger,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _sdk = new SdkWrapper();
        _sdk.Start();
    }

    public bool Connect()
    {
        try
        {
            if (_sdk.IsConnected)
            {
                IsConnected = true;
                _logger.LogInformation("Connected to iRacing");
                ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to iRacing");
            return false;
        }
    }

    public void Disconnect()
    {
        IsConnected = false;
        _drivers.Clear();
        _lastLapCheck.Clear();
        _sdk.Stop();
        ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        _logger.LogInformation("Disconnected from iRacing");
    }

    public bool Update()
    {
        try
        {
            if (!_sdk.IsConnected)
            {
                if (IsConnected)
                {
                    Disconnect();
                }
                return false;
            }

            if (!IsConnected)
            {
                Connect();
            }

            var telemetry = _sdk.GetTelemetryValue<object>("SessionTime");
            if (telemetry == null)
                return false;

            UpdateDrivers();

            // Raise event with updated data
            if (IsInRaceSession())
            {
                var player = GetPlayerDriver();
                if (player != null)
                {
                    var (ahead, behind) = GetClassOpponents(
                        player,
                        _settings.NumOpponentsAhead,
                        _settings.NumOpponentsBehind);

                    RaceDataUpdated?.Invoke(this, new RaceDataUpdatedEventArgs
                    {
                        PlayerDriver = player,
                        OpponentsAhead = ahead,
                        OpponentsBehind = behind,
                        IsRaceActive = true
                    });
                }
            }
            else
            {
                RaceDataUpdated?.Invoke(this, new RaceDataUpdatedEventArgs
                {
                    IsRaceActive = false
                });
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating telemetry");
            return false;
        }
    }

    private void UpdateDrivers()
    {
        var sessionData = _sdk.GetSessionInfo();
        if (sessionData == null) return;

        _playerCarIdx = sessionData["DriverInfo"]["DriverCarIdx"].GetValue(0);
        _sessionTime = _sdk.GetTelemetryValue<double>("SessionTime").GetValueOrDefault();

        var drivers = sessionData["DriverInfo"]["Drivers"];
        if (drivers == null) return;

        var carIdxPosition = _sdk.GetTelemetryValue<int[]>("CarIdxPosition");
        var carIdxClassPosition = _sdk.GetTelemetryValue<int[]>("CarIdxClassPosition");
        var carIdxLap = _sdk.GetTelemetryValue<int[]>("CarIdxLap");
        var carIdxLastLapTime = _sdk.GetTelemetryValue<float[]>("CarIdxLastLapTime");
        var carIdxLapDistPct = _sdk.GetTelemetryValue<float[]>("CarIdxLapDistPct");

        if (carIdxPosition == null) return;

        for (int carIdx = 0; carIdx < carIdxPosition.Length && carIdx < 64; carIdx++)
        {
            if (carIdxPosition[carIdx] <= 0)
                continue;

            if (!_drivers.ContainsKey(carIdx))
            {
                var driver = drivers.FirstOrDefault(d => d["CarIdx"].GetValue(0) == carIdx);
                if (driver == null) continue;

                _drivers[carIdx] = new DriverData
                {
                    CarIdx = carIdx,
                    Name = driver["UserName"].GetValue("Unknown"),
                    CarClassId = driver["CarClassID"].GetValue(0),
                    IsPlayer = carIdx == _playerCarIdx
                };
            }

            var driverData = _drivers[carIdx];

            // Update position in class
            if (carIdxClassPosition != null && carIdx < carIdxClassPosition.Length)
                driverData.PositionInClass = carIdxClassPosition[carIdx];

            // Update current lap
            int currentLap = 0;
            if (carIdxLap != null && carIdx < carIdxLap.Length)
                currentLap = carIdxLap[carIdx];

            // Check for completed lap
            if (_lastLapCheck.ContainsKey(carIdx))
            {
                if (currentLap > _lastLapCheck[carIdx])
                {
                    if (carIdxLastLapTime != null && carIdx < carIdxLastLapTime.Length)
                    {
                        var lapTime = carIdxLastLapTime[carIdx];
                        if (lapTime > 0)
                        {
                            driverData.AddLap(currentLap - 1, lapTime, _sessionTime);
                        }
                    }
                }
            }

            _lastLapCheck[carIdx] = currentLap;
            driverData.CurrentLap = currentLap;

            // Update track position
            if (carIdxLapDistPct != null && carIdx < carIdxLapDistPct.Length)
                driverData.CurrentDistance = carIdxLapDistPct[carIdx];
        }
    }

    public DriverData? GetPlayerDriver()
    {
        if (_playerCarIdx.HasValue && _drivers.ContainsKey(_playerCarIdx.Value))
        {
            return _drivers[_playerCarIdx.Value];
        }
        return null;
    }

    public (List<OpponentInfo>, List<OpponentInfo>) GetClassOpponents(
        DriverData player,
        int numAhead,
        int numBehind)
    {
        var sameClass = _drivers.Values
            .Where(d => d.CarClassId == player.CarClassId && d.CarIdx != player.CarIdx)
            .OrderBy(d => d.PositionInClass)
            .ToList();

        var ahead = sameClass
            .Where(d => d.PositionInClass < player.PositionInClass)
            .TakeLast(numAhead)
            .Select(d => CalculateCatchInfo(player, d, isAhead: true))
            .ToList();

        var behind = sameClass
            .Where(d => d.PositionInClass > player.PositionInClass)
            .Take(numBehind)
            .Select(d => CalculateCatchInfo(player, d, isAhead: false))
            .ToList();

        return (ahead, behind);
    }

    private OpponentInfo CalculateCatchInfo(DriverData player, DriverData opponent, bool isAhead)
    {
        var oppInfo = new OpponentInfo
        {
            DriverData = opponent,
            IsAhead = isAhead
        };

        var playerPace = player.GetWeightedAveragePace(
            _settings.LapsToConsider,
            _settings.WeightDecayFactor);
        var oppPace = opponent.GetWeightedAveragePace(
            _settings.LapsToConsider,
            _settings.WeightDecayFactor);

        if (!playerPace.HasValue || !oppPace.HasValue)
            return oppInfo;

        // Calculate pace advantage (positive = player is faster)
        var paceDiff = oppPace.Value - playerPace.Value;
        oppInfo.PaceAdvantage = paceDiff;

        // Calculate distance delta
        var distanceDelta = CalculateDistanceDelta(player, opponent, isAhead);
        oppInfo.DistanceDelta = distanceDelta;

        // Calculate time delta
        var avgLapTime = (playerPace.Value + oppPace.Value) / 2.0;
        var timeDelta = distanceDelta * avgLapTime;
        oppInfo.TimeDelta = timeDelta;

        // Calculate laps to catch
        if (Math.Abs(paceDiff) < 0.001)
        {
            oppInfo.LapsToCatch = null;
        }
        else if (isAhead)
        {
            if (paceDiff > 0)
            {
                var lapsToCatch = timeDelta / paceDiff;
                oppInfo.LapsToCatch = lapsToCatch > 0 ? lapsToCatch : null;
            }
        }
        else
        {
            if (paceDiff < 0)
            {
                var lapsToCatch = timeDelta / Math.Abs(paceDiff);
                oppInfo.LapsToCatch = lapsToCatch > 0 ? lapsToCatch : null;
            }
        }

        return oppInfo;
    }

    private double CalculateDistanceDelta(DriverData player, DriverData opponent, bool isAhead)
    {
        var lapDiff = opponent.CurrentLap - player.CurrentLap;
        var posDiff = opponent.CurrentDistance - player.CurrentDistance;
        var totalDistance = lapDiff + posDiff;

        if (isAhead)
        {
            if (totalDistance < -0.5)
                totalDistance += 1.0;
        }
        else
        {
            if (totalDistance > 0.5)
                totalDistance -= 1.0;
        }

        return Math.Abs(totalDistance);
    }

    private bool IsInRaceSession()
    {
        try
        {
            var sessionData = _sdk.GetSessionInfo();
            if (sessionData == null) return false;

            var sessionNum = _sdk.GetTelemetryValue<int>("SessionNum").GetValueOrDefault();
            var sessions = sessionData["SessionInfo"]["Sessions"];

            if (sessions != null && sessionNum >= 0 && sessionNum < sessions.Count())
            {
                var sessionType = sessions.ElementAt(sessionNum)["SessionType"].GetValue("");
                if (!sessionType.Contains("Race"))
                    return false;
            }

            var carCount = _drivers.Count(d => d.Value.PositionInClass > 0);
            return carCount > 1;
        }
        catch
        {
            return false;
        }
    }

}

public class RaceDataUpdatedEventArgs : EventArgs
{
    public DriverData? PlayerDriver { get; set; }
    public List<OpponentInfo> OpponentsAhead { get; set; } = new();
    public List<OpponentInfo> OpponentsBehind { get; set; } = new();
    public bool IsRaceActive { get; set; }
}

