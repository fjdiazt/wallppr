# wallppr

Focused Windows wallpaper manager. Current POC lists active monitors, previews one selected image per monitor, and applies it through Windows `IDesktopWallpaper`.

## Run

```powershell
dotnet run --project C:\src\wallppr\Wallppr.csproj
```

## POC scope

- Dark WPF interface
- Active-monitor resolution and orientation
- Current wallpaper preview
- Per-monitor image selection and apply
- No autorun, scheduler, random selection, or saved profiles yet

Monitor COM interop derives from [WallP](https://github.com/LesFerch/WallP), licensed under MIT. See [LICENSE-WallP.txt](LICENSE-WallP.txt).
