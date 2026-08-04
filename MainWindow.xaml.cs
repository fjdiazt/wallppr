using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Wallppr;

public partial class MainWindow : Window
{
    private readonly IWallpaperPlatform wallpaperPlatform;
    private readonly WallpaperActions actions;
    private readonly AppBehaviorActions behaviorActions;
    private string? startupWarning;
    private bool exitAllowed;

    public ObservableCollection<MonitorCardViewModel> Monitors { get; } = [];

    public event Action? SettingsRequested;
    public MainWindow(IWallpaperPlatform wallpaperPlatform, WallpaperActions actions, AppBehaviorActions behaviorActions, string? startupWarning = null)
    {
        this.wallpaperPlatform = wallpaperPlatform;
        this.actions = actions;
        this.behaviorActions = behaviorActions;
        this.startupWarning = startupWarning;
        InitializeComponent();
        DataContext = this;
        Loaded += (_, _) => LoadMonitors();
        SourceInitialized += (_, _) => EnableDarkTitleBar();
        StateChanged += OnStateChanged;
        Closing += OnClosing;
    }


    public void Restore()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    public void AllowExit() => exitAllowed = true;

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && behaviorActions.Current.MinimizeToTray)
        {
            Hide();
            WindowState = WindowState.Normal;
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!exitAllowed && behaviorActions.Current.CloseToTray)
        {
            e.Cancel = true;
            Hide();
        }
    }
    private void LoadMonitors()
    {
        try
        {
            Monitors.Clear();
            foreach (var monitor in wallpaperPlatform.GetMonitors())
            {
                var viewModel = new MonitorCardViewModel(monitor);
                viewModel.ApplyProfile(actions.GetProfile(monitor.Id));
                Monitors.Add(viewModel);
            }

            DisplayCountText.Text = $"{Monitors.Count} display{(Monitors.Count == 1 ? string.Empty : "s")} online";
            if (startupWarning is not null)
            {
                ShowStatus(startupWarning, error: true);
                startupWarning = null;
            }
            else
            {
                ShowStatus("Choose an image or folder to apply it immediately.");
            }
        }
        catch (Exception exception)
        {
            ShowStatus(exception.Message, error: true);
        }
    }

    private void Choose_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            ChooseImage(monitor);
        }
    }

    private void ChooseImage(MonitorCardViewModel monitor)
    {
        var dialog = new OpenFileDialog
        {
            Title = $"Wallpaper for {monitor.Name}",
            Filter = "Images|*.bmp;*.gif;*.jpeg;*.jpg;*.png;*.tif;*.tiff;*.webp|All files|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (File.Exists(monitor.PreviewPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(monitor.PreviewPath);
        }

        if (dialog.ShowDialog(this) == true)
        {
            RunAction(monitor, () => actions.SelectImage(monitor.Id, dialog.FileName),
                profile => $"{monitor.Name}: applied {Path.GetFileName(profile.ImagePath)}.");
        }
    }

    private void ImageSource_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            RunAction(monitor, () => actions.SetSource(monitor.Id, WallpaperSource.Image),
                _ => $"{monitor.Name}: image source selected.");
        }
    }

    private void FolderSource_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            RunAction(monitor, () => actions.SetSource(monitor.Id, WallpaperSource.Folder),
                _ => $"{monitor.Name}: folder source selected.");
        }
    }

    private void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            ChooseFolder(monitor);
        }
    }

    private void ChooseFolder(MonitorCardViewModel monitor)
    {
        var dialog = new OpenFolderDialog
        {
            Title = $"Slideshow folder for {monitor.Name}",
            Multiselect = false
        };

        if (Directory.Exists(monitor.SlideshowFolderPath))
        {
            dialog.InitialDirectory = monitor.SlideshowFolderPath;
        }

        if (dialog.ShowDialog(this) == true)
        {
            var order = monitor.IsRandomOrder ? WallpaperOrder.Random : WallpaperOrder.Sequential;
            RunAction(monitor, () => actions.SelectFolder(monitor.Id, dialog.FolderName, order),
                profile => $"{monitor.Name}: applied {Path.GetFileName(profile.CurrentFolderImagePath)}.");
        }
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not MonitorCardViewModel monitor)
        {
            return;
        }

        if (monitor.Source == WallpaperSource.Folder)
        {
            ChooseFolder(monitor);
            return;
        }

        ChooseImage(monitor);
    }

    private void NextFolderImage_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            RunAction(monitor, () => actions.Next(monitor.Id),
                profile => $"{monitor.Name}: applied {Path.GetFileName(profile.CurrentFolderImagePath)}.");
        }
    }

    private void SequentialOrder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            RunAction(monitor, () => actions.SetOrder(monitor.Id, WallpaperOrder.Sequential),
                _ => $"{monitor.Name}: sequential order selected.");
        }
    }

    private void RandomOrder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            RunAction(monitor, () => actions.SetOrder(monitor.Id, WallpaperOrder.Random),
                _ => $"{monitor.Name}: random order selected.");
        }
    }

    private void RunAction(MonitorCardViewModel monitor, Func<DisplayProfile> action, Func<DisplayProfile, string> successMessage)
    {
        try
        {
            var profile = action();
            monitor.ApplyProfile(profile);
            ShowStatus(successMessage(profile), success: true);
        }
        catch (Exception exception)
        {
            ShowStatus(exception.Message, error: true);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadMonitors();

    private void Settings_Click(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke();

    private void ShowStatus(string message, bool error = false, bool success = false)
    {
        StatusText.Text = message;
        StatusText.Foreground = Brush(error ? "#FECDD3" : success ? "#A7F3D0" : "#C7D2FE");
        StatusBanner.Background = Brush(error ? "#351923" : success ? "#123027" : "#172033");
        StatusBanner.BorderBrush = Brush(error ? "#7F3041" : success ? "#25634F" : "#2D3E62");
        StatusBanner.Visibility = Visibility.Visible;
    }

    private void EnableDarkTitleBar()
    {
        var enabled = 1;
        DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref enabled, sizeof(int));
        SetWindowColor(34, "#283044");
        SetWindowColor(35, "#111520");
        SetWindowColor(36, "#F5F7FC");
    }

    private void SetWindowColor(int attribute, string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        var colorRef = color.R | color.G << 8 | color.B << 16;
        DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, attribute, ref colorRef, sizeof(int));
    }

    private static SolidColorBrush Brush(string color) =>
        new((Color)ColorConverter.ConvertFromString(color));

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int valueSize);
}
