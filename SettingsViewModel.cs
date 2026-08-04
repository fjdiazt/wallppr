using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wallppr;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    public bool StartWithWindows { get; private set; }
    public bool MinimizeToTray { get; private set; }
    public bool CloseToTray { get; private set; }

    public void Apply(AppBehaviorSettings settings)
    {
        StartWithWindows = settings.StartWithWindows;
        MinimizeToTray = settings.MinimizeToTray;
        CloseToTray = settings.CloseToTray;
        Notify(nameof(StartWithWindows));
        Notify(nameof(MinimizeToTray));
        Notify(nameof(CloseToTray));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
