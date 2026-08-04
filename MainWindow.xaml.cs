using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Wallppr;

public partial class MainWindow : Window
{
    private readonly DisplayDiscovery displayDiscovery;
    private readonly WallpaperActions actions;
    private readonly AppBehaviorActions behaviorActions;
    private readonly WallpaperThumbnailCache thumbnails;
    private CancellationTokenSource thumbnailLoads = new();
    private string? startupWarning;
    private bool exitAllowed;
    private bool isLoading;

    public ObservableCollection<MonitorCardViewModel> Monitors { get; } = [];

    public event Action? SettingsRequested;

    public MainWindow(
        DisplayDiscovery displayDiscovery,
        WallpaperActions actions,
        AppBehaviorActions behaviorActions,
        WallpaperThumbnailCache thumbnails,
        string? startupWarning = null)
    {
        this.displayDiscovery = displayDiscovery;
        this.actions = actions;
        this.behaviorActions = behaviorActions;
        this.thumbnails = thumbnails;
        this.startupWarning = startupWarning;
        InitializeComponent();
        DataContext = this;
        ContentRendered += OnContentRendered;
        SourceInitialized += (_, _) => EnableDarkTitleBar();
        StateChanged += OnStateChanged;
        Closing += OnClosing;
        Closed += (_, _) => thumbnailLoads.Cancel();
    }

    public void Restore()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    public void AllowExit() => exitAllowed = true;

    private async void OnContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= OnContentRendered;
        await LoadMonitorsAsync(refresh: false);
    }

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

    private async Task LoadMonitorsAsync(bool refresh)
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        SetLoading(true);
        await Dispatcher.Yield(DispatcherPriority.Render);
        try
        {
            var monitors = await displayDiscovery.LoadAsync(refresh);
            var cards = monitors.Select(monitor =>
            {
                var viewModel = new MonitorCardViewModel(monitor);
                viewModel.ApplyProfile(actions.GetProfile(monitor.Id));
                return viewModel;
            }).ToList();

            thumbnailLoads.Cancel();
            thumbnailLoads.Dispose();
            thumbnailLoads = new CancellationTokenSource();

            Monitors.Clear();
            foreach (var card in cards)
            {
                Monitors.Add(card);
                await Dispatcher.Yield(DispatcherPriority.Background);
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

            _ = LoadThumbnailsAsync(cards, thumbnailLoads.Token);
        }
        catch (Exception exception)
        {
            if (Monitors.Count == 0) DisplayCountText.Text = "Displays unavailable";
            ShowStatus(exception.Message, error: true);
        }
        finally
        {
            SetLoading(false);
            isLoading = false;
        }
    }

    private async Task LoadThumbnailsAsync(IEnumerable<MonitorCardViewModel> cards, CancellationToken cancellationToken)
    {
        try
        {
            await Task.WhenAll(cards.Select(card => LoadThumbnailAsync(card, ensure: true, cancellationToken)));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadThumbnailAsync(MonitorCardViewModel monitor, bool ensure, CancellationToken cancellationToken)
    {
        monitor.IsThumbnailLoading = true;
        try
        {
            var profile = actions.GetProfile(monitor.Id);
            var image = await thumbnails.LoadAsync(actions.GetThumbnailPath(profile), cancellationToken);
            if (image is null && ensure)
            {
                profile = await actions.EnsureThumbnailAsync(monitor.Id, cancellationToken);
                image = await thumbnails.LoadAsync(actions.GetThumbnailPath(profile), cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            monitor.ApplyProfile(profile);
            monitor.Thumbnail = image;
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                monitor.IsThumbnailLoading = false;
            }
        }
    }

    private void SetLoading(bool loading)
    {
        RefreshButton.IsEnabled = !loading;
        LoadingOverlay.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        if (loading && Monitors.Count == 0) DisplayCountText.Text = "Loading displays…";
    }

    private async void Choose_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            await ChooseImageAsync(monitor);
        }
    }

    private async Task ChooseImageAsync(MonitorCardViewModel monitor)
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
            await RunActionAsync(monitor, token => actions.SelectImageAsync(monitor.Id, dialog.FileName, token),
                profile => $"{monitor.Name}: applied {Path.GetFileName(profile.ImagePath)}.");
        }
    }

    private async void ImageSource_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            await RunActionAsync(monitor, async token =>
            {
                actions.SetSource(monitor.Id, WallpaperSource.Image);
                return await actions.EnsureThumbnailAsync(monitor.Id, token);
            }, _ => $"{monitor.Name}: image source selected.");
        }
    }

    private async void FolderSource_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            await RunActionAsync(monitor, async token =>
            {
                actions.SetSource(monitor.Id, WallpaperSource.Folder);
                return await actions.EnsureThumbnailAsync(monitor.Id, token);
            }, _ => $"{monitor.Name}: folder source selected.");
        }
    }

    private async void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            await ChooseFolderAsync(monitor);
        }
    }

    private async Task ChooseFolderAsync(MonitorCardViewModel monitor)
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
            await RunActionAsync(monitor, token => actions.SelectFolderAsync(monitor.Id, dialog.FolderName, order, token),
                profile => $"{monitor.Name}: applied {Path.GetFileName(profile.CurrentFolderImagePath)}.");
        }
    }

    private async void Preview_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not MonitorCardViewModel monitor)
        {
            return;
        }

        if (monitor.Source == WallpaperSource.Folder)
        {
            await ChooseFolderAsync(monitor);
            return;
        }

        await ChooseImageAsync(monitor);
    }

    private async void NextFolderImage_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            await RunActionAsync(monitor, token => actions.NextAsync(monitor.Id, token),
                profile => $"{monitor.Name}: applied {Path.GetFileName(profile.CurrentFolderImagePath)}.");
        }
    }

    private async void SequentialOrder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            await RunActionAsync(monitor,
                _ => Task.FromResult(actions.SetOrder(monitor.Id, WallpaperOrder.Sequential)),
                _ => $"{monitor.Name}: sequential order selected.");
        }
    }

    private async void RandomOrder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MonitorCardViewModel monitor)
        {
            await RunActionAsync(monitor,
                _ => Task.FromResult(actions.SetOrder(monitor.Id, WallpaperOrder.Random)),
                _ => $"{monitor.Name}: random order selected.");
        }
    }

    private async Task RunActionAsync(
        MonitorCardViewModel monitor,
        Func<CancellationToken, Task<DisplayProfile>> action,
        Func<DisplayProfile, string> successMessage)
    {
        monitor.IsThumbnailLoading = true;
        monitor.Thumbnail = null;
        try
        {
            var profile = await action(thumbnailLoads.Token);
            monitor.ApplyProfile(profile);
            monitor.Thumbnail = await thumbnails.LoadAsync(actions.GetThumbnailPath(profile), thumbnailLoads.Token);
            ShowStatus(successMessage(profile), success: true);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            ShowStatus(exception.Message, error: true);
        }
        finally
        {
            monitor.IsThumbnailLoading = false;
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadMonitorsAsync(refresh: true);

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
