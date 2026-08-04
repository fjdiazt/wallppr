using System.Runtime.InteropServices;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace Wallppr;

public partial class SettingsWindow : System.Windows.Window
{
    private readonly AppBehaviorActions actions;
    private readonly SettingsViewModel viewModel = new();

    public SettingsWindow(AppBehaviorActions actions)
    {
        this.actions = actions;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Apply(actions.Current);
        SourceInitialized += (_, _) => EnableDarkTitleBar();
    }

    private void StartWithWindows_Click(object sender, System.Windows.RoutedEventArgs e) =>
        RunAction(((ToggleButton)sender).IsChecked == true, actions.SetStartWithWindows);

    private void MinimizeToTray_Click(object sender, System.Windows.RoutedEventArgs e) =>
        RunAction(((ToggleButton)sender).IsChecked == true, actions.SetMinimizeToTray);

    private void CloseToTray_Click(object sender, System.Windows.RoutedEventArgs e) =>
        RunAction(((ToggleButton)sender).IsChecked == true, actions.SetCloseToTray);

    private void RunAction(bool enabled, Func<bool, AppBehaviorSettings> action)
    {
        try
        {
            viewModel.Apply(action(enabled));
            StatusText.Text = "Saved";
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xA7, 0xF3, 0xD0));
        }
        catch (Exception exception)
        {
            viewModel.Apply(actions.Current);
            StatusText.Text = exception.Message;
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFE, 0xCD, 0xD3));
        }
    }

    private void EnableDarkTitleBar()
    {
        var enabled = 1;
        DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref enabled, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int valueSize);
}
