# iRacing Overlay - Project Overview

A professional Windows application for iRacing that provides real-time race position overlay with intelligent catch predictions.

## Quick Start

### For Users (Download & Install)
1. Download `iRacingOverlay-Setup.msi` from [Releases](../../releases)
2. Run the installer
3. Launch from Start Menu or system tray
4. Application auto-shows when you start racing

### For Developers (Build from Source)
```powershell
# Build
.\build.ps1

# Or manually
dotnet restore
dotnet build
dotnet run --project src/iRacingOverlay
```

## Project Structure

```
iracing-scripts/
├── src/
│   └── iRacingOverlay/
│       ├── Models/              # Data structures
│       │   ├── LapData.cs
│       │   ├── DriverData.cs
│       │   ├── OpponentInfo.cs
│       │   └── Settings.cs
│       ├── Services/            # Business logic
│       │   ├── IRacingTelemetryService.cs
│       │   └── StartupService.cs
│       ├── Views/               # WPF UI
│       │   ├── OverlayWindow.xaml
│       │   └── OverlayWindow.xaml.cs
│       ├── ViewModels/          # MVVM
│       │   └── OverlayViewModel.cs
│       ├── Resources/           # Icons, images
│       ├── App.xaml             # Application entry
│       ├── App.xaml.cs
│       ├── appsettings.json     # Configuration
│       └── iRacingOverlay.csproj
├── .github/
│   └── workflows/
│       └── dotnet-build.yml     # CI/CD pipeline
├── build.ps1                    # Build script
├── iRacingOverlay.sln           # Visual Studio solution
├── README.md                    # Main documentation
├── TECHNICAL_OVERVIEW.md        # Architecture details
├── QUICK_REFERENCE.md           # Quick reference
└── LICENSE                      # License

```

## Features

### Core Functionality
- Real-time position tracking (3 ahead, 3 behind)
- Weighted lap time calculations with exponential decay
- Intelligent catch time predictions (2 decimal precision)
- Real-time distance and speed integration
- Color-coded display (green = faster, red = slower)
- Class-based filtering
- Automatic race detection

### Windows Integration
- System tray icon with context menu
- Auto-start with Windows (optional)
- Background operation
- Minimize to tray
- Auto-show when race starts
- Always-on-top overlay

### Distribution
- MSI installer with WiX
- GitHub Actions CI/CD
- Automated builds and releases
- Professional installation experience

## Technology Stack

- **.NET 8.0** - Modern .NET framework
- **WPF** - Windows Presentation Foundation for UI
- **MVVM** - Model-View-ViewModel architecture
- **Dependency Injection** - Microsoft.Extensions.*
- **iRacingSDK.Net** - iRacing telemetry integration
- **Hardcodet.NotifyIcon.Wpf** - System tray
- **CommunityToolkit.Mvvm** - MVVM helpers
- **WiX Toolset** - MSI installer creation

## Configuration

All settings in `appsettings.json`:

```json
{
  "AppSettings": {
    "LapsToConsider": 5,
    "WeightDecayFactor": 0.7,
    "NumOpponentsAhead": 3,
    "NumOpponentsBehind": 3,
    "UpdateIntervalMs": 100,
    "StartWithWindows": false,
    "AutoStartOnRace": true
  },
  "OverlaySettings": {
    "Width": 400,
    "Height": 350,
    "Opacity": 0.9,
    "ColorFaster": "#00ff00",
    "ColorSlower": "#ff0000",
    "TopMost": true
  }
}
```

## Performance

- **Install Size**: 10-15MB
- **Memory**: 20-50MB
- **CPU**: <1%
- **Startup**: <1 second
- **Update Rate**: 10Hz (100ms)

## Development

### Requirements
- .NET 8.0 SDK
- Windows 10/11
- Visual Studio 2022 (optional)

### Build Commands
```powershell
# Restore packages
dotnet restore

# Build
dotnet build --configuration Release

# Publish
dotnet publish src/iRacingOverlay/iRacingOverlay.csproj `
  --configuration Release `
  --output ./publish `
  --self-contained true `
  --runtime win-x64
```

### CI/CD
GitHub Actions automatically:
1. Builds on push to main
2. Creates MSI installer
3. Publishes release
4. Uploads installer

## Documentation

- **README.md** - User guide and features
- **TECHNICAL_OVERVIEW.md** - Architecture and implementation
- **QUICK_REFERENCE.md** - Developer quick reference
- **PROJECT_COMPLETE.md** - Project completion summary

## Usage

### System Tray Menu
Right-click the tray icon:
- Show Overlay
- Hide Overlay  
- ☐ Start with Windows
- Settings
- Exit

### Overlay
- Drag to reposition
- Resize from corner
- Auto-shows on race start
- Color-coded pace indicators

## Algorithm

### Weighted Pace
```
weight(lap_i) = decay_factor^(distance_from_most_recent)
weighted_pace = Σ(lap_time × weight) / Σ(weight)
```

### Catch Time
```
pace_advantage = opponent_pace - player_pace
distance_gap = calculate_distance_delta()
time_gap = distance_gap × average_lap_time
laps_to_catch = time_gap / pace_advantage
```

## License

See LICENSE file for details.

## Contributing

Contributions welcome! Submit pull requests or open issues.

## Support

- Check documentation files
- Open GitHub issues
- Review troubleshooting in README.md

---

**Built for the iRacing community** 🏁

