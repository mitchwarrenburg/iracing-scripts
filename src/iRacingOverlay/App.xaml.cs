using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using iRacingOverlay.Services;
using iRacingOverlay.ViewModels;
using iRacingOverlay.Views;
using iRacingOverlay.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace iRacingOverlay;

public partial class App : Application
{
    private IHost? _host;
    private IRacingTelemetryService? _telemetryService;
    private StartupService? _startupService;
    private OverlayWindow? _overlayWindow;
    private OverlayViewModel? _overlayViewModel;
    private DispatcherTimer? _updateTimer;
    private TaskbarIcon? _notifyIcon;
    private AppSettings? _appSettings;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Setup dependency injection
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
                services.Configure<OverlaySettings>(configuration.GetSection("OverlaySettings"));

                // Services
                services.AddSingleton<IRacingTelemetryService>();
                services.AddSingleton<StartupService>();
                services.AddSingleton<OverlayViewModel>();
                services.AddTransient<OverlayWindow>();

                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                    builder.AddDebug();
                });
            })
            .Build();

        await _host.StartAsync();

        // Get services
        _telemetryService = _host.Services.GetRequiredService<IRacingTelemetryService>();
        _startupService = _host.Services.GetRequiredService<StartupService>();
        _overlayViewModel = _host.Services.GetRequiredService<OverlayViewModel>();
        _overlayWindow = _host.Services.GetRequiredService<OverlayWindow>();
        _appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();

        // Setup system tray
        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        if (_notifyIcon != null)
        {
            var startupMenuItem = _notifyIcon.ContextMenu?.Items[3] as MenuItem;
            if (startupMenuItem != null)
            {
                startupMenuItem.IsChecked = _startupService.IsStartupEnabled();
            }
        }

        // Setup telemetry events
        _telemetryService.RaceDataUpdated += OnRaceDataUpdated;

        // Setup update timer
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_appSettings?.UpdateIntervalMs ?? 100)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();

        // Show overlay initially
        if (_appSettings?.AutoStartOnRace ?? true)
        {
            _overlayWindow.Show();
        }

        // Show notification
        _notifyIcon?.ShowBalloonTip("iRacing Overlay", "Application started", BalloonIcon.Info);
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (_telemetryService?.IsConnected != true)
        {
            _telemetryService?.Connect();
        }

        _telemetryService?.Update();
    }

    private void OnRaceDataUpdated(object? sender, RaceDataUpdatedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _overlayViewModel?.UpdateRaceData(
                e.PlayerDriver,
                e.OpponentsAhead,
                e.OpponentsBehind,
                e.IsRaceActive);

            // Auto show/hide overlay based on race state
            if (_appSettings?.AutoStartOnRace ?? true)
            {
                if (e.IsRaceActive && _overlayWindow?.IsVisible != true)
                {
                    _overlayWindow?.Show();
                }
            }
        });
    }

    private void ShowOverlay_Click(object sender, RoutedEventArgs e)
    {
        _overlayWindow?.Show();
        _overlayWindow?.Activate();
    }

    private void HideOverlay_Click(object sender, RoutedEventArgs e)
    {
        _overlayWindow?.Hide();
    }

    private void StartWithWindows_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            if (menuItem.IsChecked)
            {
                _startupService?.EnableStartup();
            }
            else
            {
                _startupService?.DisableStartup();
            }
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Settings can be configured in appsettings.json\nRestart the application after making changes.",
            "Settings",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Shutdown();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _updateTimer?.Stop();
        _telemetryService?.Disconnect();
        _notifyIcon?.Dispose();

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}

