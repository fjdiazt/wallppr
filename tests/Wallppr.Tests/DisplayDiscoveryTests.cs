namespace Wallppr.Tests;

[TestClass]
public sealed class DisplayDiscoveryTests
{
    [TestMethod]
    public async Task Cached_load_skips_platform_enumeration()
    {
        var cached = new MonitorWallpaper(0, "display-1", 0, 0, 1920, 1080, "wall.jpg");
        var store = new MemorySettingsStore(new WallpprSettings { CachedDisplays = [cached] });
        var platform = new BlockingWallpaperPlatform();
        var discovery = new DisplayDiscovery(platform, new SettingsRepository(store, store.Settings));

        var monitors = await discovery.LoadAsync(refresh: false);

        Assert.AreEqual(cached, monitors.Single());
        Assert.AreEqual(0, platform.EnumerationCount);
    }

    [TestMethod]
    public async Task Refresh_runs_without_blocking_caller_and_persists_result()
    {
        var discovered = new MonitorWallpaper(1, "display-2", 1920, 0, 3440, 1440, "wide.jpg");
        var store = new MemorySettingsStore(new WallpprSettings
        {
            CachedDisplays = [new MonitorWallpaper(0, "old", 0, 0, 800, 600, "old.jpg")]
        });
        var platform = new BlockingWallpaperPlatform { Result = [discovered] };
        var discovery = new DisplayDiscovery(platform, new SettingsRepository(store, store.Settings));

        var loading = discovery.LoadAsync(refresh: true);
        try
        {
            Assert.IsTrue(platform.Started.Wait(TimeSpan.FromSeconds(2)));
            Assert.IsFalse(loading.IsCompleted);
        }
        finally
        {
            platform.Release.Set();
        }

        var monitors = await loading;

        Assert.AreEqual(discovered, monitors.Single());
        Assert.AreEqual(discovered, store.Settings.CachedDisplays.Single());
    }

    private sealed class BlockingWallpaperPlatform : IWallpaperPlatform
    {
        public IReadOnlyList<MonitorWallpaper> Result { get; init; } = [];
        public ManualResetEventSlim Started { get; } = new();
        public ManualResetEventSlim Release { get; } = new();
        public int EnumerationCount { get; private set; }

        public IReadOnlyList<MonitorWallpaper> GetMonitors()
        {
            EnumerationCount++;
            Started.Set();
            Release.Wait();
            return Result;
        }

        public void SetWallpaper(string displayId, string imagePath) { }
    }

    private sealed class MemorySettingsStore(WallpprSettings settings) : ISettingsStore
    {
        public WallpprSettings Settings { get; private set; } = settings;
        public WallpprSettings Load() => Settings;
        public void Save(WallpprSettings value) => Settings = value;
    }
}
