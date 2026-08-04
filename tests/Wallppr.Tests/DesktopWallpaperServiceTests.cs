namespace Wallppr.Tests;

[TestClass]
public sealed class DesktopWallpaperServiceTests
{
    [TestMethod]
    public void Monitor_wallpaper_reports_geometry_without_platform_access()
    {
        var monitor = new MonitorWallpaper(0, "display-1", 10, 20, 3440, 1440, "wall.jpg");

        Assert.AreEqual("3440 × 1440", monitor.Resolution);
        Assert.AreEqual("Ultrawide", monitor.Orientation);
    }
}
