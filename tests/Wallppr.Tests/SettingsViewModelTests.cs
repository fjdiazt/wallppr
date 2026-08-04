namespace Wallppr.Tests;

[TestClass]
public sealed class SettingsViewModelTests
{
    [TestMethod]
    public void Apply_updates_all_behavior_toggles()
    {
        var viewModel = new SettingsViewModel();

        viewModel.Apply(new AppBehaviorSettings
        {
            StartWithWindows = true,
            MinimizeToTray = true,
            CloseToTray = true
        });

        Assert.IsTrue(viewModel.StartWithWindows);
        Assert.IsTrue(viewModel.MinimizeToTray);
        Assert.IsTrue(viewModel.CloseToTray);
    }
}
