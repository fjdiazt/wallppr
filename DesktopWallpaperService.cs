using System.IO;
using System.Runtime.InteropServices;

namespace Wallppr;

// COM shape derived from Les Ferch's MIT-licensed WallP implementation.
public sealed class DesktopWallpaperService : IDisposable
{
    private IDesktopWallpaper? wallpaper = (IDesktopWallpaper)new DesktopWallpaperClass();

    public IReadOnlyList<MonitorWallpaper> GetMonitors()
    {
        ObjectDisposedException.ThrowIf(wallpaper is null, this);

        var monitors = new List<MonitorWallpaper>();
        var count = wallpaper.GetMonitorDevicePathCount();
        for (uint index = 0; index < count; index++)
        {
            var id = wallpaper.GetMonitorDevicePathAt(index);
            var bounds = wallpaper.GetMonitorRECT(id);
            monitors.Add(new MonitorWallpaper(
                index,
                id,
                bounds.Left,
                bounds.Top,
                bounds.Right - bounds.Left,
                bounds.Bottom - bounds.Top,
                wallpaper.GetWallpaper(id) ?? string.Empty));
        }

        return monitors;
    }

    public void SetWallpaper(string monitorId, string imagePath)
    {
        ObjectDisposedException.ThrowIf(wallpaper is null, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(monitorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);

        var fullPath = Path.GetFullPath(imagePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Wallpaper image not found.", fullPath);
        }

        wallpaper.SetWallpaper(monitorId, fullPath);
    }

    public void Dispose()
    {
        if (wallpaper is not null)
        {
            Marshal.FinalReleaseComObject(wallpaper);
            wallpaper = null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Rect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }

    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopWallpaper
    {
        void SetWallpaper(
            [MarshalAs(UnmanagedType.LPWStr)] string monitorId,
            [MarshalAs(UnmanagedType.LPWStr)] string wallpaperPath);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorId);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetMonitorDevicePathAt(uint monitorIndex);

        uint GetMonitorDevicePathCount();

        Rect GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorId);
    }

    [ComImport]
    [Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")]
    private class DesktopWallpaperClass;
}
