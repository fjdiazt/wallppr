# Responsive display loading design

## Goal

Show the main window immediately, avoid UI-thread display enumeration, and enumerate only on first run or manual Refresh.

## Root cause

`App.OnStartup` constructs the desktop-wallpaper COM object before showing the window. `MainWindow.Loaded` then calls `GetMonitors()` synchronously on the WPF dispatcher. COM activation and monitor enumeration therefore delay first paint and block input.

## Behavior

- Persist the last successful monitor list in the existing `%LocalAppData%\wallppr\settings.json` file.
- On startup, render cached monitor cards immediately when cache exists. Do not enumerate automatically.
- With no cache, show a centered animated loading overlay in the cards area while enumerating.
- Manual Refresh keeps existing cards visible under the loading overlay, enumerates asynchronously, then replaces cards and cache together.
- Refresh failure preserves existing cards and cache, removes the overlay, and shows the existing error banner.
- Disable Refresh while a load is active.
- Place Refresh immediately left of Settings.

## Architecture

- `DisplayDiscovery` owns cache-first loading and asynchronous refresh.
- `DesktopWallpaperService` defers its long-lived COM object until wallpaper application. Enumeration creates and releases a worker-thread COM object inside `GetMonitors()`.
- `MainWindow` owns only visual loading state and projects returned monitors into cards.
- `WallpprSettings.CachedDisplays` stores `MonitorWallpaper` records. Settings updates use record copying so behavior and wallpaper changes preserve the cache.

## Testing

- Cache-first loading never calls the platform.
- Refresh returns without blocking the caller, then persists the successful result.
- Settings JSON round-trips cached displays.
- Existing action services preserve the display cache.
- Full tests and Release build must pass.
