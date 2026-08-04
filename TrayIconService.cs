using System.Drawing;
using System.Windows.Forms;

namespace Wallppr;

public sealed class TrayIconService : IDisposable
{
    private readonly Icon appIcon;
    private readonly ContextMenuStrip menu;
    private readonly NotifyIcon notifyIcon;

    public TrayIconService(Action openWallppr, Action openSettings, Action exit)
    {
        var resource = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/Assets/wallppr.ico"));
        appIcon = new Icon(resource.Stream);
        menu = new ContextMenuStrip();
        menu.Items.Add("Open Wallppr", null, (_, _) => openWallppr());
        menu.Items.Add("Settings", null, (_, _) => openSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => exit());

        notifyIcon = new NotifyIcon
        {
            Text = "wallppr",
            Icon = appIcon,
            ContextMenuStrip = menu
        };
        notifyIcon.DoubleClick += (_, _) => openWallppr();
    }

    public void SetVisible(bool visible) => notifyIcon.Visible = visible;

    public void Dispose()
    {
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        menu.Dispose();
        appIcon.Dispose();
    }
}
