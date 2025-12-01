using System;
using System.Collections.Generic;
using System.Linq;
using iRacingOverlay.Models;
using iRacingSDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace iRacingOverlay.Services;

public class IRacingTelemetryService : IDisposable
{
    private readonly ILogger<IRacingTelemetryService> _logger;
    private readonly AppSettings _settings;
    private readonly iRacingConnection _connection;
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
        _connection = new iRacingConnection();
    }

    public bool Connect()
    {
        try
        {
            if (_connection.IsConnected())
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
        ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        _logger.LogInformation("Disconnected from iRacing");
    }

    public bool Update()
    {
        try
        {
            if (!_connection.IsConnected())
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

            var data = _connection.GetDataFeed();
            if (data == null)
                return false;

            _sessionTime = data.SessionData.SessionInfo.Sessions[data.SessionData.SessionNum].SessionTime;
            _trackLength = data.SessionData.WeekendInfo.TrackLength;

            UpdateDrivers(data);

            // Raise event with updated data
            if (IsInRaceSession(data))
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

    private void UpdateDrivers(DataSample data)
    {
        var driverInfo = data.SessionData.DriverInfo;
        _playerCarIdx = driverInfo.DriverCarIdx;

        foreach (var driver in driverInfo.CompetingDrivers)
        {
            var carIdx = driver.CarIdx;

            // Skip if not on track
            if (data.Telemetry.CarIdxPosition[carIdx] <= 0)
                continue;

            if (!_drivers.ContainsKey(carIdx))
            {
                _drivers[carIdx] = new DriverData
                {
                    CarIdx = carIdx,
                    Name = driver.UserName ?? "Unknown",
                    CarClassId = driver.CarClassID,
                    IsPlayer = carIdx == _playerCarIdx
                };
            }

            var driverData = _drivers[carIdx];

            // Update position in class
            driverData.PositionInClass = data.Telemetry.CarIdxClassPosition[carIdx];

            // Update current lap
            var currentLap = data.Telemetry.CarIdxLap[carIdx];

            // Check for completed lap
            if (_lastLapCheck.ContainsKey(carIdx))
            {
                if (currentLap > _lastLapCheck[carIdx])
                {
                    var lapTime = data.Telemetry.CarIdxLastLapTime[carIdx];
                    if (lapTime > 0)
                    {
                        driverData.AddLap(currentLap - 1, lapTime, _sessionTime);
                    }
                }
            }

            _lastLapCheck[carIdx] = currentLap;
            driverData.CurrentLap = currentLap;

            // Update track position
            driverData.CurrentDistance = data.Telemetry.CarIdxLapDistPct[carIdx];
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

    private bool IsInRaceSession(DataSample data)
    {
        try
        {
            var currentSession = data.SessionData.SessionInfo.Sessions[data.SessionData.SessionNum];
            if (!currentSession.SessionType.Contains("Race"))
                return false;

            var carCount = _drivers.Count(d => d.Value.PositionInClass > 0);
            return carCount > 1;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        Disconnect();
        _connection?.Dispose();
    }
}

public class RaceDataUpdatedEventArgs : EventArgs
{
    public DriverData? PlayerDriver { get; set; }
    public List<OpponentInfo> OpponentsAhead { get; set; } = new();
    public List<OpponentInfo> OpponentsBehind { get; set; } = new();
    public bool IsRaceActive { get; set; }
}

