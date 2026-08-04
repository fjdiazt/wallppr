using System.IO;
using System.Runtime.InteropServices;

namespace Wallppr;

// COM shape derived from Les Ferch's MIT-licensed WallP implementation.
public sealed class DesktopWallpaperService : IWallpaperPlatform, IDisposable
{
    private IDesktopWallpaper? wallpaper = (IDesktopWallpaper)new DesktopWallpaperClass();

    public IReadOnlyList<MonitorWallpaper> GetMonitors()
    {
        ObjectDisposedException.ThrowIf(wallpaper is null, this);

        return EnumerateMonitors(
            wallpaper.GetMonitorDevicePathCount(),
            wallpaper.GetMonitorDevicePathAt,
            id =>
            {
                var bounds = wallpaper.GetMonitorRECT(id);
                return (
                    bounds.Left,
                    bounds.Top,
                    bounds.Right - bounds.Left,
                    bounds.Bottom - bounds.Top);
            },
            id => wallpaper.GetWallpaper(id));
    }

    public static IReadOnlyList<MonitorWallpaper> EnumerateMonitors(
        uint count,
        Func<uint, string> getMonitorId,
        Func<string, (int Left, int Top, int Width, int Height)> getGeometry,
        Func<string, string?> getWallpaper)
    {
        var monitors = new List<MonitorWallpaper>();
        for (uint index = 0; index < count; index++)
        {
            var id = getMonitorId(index);
            (int Left, int Top, int Width, int Height) geometry;
            try
            {
                geometry = getGeometry(id);
            }
            catch (COMException exception) when (exception.HResult == unchecked((int)0x80004005))
            {
                continue;
            }

            monitors.Add(new MonitorWallpaper(
                index,
                id,
                geometry.Left,
                geometry.Top,
                geometry.Width,
                geometry.Height,
                getWallpaper(id) ?? string.Empty));
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
