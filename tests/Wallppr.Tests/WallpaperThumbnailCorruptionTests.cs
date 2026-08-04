namespace Wallppr.Tests;

[TestClass]
public sealed class WallpaperThumbnailCorruptionTests
{
    [TestMethod]
    public async Task Loading_a_corrupt_thumbnail_returns_null_and_removes_it()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"wallppr-corrupt-thumbnail-{Guid.NewGuid():N}");
        var cache = new WallpaperThumbnailCache(folder);
        var path = cache.GetPath("display-1");
        Directory.CreateDirectory(folder);
        File.WriteAllText(path, "not an image");

        try
        {
            Assert.IsNull(await cache.LoadAsync(path));
            Assert.IsFalse(File.Exists(path));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
