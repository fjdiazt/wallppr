namespace Wallppr.Tests;

[TestClass]
public sealed class MonitorCardPresentationTests
{
    [TestMethod]
    public void Applying_profile_updates_folder_presentation_state()
    {
        var viewModel = new MonitorCardViewModel(new MonitorWallpaper(0, "display-1", 0, 0, 1920, 1080, "current.jpg"));
        var profile = new DisplayProfile
        {
            DisplayId = "display-1",
            Source = WallpaperSource.Folder,
            FolderPath = @"C:\walls",
            Order = WallpaperOrder.Random,
            CurrentFolderImagePath = @"C:\walls\a.jpg"
        };

        viewModel.ApplyProfile(profile);

        Assert.IsTrue(viewModel.IsFolderSource);
        Assert.IsTrue(viewModel.IsRandomOrder);
        Assert.AreEqual(WallpaperSource.Folder, viewModel.Source);
        Assert.AreEqual("Choose wallpaper folder", viewModel.PreviewActionText);
        Assert.AreEqual(profile.CurrentFolderImagePath, viewModel.FolderPreviewPath);
        Assert.AreEqual(profile.FolderPath, viewModel.SlideshowFolderPath);
    }
}
