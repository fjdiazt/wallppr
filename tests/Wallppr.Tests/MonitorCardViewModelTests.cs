namespace Wallppr.Tests;

[TestClass]
public sealed class MonitorCardViewModelTests
{
    [TestMethod]
    public void Selecting_wallpaper_updates_preview_path()
    {
        var monitor = new MonitorWallpaper(0, "monitor-id", 0, 0, 1920, 1080, "current.jpg");
        var viewModel = new MonitorCardViewModel(monitor);

        viewModel.PendingWallpaperPath = "next.png";

        Assert.AreEqual("next.png", viewModel.PreviewPath);
        Assert.IsTrue(viewModel.HasPendingWallpaper);
    }

    [TestMethod]
    public void Folder_source_preserves_image_and_folder_choices()
    {
        var monitor = new MonitorWallpaper(0, "monitor-id", 0, 0, 1920, 1080, "current.jpg");
        var viewModel = new MonitorCardViewModel(monitor)
        {
            PendingWallpaperPath = "next.png",
            SlideshowFolderPath = @"C:\wallpapers",
            IsFolderSource = true,
            IsRandomOrder = true
        };

        Assert.IsTrue(viewModel.IsFolderSource);
        Assert.IsFalse(viewModel.IsImageSource);
        Assert.IsTrue(viewModel.IsRandomOrder);
        Assert.IsFalse(viewModel.IsSequentialOrder);
        Assert.AreEqual("wallpapers", viewModel.FolderName);
        Assert.AreEqual("next.png", viewModel.PendingWallpaperPath);
    }

    [TestMethod]
    public void Sequential_folder_preview_filters_sorts_and_wraps()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"wallppr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);

        try
        {
            File.WriteAllBytes(Path.Combine(folder, "b.png"), []);
            File.WriteAllBytes(Path.Combine(folder, "a.jpg"), []);
            File.WriteAllBytes(Path.Combine(folder, "ignored.txt"), []);
            var viewModel = new MonitorCardViewModel(new MonitorWallpaper(0, "monitor-id", 0, 0, 1920, 1080, "current.jpg"))
            {
                SlideshowFolderPath = folder
            };

            Assert.AreEqual(Path.Combine(folder, "a.jpg"), viewModel.FolderPreviewPath);
            Assert.IsTrue(viewModel.HasFolderImage);

            viewModel.MoveNextFolderImage();
            Assert.AreEqual(Path.Combine(folder, "b.png"), viewModel.FolderPreviewPath);

            viewModel.MoveNextFolderImage();
            Assert.AreEqual(Path.Combine(folder, "a.jpg"), viewModel.FolderPreviewPath);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void Random_folder_preview_avoids_immediate_repeat()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"wallppr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);

        try
        {
            File.WriteAllBytes(Path.Combine(folder, "a.jpg"), []);
            File.WriteAllBytes(Path.Combine(folder, "b.png"), []);
            var viewModel = new MonitorCardViewModel(new MonitorWallpaper(0, "monitor-id", 0, 0, 1920, 1080, "current.jpg"))
            {
                IsRandomOrder = true,
                SlideshowFolderPath = folder
            };
            var first = viewModel.FolderPreviewPath;

            viewModel.MoveNextFolderImage();

            Assert.AreNotEqual(first, viewModel.FolderPreviewPath);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
