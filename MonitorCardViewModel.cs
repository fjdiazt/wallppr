using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Wallppr;

public sealed class MonitorCardViewModel(MonitorWallpaper monitor) : INotifyPropertyChanged
{
    private string currentWallpaperPath = monitor.WallpaperPath;
    private string? pendingWallpaperPath;
    private string? slideshowFolderPath;
    private bool isFolderSource;
    private bool isRandomOrder;

    public MonitorWallpaper Monitor { get; } = monitor;
    public string Name => Monitor.Name;
    public string Resolution => Monitor.Resolution;
    public string Orientation => Monitor.Orientation;
    public string Id => Monitor.Id;
    public string CurrentWallpaperPath
    {
        get => currentWallpaperPath;
        set
        {
            currentWallpaperPath = value;
            Notify();
            Notify(nameof(PreviewPath));
            Notify(nameof(FileName));
        }
    }

    public string? PendingWallpaperPath
    {
        get => pendingWallpaperPath;
        set
        {
            pendingWallpaperPath = value;
            Notify();
            Notify(nameof(PreviewPath));
            Notify(nameof(FileName));
            Notify(nameof(HasPendingWallpaper));
        }
    }

    public string PreviewPath => PendingWallpaperPath ?? CurrentWallpaperPath;
    public string FileName => string.IsNullOrWhiteSpace(PreviewPath) ? "No wallpaper set" : Path.GetFileName(PreviewPath);
    public bool HasPendingWallpaper => !string.IsNullOrWhiteSpace(PendingWallpaperPath);
    public bool IsImageSource => !IsFolderSource;
    public bool IsFolderSource
    {
        get => isFolderSource;
        set
        {
            if (isFolderSource == value)
            {
                return;
            }

            isFolderSource = value;
            Notify();
            Notify(nameof(IsImageSource));
        }
    }

    public string? SlideshowFolderPath
    {
        get => slideshowFolderPath;
        set
        {
            if (slideshowFolderPath == value)
            {
                return;
            }

            slideshowFolderPath = value;
            Notify();
            Notify(nameof(FolderName));
        }
    }

    public string FolderName => string.IsNullOrWhiteSpace(SlideshowFolderPath)
        ? "No folder selected"
        : new DirectoryInfo(SlideshowFolderPath).Name;

    public bool IsRandomOrder
    {
        get => isRandomOrder;
        set
        {
            if (isRandomOrder == value)
            {
                return;
            }

            isRandomOrder = value;
            Notify();
            Notify(nameof(IsSequentialOrder));
        }
    }
    public bool IsSequentialOrder => !IsRandomOrder;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
