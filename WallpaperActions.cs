using System.IO;

namespace Wallppr;

public sealed class WallpaperActions(
    IWallpaperPlatform platform,
    SettingsRepository settings,
    Random? random = null,
    Func<DateTimeOffset>? utcNow = null)
{
    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".tif", ".tiff", ".webp"
    };

    private readonly Random random = random ?? Random.Shared;
    private readonly Func<DateTimeOffset> utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);

    public DisplayProfile GetProfile(string displayId) =>
        settings.Current.Displays.TryGetValue(displayId, out var profile)
            ? profile
            : new DisplayProfile { DisplayId = displayId };

    public DisplayProfile SelectImage(string displayId, string imagePath)
    {
        var fullPath = RequireImage(imagePath);
        platform.SetWallpaper(displayId, fullPath);
        return Persist(GetProfile(displayId) with
        {
            Source = WallpaperSource.Image,
            ImagePath = fullPath,
            LastAppliedUtc = utcNow()
        }, wallpaperChanged: true);
    }

    public DisplayProfile SelectFolder(string displayId, string folderPath, WallpaperOrder order)
    {
        var fullPath = Path.GetFullPath(folderPath);
        var images = GetFolderImages(fullPath);
        var image = order == WallpaperOrder.Random ? images[random.Next(images.Length)] : images[0];
        platform.SetWallpaper(displayId, image);
        return Persist(GetProfile(displayId) with
        {
            Source = WallpaperSource.Folder,
            FolderPath = fullPath,
            Order = order,
            CurrentFolderImagePath = image,
            LastAppliedUtc = utcNow()
        }, wallpaperChanged: true);
    }

    public DisplayProfile Next(string displayId)
    {
        var profile = GetProfile(displayId);
        if (profile.Source != WallpaperSource.Folder || string.IsNullOrWhiteSpace(profile.FolderPath))
        {
            throw new InvalidOperationException("Select a wallpaper folder first.");
        }

        var images = GetFolderImages(profile.FolderPath);
        var currentIndex = Array.FindIndex(images, path => string.Equals(path, profile.CurrentFolderImagePath, StringComparison.OrdinalIgnoreCase));
        var nextIndex = profile.Order switch
        {
            WallpaperOrder.Random when images.Length > 1 && currentIndex >= 0 =>
                (currentIndex + random.Next(1, images.Length)) % images.Length,
            WallpaperOrder.Random => random.Next(images.Length),
            _ => currentIndex < 0 ? 0 : (currentIndex + 1) % images.Length
        };
        var image = images[nextIndex];
        platform.SetWallpaper(displayId, image);
        return Persist(profile with
        {
            CurrentFolderImagePath = image,
            LastAppliedUtc = utcNow()
        }, wallpaperChanged: true);
    }

    public DisplayProfile SetOrder(string displayId, WallpaperOrder order) =>
        Persist(GetProfile(displayId) with { Order = order }, wallpaperChanged: false);

    public DisplayProfile SetSource(string displayId, WallpaperSource source) =>
        Persist(GetProfile(displayId) with { Source = source }, wallpaperChanged: false);

    private DisplayProfile Persist(DisplayProfile profile, bool wallpaperChanged)
    {
        var current = settings.Current;
        var updated = new WallpprSettings
        {
            Version = current.Version,
            Behavior = current.Behavior,
            Displays = new Dictionary<string, DisplayProfile>(current.Displays)
            {
                [profile.DisplayId] = profile
            }
        };

        try
        {
            settings.Save(updated);
        }
        catch (Exception exception) when (wallpaperChanged)
        {
            throw new InvalidOperationException("Wallpaper changed, but settings could not be saved.", exception);
        }

        return profile;
    }

    private static string RequireImage(string imagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
        var fullPath = Path.GetFullPath(imagePath);
        if (!File.Exists(fullPath) || !SupportedImageExtensions.Contains(Path.GetExtension(fullPath)))
        {
            throw new FileNotFoundException("Wallpaper image not found or unsupported.", fullPath);
        }

        return fullPath;
    }

    private static string[] GetFolderImages(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Wallpaper folder not found: {folderPath}");
        }

        var images = Directory.EnumerateFiles(folderPath)
            .Where(path => SupportedImageExtensions.Contains(Path.GetExtension(path)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return images.Length > 0
            ? images
            : throw new InvalidOperationException("Wallpaper folder contains no supported images.");
    }
}
