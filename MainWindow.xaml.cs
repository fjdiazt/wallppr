using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using Microsoft.Win32;

namespace Wallppr;

public partial class MainWindow : Window
{
    private readonly DesktopWallpaperService wallpaperService = new();

    public ObservableCollection<MonitorCardViewModel> Monitors { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += (_, _) => LoadMonitors();
        Closed += (_, _) => wallpaperService.Dispose();
        SourceInitialized += (_, _) => EnableDarkTitleBar();
    }

    private void LoadMonitors()
    {
        try
        {
            Monitors.Clear();
            foreach (var monitor in wallpaperService.GetMonitors())
            {
                Monitors.Add(new MonitorCardViewModel(monitor));
            }

            DisplayCountText.Text = $"{Monitors.Count} display{(Monitors.Count == 1 ? string.Empty : "s")} online";
            ShowStatus("Choose an image, preview it, then apply it to one display.");
        }
        catch (Exception exception)
        {
            ShowStatus(exception.Message, error: true);
        }
    }

    private void Choose_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not MonitorCardViewModel monitor)
        {
            return;
        }

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
            monitor.PendingWallpaperPath = dialog.FileName;
            ShowStatus($"{monitor.Name}: ready to apply {Path.GetFileName(dialog.FileName)}.");
        }
    }

    private void ImageSource_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            monitor.IsFolderSource = false;
            ShowStatus($"{monitor.Name}: image source selected.");
        }
    }

    private void FolderSource_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            monitor.IsFolderSource = true;
            ShowStatus($"{monitor.Name}: folder UI selected. Slideshow behavior is not implemented yet.");
        }
    }

    private void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not MonitorCardViewModel monitor)
        {
            return;
        }

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
            monitor.SlideshowFolderPath = dialog.FolderName;
            ShowStatus($"{monitor.Name}: folder selected. Slideshow behavior is not implemented yet.");
        }
    }

    private void SequentialOrder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            monitor.IsRandomOrder = false;
            ShowStatus($"{monitor.Name}: sequential order selected. UI only.");
        }
    }

    private void RandomOrder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            monitor.IsRandomOrder = true;
            ShowStatus($"{monitor.Name}: random order selected. UI only.");
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not MonitorCardViewModel monitor ||
            string.IsNullOrWhiteSpace(monitor.PendingWallpaperPath))
        {
            return;
        }

        try
        {
            wallpaperService.SetWallpaper(monitor.Id, monitor.PendingWallpaperPath);
            monitor.CurrentWallpaperPath = monitor.PendingWallpaperPath;
            monitor.PendingWallpaperPath = null;
            ShowStatus($"Applied to {monitor.Name}.", success: true);
        }
        catch (Exception exception)
        {
            ShowStatus(exception.Message, error: true);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadMonitors();

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
