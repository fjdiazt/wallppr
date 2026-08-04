namespace Wallppr;

public sealed class DisplayDiscovery(IWallpaperPlatform platform, SettingsRepository settings)
{
    public Task<IReadOnlyList<MonitorWallpaper>> LoadAsync(bool refresh) =>
        !refresh && settings.Current.CachedDisplays.Count > 0
            ? Task.FromResult<IReadOnlyList<MonitorWallpaper>>(settings.Current.CachedDisplays)
            : RefreshAsync();

    private async Task<IReadOnlyList<MonitorWallpaper>> RefreshAsync()
    {
        var monitors = await Task.Run(platform.GetMonitors);
        var cached = monitors.ToList();
        settings.Save(settings.Current with { CachedDisplays = cached });
        return cached;
    }
}
