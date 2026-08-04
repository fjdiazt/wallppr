using Microsoft.Win32;

namespace Wallppr;

public sealed class WindowsStartupRegistration : IStartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "wallppr";

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        if (enabled)
        {
            var executablePath = Environment.ProcessPath;
            ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
            key.SetValue(ValueName, $"\"{executablePath}\"", RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
