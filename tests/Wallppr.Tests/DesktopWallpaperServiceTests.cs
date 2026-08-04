namespace Wallppr.Tests;

[TestClass]
public sealed class DesktopWallpaperServiceTests
{
    [TestMethod]
    public void GetMonitors_returns_active_monitor_geometry()
    {
        using var service = new DesktopWallpaperService();

        var monitors = service.GetMonitors();

        Assert.IsGreaterThan(0, monitors.Count);
        Assert.IsTrue(monitors.All(monitor =>
            !string.IsNullOrWhiteSpace(monitor.Id) &&
            monitor.Width > 0 &&
            monitor.Height > 0));
    }
}
