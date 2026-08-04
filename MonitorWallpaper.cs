namespace Wallppr;

public sealed record MonitorWallpaper(
    uint Index,
    string Id,
    int Left,
    int Top,
    int Width,
    int Height,
    string WallpaperPath)
{
    public string Name => $"Display {Index + 1}";
    public string Resolution => $"{Width} × {Height}";
    public string Orientation => Height > Width ? "Portrait" : Width > Height * 2 ? "Ultrawide" : "Landscape";
}
