namespace Wallppr.Tests;

[TestClass]
public sealed class WallpaperThumbnailCacheTests
{
    [TestMethod]
    public async Task Create_and_load_produces_a_local_frozen_downsampled_thumbnail()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"wallppr-thumbnails-{Guid.NewGuid():N}");
        var source = Path.Combine(folder, "source.bmp");
        Directory.CreateDirectory(folder);
        WriteBitmap(source, width: 1200, height: 600);
        var cache = new WallpaperThumbnailCache(Path.Combine(folder, "cache"));

        try
        {
            var path = await cache.CreateAsync("display-1", source);
            var image = await cache.LoadAsync(path);

            Assert.AreEqual(cache.GetPath("display-1"), path);
            Assert.IsTrue(File.Exists(path));
            Assert.IsNotNull(image);
            Assert.IsTrue(image.IsFrozen);
            Assert.AreEqual(640, image.PixelWidth);
            Assert.AreEqual(320, image.PixelHeight);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
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
}
