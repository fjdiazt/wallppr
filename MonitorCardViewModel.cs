using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Wallppr;

public sealed class MonitorCardViewModel(MonitorWallpaper monitor) : INotifyPropertyChanged
{
    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".tif", ".tiff", ".webp"
    };

    private string currentWallpaperPath = monitor.WallpaperPath;
    private string? pendingWallpaperPath;
    private string? slideshowFolderPath;
    private string[] slideshowImages = [];
    private int slideshowImageIndex = -1;
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
            slideshowFolderPath = value;
            LoadSlideshowImages();
            Notify();
            Notify(nameof(FolderName));
        }
    }

    public string FolderName => string.IsNullOrWhiteSpace(SlideshowFolderPath)
        ? "No folder selected"
        : new DirectoryInfo(SlideshowFolderPath).Name;

    public string? FolderPreviewPath => slideshowImageIndex >= 0 ? slideshowImages[slideshowImageIndex] : null;
    public string FolderImageName => string.IsNullOrWhiteSpace(FolderPreviewPath) ? "No images found" : Path.GetFileName(FolderPreviewPath);
    public bool HasFolderImage => slideshowImageIndex >= 0;
    public bool HasNoFolderImage => !HasFolderImage;

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

    public void MoveNextFolderImage()
    {
        if (slideshowImages.Length == 0)
        {
            return;
        }

        slideshowImageIndex = slideshowImages.Length == 1
            ? 0
            : IsRandomOrder
                ? (slideshowImageIndex + Random.Shared.Next(1, slideshowImages.Length)) % slideshowImages.Length
                : (slideshowImageIndex + 1) % slideshowImages.Length;
        NotifyFolderPreview();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void LoadSlideshowImages()
    {
        try
        {
            slideshowImages = Directory.Exists(SlideshowFolderPath)
                ? Directory.EnumerateFiles(SlideshowFolderPath)
                    .Where(path => SupportedImageExtensions.Contains(Path.GetExtension(path)))
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
                : [];
        }
        catch (IOException)
        {
            slideshowImages = [];
        }
        catch (UnauthorizedAccessException)
        {
            slideshowImages = [];
        }

        slideshowImageIndex = slideshowImages.Length == 0
            ? -1
            : IsRandomOrder ? Random.Shared.Next(slideshowImages.Length) : 0;
        NotifyFolderPreview();
    }

    private void NotifyFolderPreview()
    {
        Notify(nameof(FolderPreviewPath));
        Notify(nameof(FolderImageName));
        Notify(nameof(HasFolderImage));
        Notify(nameof(HasNoFolderImage));
    }

    private void Notify([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
