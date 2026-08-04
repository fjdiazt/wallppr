namespace Wallppr;

public sealed class SettingsRepository(ISettingsStore store, WallpprSettings initialSettings)
{
    public WallpprSettings Current { get; private set; } = initialSettings;

    public void Save(WallpprSettings settings)
    {
        store.Save(settings);
        Current = settings;
    }
}
