using System;
using System.IO;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;

namespace iRacingOverlay.Services;

public class StartupService
{
    private const string AppName = "iRacingOverlay";
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private readonly ILogger<StartupService> _logger;

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            var value = key?.GetValue(AppName);
            return value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking startup status");
            return false;
        }
    }

    public bool EnableStartup()
    {
        try
        {
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            exePath = exePath.Replace(".dll", ".exe");

            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            key?.SetValue(AppName, $"\"{exePath}\"");

            _logger.LogInformation("Startup enabled");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling startup");
            return false;
        }
    }

    public bool DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            key?.DeleteValue(AppName, false);

            _logger.LogInformation("Startup disabled");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling startup");
            return false;
        }
    }
}

