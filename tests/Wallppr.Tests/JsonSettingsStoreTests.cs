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
        var settings = new WallpprSettings();
        settings.Displays[profile.DisplayId] = profile;

        try
        {
            var store = new JsonSettingsStore(path);
            store.Save(settings);

            var loaded = store.Load();

            Assert.AreEqual(profile, loaded.Displays[profile.DisplayId]);
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
