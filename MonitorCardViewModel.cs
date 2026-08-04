using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Wallppr;

public sealed class MonitorCardViewModel(MonitorWallpaper monitor) : INotifyPropertyChanged
{
    private string currentWallpaperPath = monitor.WallpaperPath;
    private string? slideshowFolderPath;
    private string? folderPreviewPath;
    private bool isFolderSource;
    private bool isRandomOrder;

    public MonitorWallpaper Monitor { get; } = monitor;
    public string Name => Monitor.Name;
    public string Resolution => Monitor.Resolution;
    public string Orientation => Monitor.Orientation;
    public string Id => Monitor.Id;
    public string CurrentWallpaperPath => currentWallpaperPath;
    public string PreviewPath => CurrentWallpaperPath;
    public string FileName => string.IsNullOrWhiteSpace(PreviewPath) ? "No wallpaper set" : Path.GetFileName(PreviewPath);
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

    public string? SlideshowFolderPath => slideshowFolderPath;
    public string FolderName => string.IsNullOrWhiteSpace(SlideshowFolderPath)
        ? "No folder selected"
        : new DirectoryInfo(SlideshowFolderPath).Name;
    public string? FolderPreviewPath => folderPreviewPath;
    public string FolderImageName => string.IsNullOrWhiteSpace(FolderPreviewPath) ? "No images found" : Path.GetFileName(FolderPreviewPath);
    public bool HasFolderImage => !string.IsNullOrWhiteSpace(FolderPreviewPath);
    public bool HasNoFolderImage => !HasFolderImage;
    public bool IsRandomOrder => isRandomOrder;
    public bool IsSequentialOrder => !IsRandomOrder;

    public void ApplyProfile(DisplayProfile profile)
    {
        isFolderSource = profile.Source == WallpaperSource.Folder;
        isRandomOrder = profile.Order == WallpaperOrder.Random;
        slideshowFolderPath = profile.FolderPath;
        folderPreviewPath = profile.CurrentFolderImagePath;

        if (!string.IsNullOrWhiteSpace(profile.ImagePath))
        {
            currentWallpaperPath = profile.ImagePath;
        }

        Notify(nameof(IsFolderSource));
        Notify(nameof(IsImageSource));
        Notify(nameof(IsRandomOrder));
        Notify(nameof(IsSequentialOrder));
        Notify(nameof(SlideshowFolderPath));
        Notify(nameof(FolderName));
        Notify(nameof(FolderPreviewPath));
        Notify(nameof(FolderImageName));
        Notify(nameof(HasFolderImage));
        Notify(nameof(HasNoFolderImage));
        Notify(nameof(CurrentWallpaperPath));
        Notify(nameof(PreviewPath));
        Notify(nameof(FileName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
