# Immediate wallpaper actions architecture

## Goal

Selecting an image, selecting a folder, or pressing `Next` changes that display's real wallpaper immediately. Settings persist across restarts. UI and future background timers reuse the same action layer.

## POC scope

Included:

- Immediate per-display wallpaper application for image selection.
- Immediate per-display wallpaper application for folder selection.
- Immediate per-display wallpaper application for folder `Next`.
- Sequential and random folder order.
- Durable per-display settings.
- Pure non-live tests; tests never change a real display.

Excluded:

- Timers and scheduling.
- Tray behavior or hidden background lifetime.
- Autorun and startup registration.
- A separate Windows Service or process.

## Runtime architecture

`wallppr.exe` remains the only process. `App` is the composition root and owns one application-level instance of each dependency:

- `DesktopWallpaperService`: Windows `IDesktopWallpaper` adapter.
- `JsonSettingsStore`: durable settings adapter.
- `WallpaperActions`: reusable application service.
- `MainWindow`: UI adapter that calls `WallpaperActions` and renders returned state.

Future `DisplayScheduler` will live in the same process and call `WallpaperActions.Next(displayId)`. It will not duplicate folder selection or wallpaper application logic.

## Application service

`WallpaperActions` exposes:

- `SelectImage(displayId, imagePath)`
- `SelectFolder(displayId, folderPath, order)`
- `Next(displayId)`
- `SetOrder(displayId, order)`
- `GetProfile(displayId)`

Each wallpaper-changing action follows one path:

1. Validate display and source.
2. Resolve the candidate image.
3. Call the wallpaper platform adapter.
4. Update profile state and `LastAppliedUtc`.
5. Persist settings.
6. Return the updated profile for UI refresh.

`SetOrder` persists order without changing wallpaper. Switching UI tabs alone does not apply a wallpaper; choosing an image/folder and pressing `Next` do.

If the Windows wallpaper call fails, profile state is not changed or persisted. If wallpaper application succeeds but settings persistence fails, the action reports that the wallpaper changed but settings were not saved.

## Folder selection

- Scan selected folder only; ignore subfolders.
- Support `.bmp`, `.gif`, `.jpeg`, `.jpg`, `.png`, `.tif`, `.tiff`, and `.webp`.
- Sequential selection sorts paths case-insensitively and wraps.
- Random selection avoids immediate repetition when more than one image exists.
- Refresh folder contents for each folder selection and `Next`, so added and removed images are respected.
- Missing, inaccessible, or empty folders fail without changing wallpaper or saved state.

Random selection uses an injected `Random`, defaulting to `Random.Shared`, so tests can use a seed.

## Persistent settings

Use JSON at `%LocalAppData%\wallppr\settings.json`, not the registry. Per-display configuration is structured and expected to grow; JSON remains inspectable, versionable, and atomic to replace.

Schema version `1` contains a dictionary keyed by the stable monitor device path returned by `IDesktopWallpaper`.

Each display profile stores:

- Source: image or folder.
- Image path.
- Folder path.
- Folder order: sequential or random.
- Current folder image path.
- `LastAppliedUtc`.

Settings for disconnected displays remain stored. Startup loads profiles but does not reapply wallpaper. Windows retains the current wallpaper; the loaded profile drives previews and future actions.

Writes use a temporary sibling file followed by replacement. Failed writes never truncate the last valid settings file. Missing or malformed settings load as defaults and surface a recoverable warning.

## UI integration

`MainWindow` no longer owns wallpaper behavior. Handlers call `WallpaperActions`, then copy returned profile state into the monitor card and show success or error status.

- Choosing image calls `SelectImage`; file applies immediately. Separate image `Apply` button is removed.
- Choosing folder calls `SelectFolder`; resolved image applies immediately.
- `Next` calls `Next`; resolved image applies immediately.
- Order toggle calls `SetOrder`; only order changes.

`MonitorCardViewModel` becomes presentation state. It does not enumerate folders, select random images, persist settings, or call Windows COM.

## Future timer integration

Every successful wallpaper-changing action updates `LastAppliedUtc`. A future per-display scheduler can compute due time from the persisted interval and this timestamp. Manual image selection, folder selection, and `Next` therefore reset that display's countdown automatically.

Timer callbacks call the same `Next(displayId)` method. App lifecycle, tray behavior, resume handling, and interval UI remain separate future work.

## Non-live TDD

Tests use:

- Fake wallpaper platform recording display and image calls.
- In-memory settings store for action tests.
- Temporary folders for real file enumeration.
- Seeded `Random` for deterministic random selection.
- Temporary settings path for JSON round-trip and malformed-file tests.

Required behavior tests:

- Image selection applies once and persists returned profile.
- Folder selection applies first sequential image.
- Folder selection applies deterministic random image.
- Sequential `Next` wraps.
- Random `Next` avoids immediate repetition.
- Order change persists without applying wallpaper.
- Platform failure leaves saved profile unchanged.
- JSON settings round-trip preserves every profile field.

No automated test invokes `IDesktopWallpaper` or changes a live wallpaper.
