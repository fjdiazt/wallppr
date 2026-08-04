namespace Wallppr;

public interface IWallpaperPlatform
{
    IReadOnlyList<MonitorWallpaper> GetMonitors();
    void SetWallpaper(string displayId, string imagePath);
}
