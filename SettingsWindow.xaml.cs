using System.Runtime.InteropServices;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace Wallppr;

public partial class SettingsWindow : System.Windows.Window
{
    private readonly AppBehaviorActions actions;
    private readonly SlideshowTimer slideshowTimer;
    private readonly SettingsViewModel viewModel = new();

    public SettingsWindow(AppBehaviorActions actions, SlideshowTimer slideshowTimer)
    {
        this.actions = actions;
        this.slideshowTimer = slideshowTimer;
        InitializeComponent();
        DataContext = viewModel;
        ApplyCurrent();
        SourceInitialized += (_, _) => EnableDarkTitleBar();
    }

    private void StartWithWindows_Click(object sender, System.Windows.RoutedEventArgs e) =>
        RunAction(((ToggleButton)sender).IsChecked == true, actions.SetStartWithWindows);

    private void MinimizeToTray_Click(object sender, System.Windows.RoutedEventArgs e) =>
        RunAction(((ToggleButton)sender).IsChecked == true, actions.SetMinimizeToTray);

    private void CloseToTray_Click(object sender, System.Windows.RoutedEventArgs e) =>
        RunAction(((ToggleButton)sender).IsChecked == true, actions.SetCloseToTray);

    private void IntervalSeconds_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e) =>
        SaveInterval();

    private void IntervalSeconds_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter)
        {
            return;
        }

        SaveInterval();
        e.Handled = true;
    }

    private void SaveInterval()
    {
        if (!int.TryParse(IntervalSecondsTextBox.Text, out var seconds) || seconds < 0)
        {
            ApplyCurrent();
            ShowError("Enter zero or a positive whole number.");
            return;
        }

        if (seconds == slideshowTimer.IntervalSeconds)
        {
            return;
        }

        try
        {
            slideshowTimer.SetIntervalSeconds(seconds);
            ApplyCurrent();
            ShowSaved();
        }
        catch (Exception exception)
        {
            ApplyCurrent();
            ShowError(exception.Message);
        }
    }

    private void RunAction(bool enabled, Func<bool, AppBehaviorSettings> action)
    {
        try
        {
            action(enabled);
            ApplyCurrent();
            ShowSaved();
        }
        catch (Exception exception)
        {
            ApplyCurrent();
            ShowError(exception.Message);
        }
    }

    private void ApplyCurrent() =>
        viewModel.Apply(actions.Current, slideshowTimer.IntervalSeconds);

    private void ShowSaved()
    {
        StatusText.Text = "Saved";
        StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xA7, 0xF3, 0xD0));
    }

    private void ShowError(string message)
    {
        StatusText.Text = message;
        StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFE, 0xCD, 0xD3));
    }

    private void EnableDarkTitleBar()
    {
        var enabled = 1;
        DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref enabled, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int valueSize);
}
