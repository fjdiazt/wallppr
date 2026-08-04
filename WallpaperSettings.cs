namespace Wallppr;

public enum WallpaperSource
{
    Image,
    Folder
}

public enum WallpaperOrder
{
    Sequential,
    Random
}

public sealed record DisplayProfile
{
    public required string DisplayId { get; init; }
    public WallpaperSource Source { get; init; }
    public string? ImagePath { get; init; }
    public string? FolderPath { get; init; }
    public WallpaperOrder Order { get; init; }
    public string? CurrentFolderImagePath { get; init; }
    public DateTimeOffset? LastAppliedUtc { get; init; }
}

public sealed class WallpprSettings
{
    public int Version { get; init; } = 1;
    public Dictionary<string, DisplayProfile> Displays { get; init; } = [];
}
