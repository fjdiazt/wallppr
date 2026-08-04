using System.Windows;

namespace Wallppr;

public partial class App : Application
{
    private DesktopWallpaperService? wallpaperPlatform;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        wallpaperPlatform = new DesktopWallpaperService();
        var settingsStore = new JsonSettingsStore();
        WallpprSettings settings;
        string? warning = null;

        try
        {
            settings = settingsStore.Load();
        }
        catch (Exception exception)
        {
            settings = new WallpprSettings();
            warning = $"Settings could not be loaded: {exception.Message}";
        }

        var actions = new WallpaperActions(wallpaperPlatform, settingsStore, settings);
        MainWindow = new MainWindow(wallpaperPlatform, actions, warning);
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        wallpaperPlatform?.Dispose();
        base.OnExit(e);
    }
}
