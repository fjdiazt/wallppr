namespace Wallppr.Tests;

[TestClass]
public sealed class ThumbnailWallpaperActionsTests
{
    [TestMethod]
    public async Task Selecting_image_applies_wallpaper_then_persists_generated_thumbnail_identity()
    {
        var folder = CreateFolder();
        var image = Path.Combine(folder, "wall.bmp");
        WriteBitmap(image, 1200, 600);
        var platform = new FakeWallpaperPlatform();
        var store = new MemorySettingsStore();
        var cache = new WallpaperThumbnailCache(Path.Combine(folder, "cache"));
        var actions = new WallpaperActions(platform, new SettingsRepository(store, store.Settings), cache);

        try
        {
            var profile = await actions.SelectImageAsync("display-1", image);

            Assert.AreEqual(("display-1", image), platform.LastSet);
            Assert.AreEqual(image, profile.ThumbnailSourcePath);
            Assert.AreEqual(profile, store.Settings.Displays["display-1"]);
            Assert.IsTrue(File.Exists(cache.GetPath("display-1")));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public async Task Folder_selection_and_next_replace_the_display_thumbnail()
    {
        var folder = CreateFolder();
        var first = Path.Combine(folder, "a.bmp");
        var second = Path.Combine(folder, "b.bmp");
        WriteBitmap(first, 1200, 600);
        WriteBitmap(second, 600, 1200);
        var platform = new FakeWallpaperPlatform();
        var store = new MemorySettingsStore();
        var cache = new WallpaperThumbnailCache(Path.Combine(folder, "cache"));
        var actions = new WallpaperActions(platform, new SettingsRepository(store, store.Settings), cache, new Random(1));

        try
        {
            var selected = await actions.SelectFolderAsync("display-1", folder, WallpaperOrder.Sequential);
            var next = await actions.NextAsync("display-1");

            Assert.AreEqual(first, selected.ThumbnailSourcePath);
            Assert.AreEqual(second, next.ThumbnailSourcePath);
            Assert.AreEqual(next, store.Settings.Displays["display-1"]);
            CollectionAssert.AreEqual(new[] { first, second }, platform.SetPaths);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static string CreateFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"wallppr-actions-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static void WriteBitmap(string path, int width, int height)
    {
        var rowSize = (width * 3 + 3) & ~3;
        var pixelBytes = rowSize * height;
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(54 + pixelBytes);
        writer.Write(0);
        writer.Write(54);
        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1);
        writer.Write((short)24);
        writer.Write(0);
        writer.Write(pixelBytes);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0);
        writer.Write(0);
        stream.SetLength(54 + pixelBytes);
    }

    private sealed class FakeWallpaperPlatform : IWallpaperPlatform
    {
        public List<string> SetPaths { get; } = [];
        public (string DisplayId, string ImagePath)? LastSet { get; private set; }
        public IReadOnlyList<MonitorWallpaper> GetMonitors() => [];

        public void SetWallpaper(string displayId, string imagePath)
        {
            LastSet = (displayId, imagePath);
            SetPaths.Add(imagePath);
        }
    }

    private sealed class MemorySettingsStore : ISettingsStore
    {
        public WallpprSettings Settings { get; private set; } = new();
        public WallpprSettings Load() => Settings;
        public void Save(WallpprSettings settings) => Settings = settings;
    }
}
