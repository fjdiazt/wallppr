using System.Runtime.InteropServices;

namespace Wallppr.Tests;

[TestClass]
public sealed class MonitorEnumerationTests
{
    [TestMethod]
    public void Enumeration_skips_inactive_virtual_display_without_rectangle()
    {
        var ids = new[] { "active-virtual", "inactive-virtual" };

        var monitors = DesktopWallpaperService.EnumerateMonitors(
            2,
            index => ids[(int)index],
            id => id == "inactive-virtual"
                ? throw new COMException("inactive", unchecked((int)0x80004005))
                : (0, 0, 1920, 1080),
            _ => "wall.jpg");

        Assert.HasCount(1, monitors);
        Assert.AreEqual("active-virtual", monitors[0].Id);
    }

    [TestMethod]
    public void Enumeration_does_not_hide_other_COM_failures()
    {
        Assert.ThrowsExactly<COMException>(() => DesktopWallpaperService.EnumerateMonitors(
            1,
            _ => "active-display",
            _ => throw new COMException("denied", unchecked((int)0x80070005)),
            _ => "wall.jpg"));
    }
}
