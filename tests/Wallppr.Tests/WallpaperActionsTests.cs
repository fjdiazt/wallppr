namespace Wallppr.Tests;

[TestClass]
public sealed class WallpaperActionsTests
{
    [TestMethod]
    public async Task Selecting_image_applies_and_persists_profile()
    {
        var folder = CreateFolder();
        var image = Path.Combine(folder, "wall.jpg");
        File.WriteAllBytes(image, []);
        var platform = new FakeWallpaperPlatform();
        var store = new MemorySettingsStore();
        var timestamp = new DateTimeOffset(2026, 8, 4, 15, 0, 0, TimeSpan.Zero);
        var cache = new WallpaperThumbnailCache(Path.Combine(folder, "cache"));
        var actions = new WallpaperActions(platform, new SettingsRepository(store, store.Settings), cache, new Random(1), () => timestamp);

        try
        {
            var profile = await actions.SelectImageAsync("display-1", image);

            Assert.AreEqual(("display-1", image), platform.LastSet);
            Assert.AreEqual(WallpaperSource.Image, profile.Source);
            Assert.AreEqual(image, profile.ImagePath);
            Assert.AreEqual(timestamp, profile.LastAppliedUtc);
            Assert.AreEqual(profile, store.Settings.Displays["display-1"]);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public async Task Sequential_folder_selection_and_next_apply_and_wrap()
    {
        var folder = CreateFolder();
        var first = Path.Combine(folder, "a.jpg");
        var second = Path.Combine(folder, "b.png");
        File.WriteAllBytes(second, []);
        File.WriteAllBytes(first, []);
        File.WriteAllBytes(Path.Combine(folder, "ignored.txt"), []);
        var platform = new FakeWallpaperPlatform();
        var store = new MemorySettingsStore();
        var cache = new WallpaperThumbnailCache(Path.Combine(folder, "cache"));
        var actions = new WallpaperActions(platform, new SettingsRepository(store, store.Settings), cache, new Random(1));

        try
        {
            Assert.AreEqual(first, (await actions.SelectFolderAsync("display-1", folder, WallpaperOrder.Sequential)).CurrentFolderImagePath);
            Assert.AreEqual(second, (await actions.NextAsync("display-1")).CurrentFolderImagePath);
            Assert.AreEqual(first, (await actions.NextAsync("display-1")).CurrentFolderImagePath);
            CollectionAssert.AreEqual(new[] { first, second, first }, platform.SetPaths);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public async Task Random_next_avoids_immediate_repeat()
    {
        var folder = CreateFolder();
        File.WriteAllBytes(Path.Combine(folder, "a.jpg"), []);
        File.WriteAllBytes(Path.Combine(folder, "b.png"), []);
        var platform = new FakeWallpaperPlatform();
        var store = new MemorySettingsStore();
        var cache = new WallpaperThumbnailCache(Path.Combine(folder, "cache"));
        var actions = new WallpaperActions(platform, new SettingsRepository(store, store.Settings), cache, new Random(1));

        try
        {
            var first = (await actions.SelectFolderAsync("display-1", folder, WallpaperOrder.Random)).CurrentFolderImagePath;
            var second = (await actions.NextAsync("display-1")).CurrentFolderImagePath;

            Assert.AreNotEqual(first, second);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void Changing_order_persists_without_applying_wallpaper()
    {
        var platform = new FakeWallpaperPlatform();
        var store = new MemorySettingsStore();
        var actions = new WallpaperActions(platform, new SettingsRepository(store, store.Settings), CreateCache(), new Random(1));

        var profile = actions.SetOrder("display-1", WallpaperOrder.Random);

        Assert.AreEqual(WallpaperOrder.Random, profile.Order);
        Assert.IsEmpty(platform.SetPaths);
        Assert.AreEqual(profile, store.Settings.Displays["display-1"]);
    }

    [TestMethod]
    public void Changing_source_persists_without_applying_wallpaper()
    {
        var platform = new FakeWallpaperPlatform();
        var store = new MemorySettingsStore();
        var actions = new WallpaperActions(platform, new SettingsRepository(store, store.Settings), CreateCache());

        var profile = actions.SetSource("display-1", WallpaperSource.Folder);

        Assert.AreEqual(WallpaperSource.Folder, profile.Source);
        Assert.IsEmpty(platform.SetPaths);
        Assert.AreEqual(profile, store.Settings.Displays["display-1"]);
    }


    [TestMethod]
    public async Task Platform_failure_does_not_persist_profile()
    {
        var folder = CreateFolder();
        var image = Path.Combine(folder, "wall.jpg");
        File.WriteAllBytes(image, []);
        var platform = new FakeWallpaperPlatform { ThrowOnSet = true };
        var store = new MemorySettingsStore();
        var cache = new WallpaperThumbnailCache(Path.Combine(folder, "cache"));
        var actions = new WallpaperActions(platform, new SettingsRepository(store, store.Settings), cache, new Random(1));

        try
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => actions.SelectImageAsync("display-1", image));
            Assert.IsFalse(store.Settings.Displays.ContainsKey("display-1"));
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

    private static WallpaperThumbnailCache CreateCache() =>
        new(Path.Combine(Path.GetTempPath(), $"wallppr-cache-{Guid.NewGuid():N}"));

    private sealed class FakeWallpaperPlatform : IWallpaperPlatform
    {
        public bool ThrowOnSet { get; init; }
        public List<string> SetPaths { get; } = [];
        public (string DisplayId, string ImagePath)? LastSet { get; private set; }

        public IReadOnlyList<MonitorWallpaper> GetMonitors() => [];

        public void SetWallpaper(string displayId, string imagePath)
        {
            if (ThrowOnSet)
            {
                throw new InvalidOperationException("platform failure");
            }

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
