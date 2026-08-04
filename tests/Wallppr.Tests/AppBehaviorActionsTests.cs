namespace Wallppr.Tests;

[TestClass]
public sealed class AppBehaviorActionsTests
{
    [TestMethod]
    public void Tray_behavior_persists_without_touching_startup_registration()
    {
        var store = new MemorySettingsStore();
        var repository = new SettingsRepository(store, store.Settings);
        var startup = new FakeStartupRegistration();
        var actions = new AppBehaviorActions(repository, startup);

        var behavior = actions.SetCloseToTray(true);

        Assert.IsTrue(behavior.CloseToTray);
        Assert.IsFalse(startup.WasCalled);
        Assert.IsTrue(store.Settings.Behavior.CloseToTray);
    }

    [TestMethod]
    public void Startup_failure_does_not_persist_enabled_state()
    {
        var store = new MemorySettingsStore();
        var repository = new SettingsRepository(store, store.Settings);
        var actions = new AppBehaviorActions(repository, new FakeStartupRegistration { ThrowOnSet = true });

        Assert.ThrowsExactly<InvalidOperationException>(() => actions.SetStartWithWindows(true));

        Assert.IsFalse(store.Settings.Behavior.StartWithWindows);
    }

    [TestMethod]
    public async Task Action_services_preserve_each_others_settings()
    {
        var cached = new MonitorWallpaper(0, "display-1", 0, 0, 1920, 1080, "cached.jpg");
        var store = new MemorySettingsStore(new WallpprSettings { CachedDisplays = [cached] });
        var repository = new SettingsRepository(store, store.Settings);
        var behaviorActions = new AppBehaviorActions(repository, new FakeStartupRegistration());
        var folder = Path.Combine(Path.GetTempPath(), $"wallppr-shared-{Guid.NewGuid():N}");
        var wallpaperActions = new WallpaperActions(new FakeWallpaperPlatform(), repository, new WallpaperThumbnailCache(Path.Combine(folder, "cache")));
        Directory.CreateDirectory(folder);
        var image = Path.Combine(folder, "wall.jpg");
        File.WriteAllBytes(image, []);

        try
        {
            behaviorActions.SetMinimizeToTray(true);
            Assert.AreEqual(cached, store.Settings.CachedDisplays.Single());
            await wallpaperActions.SelectImageAsync("display-1", image);

            Assert.IsTrue(store.Settings.Behavior.MinimizeToTray);
            Assert.AreEqual(image, store.Settings.Displays["display-1"].ImagePath);
            Assert.AreEqual(cached, store.Settings.CachedDisplays.Single());
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private sealed class FakeStartupRegistration : IStartupRegistration
    {
        public bool ThrowOnSet { get; init; }
        public bool WasCalled { get; private set; }

        public void SetEnabled(bool enabled)
        {
            WasCalled = true;
            if (ThrowOnSet)
            {
                throw new InvalidOperationException("registry failure");
            }
        }
    }

    private sealed class MemorySettingsStore(WallpprSettings? settings = null) : ISettingsStore
    {
        public WallpprSettings Settings { get; private set; } = settings ?? new();
        public WallpprSettings Load() => Settings;
        public void Save(WallpprSettings settings) => Settings = settings;
    }

    private sealed class FakeWallpaperPlatform : IWallpaperPlatform
    {
        public IReadOnlyList<MonitorWallpaper> GetMonitors() => [];
        public void SetWallpaper(string displayId, string imagePath) { }
    }
}
