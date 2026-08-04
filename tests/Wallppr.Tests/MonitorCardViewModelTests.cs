namespace Wallppr.Tests;

[TestClass]
public sealed class MonitorCardViewModelTests
{
    [TestMethod]
    public void Applying_image_profile_updates_image_presentation_state()
    {
        var viewModel = new MonitorCardViewModel(new MonitorWallpaper(0, "display-1", 0, 0, 1920, 1080, "current.jpg"));
        var profile = new DisplayProfile
        {
            DisplayId = "display-1",
            Source = WallpaperSource.Image,
            ImagePath = @"C:\walls\next.png"
        };

        viewModel.ApplyProfile(profile);

        Assert.IsTrue(viewModel.IsImageSource);
        Assert.AreEqual(profile.ImagePath, viewModel.PreviewPath);
        Assert.AreEqual("next.png", viewModel.FileName);
    }
}
