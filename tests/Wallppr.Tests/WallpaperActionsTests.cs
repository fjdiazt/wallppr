namespace Wallppr.Tests;

[TestClass]
public sealed class WallpaperActionsTests
{
    [TestMethod]
    public void Selecting_image_applies_and_persists_profile()
    {
        var folder = CreateFolder();
        var image = Path.Combine(folder, "wall.jpg");
        File.WriteAllBytes(image, []);
        var platform = new FakeWallpaperPlatform();
        var store = new MemorySettingsStore();
        var timestamp = new DateTimeOffset(2026, 8, 4, 15, 0, 0, TimeSpan.Zero);
        var actions = new WallpaperActions(platform, store, store.Settings, new Random(1), () => timestamp);

        try
        {
            var profile = actions.SelectImage("display-1", image);

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
    public void Sequential_folder_selection_and_next_apply_and_wrap()
    {
        var folder = CreateFolder();
        var first = Path.Combine(folder, "a.jpg");
        var second = Path.Combine(folder, "b.png");
        File.WriteAllBytes(second, []);
        File.WriteAllBytes(first, []);
        File.WriteAllBytes(Path.Combine(folder, "ignored.txt"), []);
        var platform = new FakeWallpaperPlatform();
        var store = new MemorySettingsStore();
        var actions = new WallpaperActions(platform, store, store.Settings, new Random(1));

        try
        {
            Assert.AreEqual(first, actions.SelectFolder("display-1", folder, WallpaperOrder.Sequential).CurrentFolderImagePath);
            Assert.AreEqual(second, actions.Next("display-1").CurrentFolderImagePath);
            Assert.AreEqual(first, actions.Next("display-1").CurrentFolderImagePath);
            CollectionAssert.AreEqual(new[] { first, second, first }, platform.SetPaths);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void Random_next_avoids_immediate_repeat()
    {
        var folder = CreateFolder();
        File.WriteAllBytes(Path.Combine(folder, "a.jpg"), []);
        File.WriteAllBytes(Path.Combine(folder, "b.png"), []);
        var platform = new FakeWallpaperPlatform();
        var store = new MemorySettingsStore();
        var actions = new WallpaperActions(platform, store, store.Settings, new Random(1));

        try
        {
            var first = actions.SelectFolder("display-1", folder, WallpaperOrder.Random).CurrentFolderImagePath;
            var second = actions.Next("display-1").CurrentFolderImagePath;

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
        var actions = new WallpaperActions(platform, store, store.Settings, new Random(1));

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
        var actions = new WallpaperActions(platform, store, store.Settings);

        var profile = actions.SetSource("display-1", WallpaperSource.Folder);

        Assert.AreEqual(WallpaperSource.Folder, profile.Source);
        Assert.IsEmpty(platform.SetPaths);
        Assert.AreEqual(profile, store.Settings.Displays["display-1"]);
    }


    [TestMethod]
    public void Platform_failure_does_not_persist_profile()
    {
        var folder = CreateFolder();
        var image = Path.Combine(folder, "wall.jpg");
        File.WriteAllBytes(image, []);
        var platform = new FakeWallpaperPlatform { ThrowOnSet = true };
        var store = new MemorySettingsStore();
        var actions = new WallpaperActions(platform, store, store.Settings, new Random(1));

        try
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => actions.SelectImage("display-1", image));
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
