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
    public void Action_services_preserve_each_others_settings()
    {
        var store = new MemorySettingsStore();
        var repository = new SettingsRepository(store, store.Settings);
        var behaviorActions = new AppBehaviorActions(repository, new FakeStartupRegistration());
        var wallpaperActions = new WallpaperActions(new FakeWallpaperPlatform(), repository);
        var folder = Path.Combine(Path.GetTempPath(), $"wallppr-shared-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var image = Path.Combine(folder, "wall.jpg");
        File.WriteAllBytes(image, []);

        try
        {
            behaviorActions.SetMinimizeToTray(true);
            wallpaperActions.SelectImage("display-1", image);

            Assert.IsTrue(store.Settings.Behavior.MinimizeToTray);
            Assert.AreEqual(image, store.Settings.Displays["display-1"].ImagePath);
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

    private sealed class MemorySettingsStore : ISettingsStore
    {
        public WallpprSettings Settings { get; private set; } = new();
        public WallpprSettings Load() => Settings;
        public void Save(WallpprSettings settings) => Settings = settings;
    }

    private sealed class FakeWallpaperPlatform : IWallpaperPlatform
    {
        public IReadOnlyList<MonitorWallpaper> GetMonitors() => [];
        public void SetWallpaper(string displayId, string imagePath) { }
    }
}
