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
}
