using Microsoft.Win32;

namespace TimeManager;

/// <summary>
/// Helper methods to manage whether the application runs at user logon.
/// </summary>
public static class StartupHelper
{
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "TimeManager";

    /// <summary>
    /// Returns true if the application is registered to run at user logon.
    /// </summary>
    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: false);
        return key?.GetValue(AppName) is string path
            && path.Equals(ExePath(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers the application to run at user logon.
    /// </summary>
    public static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
        key?.SetValue(AppName, ExePath());
    }

    /// <summary>
    /// Removes the application from the user logon run list.
    /// </summary>
    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }

    private static string ExePath() =>
        $"\"{Environment.ProcessPath}\"";
}
