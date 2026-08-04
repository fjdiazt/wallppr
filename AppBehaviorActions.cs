namespace Wallppr;

public sealed class AppBehaviorActions(SettingsRepository settings, IStartupRegistration startupRegistration)
{
    public AppBehaviorSettings Current => settings.Current.Behavior;
    public event Action<AppBehaviorSettings>? Changed;

    public AppBehaviorSettings SetStartWithWindows(bool enabled)
    {
        var previous = Current.StartWithWindows;
        startupRegistration.SetEnabled(enabled);

        try
        {
            return Save(Current with { StartWithWindows = enabled });
        }
        catch
        {
            startupRegistration.SetEnabled(previous);
            throw;
        }
    }

    public AppBehaviorSettings SetMinimizeToTray(bool enabled) =>
        Save(Current with { MinimizeToTray = enabled });

    public AppBehaviorSettings SetCloseToTray(bool enabled) =>
        Save(Current with { CloseToTray = enabled });

    private AppBehaviorSettings Save(AppBehaviorSettings behavior)
    {
        var current = settings.Current;
        settings.Save(current with { Behavior = behavior });
        Changed?.Invoke(behavior);
        return behavior;
    }
}
