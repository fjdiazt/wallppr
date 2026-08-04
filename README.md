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
- Persisted per-display settings in `%LocalAppData%\wallppr\settings.json`
- UI and future scheduling share the same wallpaper actions
- No autorun, scheduler, tray behavior, or background mode yet

Monitor COM interop derives from [WallP](https://github.com/LesFerch/WallP), licensed under MIT. See [LICENSE-WallP.txt](LICENSE-WallP.txt).
