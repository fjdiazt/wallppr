using System.Windows;

namespace Wallppr;

public partial class App : System.Windows.Application
{
    private SingleInstanceCoordinator? singleInstance;
    private DesktopWallpaperService? wallpaperPlatform;
    private AppBehaviorActions? behaviorActions;
    private MainWindow? mainWindow;
    private SettingsWindow? settingsWindow;
    private TrayIconService? trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        singleInstance = new SingleInstanceCoordinator("wallppr");
        if (!singleInstance.IsPrimary)
        {
            singleInstance.SignalPrimary();
            Shutdown();
            return;
        }

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

        var settingsRepository = new SettingsRepository(settingsStore, settings);
        var thumbnails = new WallpaperThumbnailCache();
        behaviorActions = new AppBehaviorActions(settingsRepository, new WindowsStartupRegistration());
        var wallpaperActions = new WallpaperActions(wallpaperPlatform, settingsRepository, thumbnails);
        var displayDiscovery = new DisplayDiscovery(wallpaperPlatform, settingsRepository);
        mainWindow = new MainWindow(displayDiscovery, wallpaperActions, behaviorActions, thumbnails, warning);
        mainWindow.SettingsRequested += ShowSettings;
        MainWindow = mainWindow;

        trayIcon = new TrayIconService(mainWindow.Restore, ShowSettings, ExitApplication);
        behaviorActions.Changed += ApplyTrayBehavior;
        ApplyTrayBehavior(behaviorActions.Current);
        mainWindow.Show();
        singleInstance.ActivationRequested += () =>
        {
            Dispatcher.BeginInvoke(mainWindow.Restore);
        };
        singleInstance.StartListening();
    }

    private void ShowSettings()
    {
        if (settingsWindow is not null)
        {
            settingsWindow.Show();
            settingsWindow.WindowState = WindowState.Normal;
            settingsWindow.Activate();
            return;
        }

        settingsWindow = new SettingsWindow(behaviorActions!)
        {
            Owner = mainWindow?.IsVisible == true ? mainWindow : null,
            WindowStartupLocation = mainWindow?.IsVisible == true
                ? WindowStartupLocation.CenterOwner
                : WindowStartupLocation.CenterScreen
        };
        settingsWindow.Closed += (_, _) => settingsWindow = null;
        settingsWindow.Show();
    }

    private void ApplyTrayBehavior(AppBehaviorSettings behavior)
    {
        var useTray = behavior.MinimizeToTray || behavior.CloseToTray;
        if (!useTray && mainWindow?.IsVisible == false)
        {
            mainWindow.Restore();
        }

        trayIcon?.SetVisible(useTray);
    }

    private void ExitApplication()
    {
        mainWindow?.AllowExit();
        settingsWindow?.Close();
        trayIcon?.SetVisible(false);
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        singleInstance?.Dispose();
        trayIcon?.Dispose();
        wallpaperPlatform?.Dispose();
        base.OnExit(e);
    }
}
