# iRacing Position Overlay

A professional Windows application for iRacing that provides real-time race position overlay with intelligent catch predictions.

## Features

### Core Functionality
- ✅ **Real-time Position Tracking** - Shows 3 opponents ahead and 3 behind in your class
- ✅ **Smart Pace Calculations** - Weighted average of recent laps (configurable)
- ✅ **Catch Time Predictions** - Estimates laps to catch based on pace differential
- ✅ **Real-time Data Integration** - Uses speed, distance, and relative time
- ✅ **Color-Coded Display** - Green (faster), Red (slower)
- ✅ **High Precision** - 2 decimal places for lap predictions

### Windows Integration
- ✅ **System Tray Icon** - Runs in background, accessible from taskbar
- ✅ **Auto-Start with Windows** - Optional startup configuration
- ✅ **Auto-Show on Race** - Automatically displays when race starts
- ✅ **Minimize to Tray** - Keeps running in background
- ✅ **MSI Installer** - Professional installation experience
- ✅ **Small Footprint** - ~10-15MB installed size
- ✅ **Native Performance** - Instant startup, minimal resources

## Features

- **Real-time Position Tracking**: Shows 3 opponents ahead and 3 behind in your class
- **Smart Pace Calculations**: Uses weighted average of recent laps (configurable, default: 5 laps)
- **Weighted Lap Times**: More recent laps have higher weight in pace calculations using exponential decay
- **Catch Time Predictions**: Estimates how many laps until you catch opponents ahead or they catch you
- **Color-Coded Display**: 
  - 🟢 Green: You have pace advantage (faster than opponent)
  - 🔴 Red: Opponent has pace advantage (slower than opponent)
- **Real-time Distance & Time Gaps**: Shows actual time delta and distance to each opponent
- **High Precision**: 2 decimal point precision for lap predictions
- **Race Session Detection**: Only activates during active race sessions with other cars on track

## Installation

### From Release (Recommended)
1. Download `iRacingOverlay-Setup.msi` from the [latest release](../../releases)
2. Run the installer
3. Launch "iRacing Overlay" from Start Menu
4. Application will appear in system tray

### From Source
```powershell
# Clone repository
git clone https://github.com/yourusername/iracing-scripts.git
cd iracing-scripts

# Build and run
dotnet restore
dotnet build
dotnet run --project src/iRacingOverlay
```

### Requirements
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (included in installer)
- iRacing installed

## Usage

### First Launch
1. Start the application from Start Menu or system tray
2. Right-click the tray icon for options
3. Join an iRacing race session
4. Overlay will automatically appear when race starts

### System Tray Menu
- **Show Overlay** - Display the overlay window
- **Hide Overlay** - Hide the overlay (continues monitoring)
- **Start with Windows** - Enable/disable auto-start
- **Settings** - Configure application (edit appsettings.json)
- **Exit** - Close application completely

### Overlay Window
- **Draggable** - Click and drag to reposition
- **Resizable** - Use resize grip in corner
- **Always on Top** - Stays visible over iRacing
- **Semi-transparent** - Configurable opacity

## Configuration

Edit `appsettings.json` in installation directory:

```json
{
  "AppSettings": {
    "LapsToConsider": 5,          // Number of laps for pace calculation
    "WeightDecayFactor": 0.7,     // Recent lap weighting (0-1)
    "NumOpponentsAhead": 3,       // Opponents to show ahead
    "NumOpponentsBehind": 3,      // Opponents to show behind
    "UpdateIntervalMs": 100,      // Update frequency (milliseconds)
    "LapsPrecision": 2,           // Decimal places
    "StartWithWindows": false,    // Auto-start enabled
    "MinimizeToTray": true,       // Minimize behavior
    "AutoStartOnRace": true       // Show overlay on race start
  },
  "OverlaySettings": {
    "Width": 400,                 // Window width (pixels)
    "Height": 350,                // Window height (pixels)
    "Opacity": 0.9,               // Transparency (0-1)
    "BackgroundColor": "#1a1a1a", // Background (hex)
    "TextColor": "#ffffff",       // Text color
    "ColorFaster": "#00ff00",     // Faster pace color
    "ColorSlower": "#ff0000",     // Slower pace color
    "TopMost": true               // Always on top
  }
}
```

## How It Works

### Pace Calculation

