namespace Wallppr.Tests;

[TestClass]
public sealed class JsonSettingsStoreTests
{
    [TestMethod]
    public void Save_and_load_round_trip_every_profile_field()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"wallppr-settings-{Guid.NewGuid():N}");
        var path = Path.Combine(folder, "settings.json");
        var timestamp = new DateTimeOffset(2026, 8, 4, 12, 30, 0, TimeSpan.Zero);
        var profile = new DisplayProfile
        {
            DisplayId = "display-1",
            Source = WallpaperSource.Folder,
            FolderPath = @"C:\walls",
            Order = WallpaperOrder.Random,
            CurrentFolderImagePath = @"C:\walls\a.jpg",
            LastAppliedUtc = timestamp
        };
        var behavior = new AppBehaviorSettings
        {
            StartWithWindows = true,
            MinimizeToTray = true,
            CloseToTray = true
        };
        var cached = new MonitorWallpaper(0, "display-1", 0, 0, 1920, 1080, "wall.jpg");
        var settings = new WallpprSettings
        {
            Behavior = behavior,
            Slideshow = new SlideshowSettings { IntervalSeconds = 45 },
            CachedDisplays = [cached]
        };
        settings.Displays[profile.DisplayId] = profile;

        try
        {
            var store = new JsonSettingsStore(path);
            store.Save(settings);

            var loaded = store.Load();

            Assert.AreEqual(profile, loaded.Displays[profile.DisplayId]);
            Assert.AreEqual(behavior, loaded.Behavior);
            Assert.AreEqual(45, loaded.Slideshow.IntervalSeconds);
            Assert.AreEqual(cached, loaded.CachedDisplays.Single());
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
