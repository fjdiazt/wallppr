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
}
