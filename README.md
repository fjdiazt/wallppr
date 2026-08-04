# wallppr

Focused Windows wallpaper manager. The POC applies an image or folder image immediately to one monitor through Windows `IDesktopWallpaper`.

## Run

```powershell
dotnet run --project C:\src\wallppr\Wallppr.csproj
```

## POC scope

- Dark WPF interface
- Active-monitor resolution and orientation
- Immediate per-monitor image and folder selection
- Sequential or random folder order with an immediate **Next** action
- Persisted display and global settings in `%LocalAppData%\wallppr\settings.json`
- Global Settings window for Windows startup, minimize-to-tray, and close-to-tray
- Notification icon with Open, Settings, and Exit actions
- UI and future scheduling share the same wallpaper actions
- No scheduler, installer, or separate background service yet

Monitor COM interop derives from [WallP](https://github.com/LesFerch/WallP), licensed under MIT. See [LICENSE-WallP.txt](LICENSE-WallP.txt).