The application calculates a **weighted average lap time** for each driver using the most recent N laps (configurable). More recent laps receive higher weight using exponential decay:

```
weight(lap_i) = decay_factor^(distance_from_most_recent)
weighted_avg = Σ(lap_time * weight) / Σ(weight)
```

Default decay factor: 0.7 (most recent lap has weight 1.0, previous lap 0.7, etc.)

### Catch Time Prediction

For each opponent, the application calculates:

1. **Pace Advantage**: Difference in weighted average lap times
2. **Distance Gap**: Current distance between you and opponent (in lap fractions)
3. **Time Gap**: Distance gap converted to seconds using average pace
4. **Laps to Catch**: Time gap divided by pace advantage per lap

The prediction accounts for:
- Current lap number difference
- Track position percentage
- Real-time telemetry data
- Lap wrapping (e.g., when opponent is ahead but on previous lap)

### Real-time Refinement

The application uses iRacing's real-time telemetry:
- `CarIdxLapDistPct`: Track position percentage for distance calculations
- `CarIdxEstTime`: Estimated time for relative positioning
- `CarIdxLastLapTime`: Completed lap times for pace history

## Display Information

### For Each Opponent:

```
P2 - Driver Name                    Catch in: 3.47 laps
                                    +0.234s/lap | Gap: 12.45s
```

- **P2**: Position in class
- **Driver Name**: Competitor name
- **Catch in X laps**: Estimated laps until catch (shown in GREEN if you're faster)
- **±X.XXXs/lap**: Pace advantage/disadvantage
- **Gap: X.XXs**: Current time gap

### Color Coding:

**For Opponents Ahead:**
- 🟢 Green: You're faster - will catch them
- 🔴 Red: They're faster - won't catch them

**For Opponents Behind:**
- 🟢 Green: You're faster - maintaining gap
- 🔴 Red: They're faster - will catch you

## Troubleshooting

### Application won't start
- Install .NET 8.0 Runtime from microsoft.com
- Run as Administrator if needed
- Check Windows Event Viewer for errors

### No connection to iRacing
- Ensure iRacing is running
- Join a session (not in menus)
- Check firewall settings

### Overlay not showing
- Right-click tray icon → Show Overlay
- Check if positioned off-screen (reset position)
- Verify "TopMost" is enabled in settings

### Auto-start not working
- Enable "Start with Windows" from tray menu
- Check Windows Task Manager → Startup tab
- Verify registry key: `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`

## Building

### Requirements
- .NET 8.0 SDK
- Windows 10/11
- Visual Studio 2022 (optional, for IDE support)

### Build Commands

```powershell
# Restore dependencies
dotnet restore

# Build Debug
dotnet build --configuration Debug

# Build Release
dotnet build --configuration Release

# Publish self-contained executable
dotnet publish src/iRacingOverlay/iRacingOverlay.csproj `
  --configuration Release `
  --output ./publish `
  --self-contained true `
  --runtime win-x64 `
  /p:PublishSingleFile=true
```

## Project Structure

```
iracing-scripts/
├── src/
│   └── iRacingOverlay/
│       ├── Models/              # Data models
│       ├── Services/            # Business logic
│       ├── Views/               # WPF views
│       ├── ViewModels/          # MVVM view models
│       ├── Resources/           # Icons, images
│       ├── App.xaml             # Application entry
│       └── appsettings.json     # Configuration
├── .github/
│   └── workflows/
│       └── dotnet-build.yml     # CI/CD pipeline
└── iRacingOverlay.sln           # Solution file
```

## Performance

- **Install Size**: 10-15MB
- **Memory Usage**: 20-50MB
- **CPU Usage**: <1%
- **Startup Time**: <1 second
- **Update Rate**: 10Hz (100ms)

## Technology Stack

- **.NET 8.0** - Framework
- **WPF** - UI framework
- **MVVM** - Architecture pattern
- **Hardcodet.NotifyIcon.Wpf** - System tray
- **iRacingSDK.Net** - iRacing telemetry
- **CommunityToolkit.Mvvm** - MVVM helpers
- **WiX Toolset** - Installer creation

## License

See LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## Acknowledgments

- Built using iRacingSDK.Net
- System tray icon by Hardcodet.NotifyIcon.Wpf
- MVVM toolkit by CommunityToolkit

