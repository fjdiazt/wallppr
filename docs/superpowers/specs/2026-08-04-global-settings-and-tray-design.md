# Global Settings and Tray Design

## Goal

Add an extendable home for global Wallppr behavior without cluttering the per-monitor workspace. Implement three settings now: start with Windows, minimize to tray, and close to tray. Reuse the provided `icon.png` as the product, window, and tray icon.

## User experience

Add a gear button beside **Refresh** in the main header. It opens one modeless **Settings** window; repeated clicks activate the existing window.

The initial window contains one section, **Startup & background**, with three immediate-save toggles:

- **Start Wallppr with Windows**
- **Minimize to notification area**
- **Keep Wallppr running when the window closes**

Do not build category navigation yet. When a second settings category exists, the same window can gain a left navigation rail without moving settings out of their current view.

When tray behavior is enabled, the notification icon provides **Open Wallppr**, **Settings**, and **Exit**. Double-click opens the main window. **Exit** always terminates the process; the main window close button hides it only when close-to-tray is enabled.

## Icon assets

Move the supplied root `icon.png` to `Assets/wallppr.png` as the canonical source. Generate `Assets/wallppr.ico` with standard Windows icon sizes from the same pixels. Use the ICO for executable metadata, both window icons, and the notification-area icon.

## Architecture

Extend the existing versioned JSON settings document with one global `AppBehaviorSettings` value. Display profiles remain unchanged.

`AppBehaviorActions` owns changes to global settings. It persists all three settings through the existing settings store. Enabling or disabling Windows startup also calls an `IStartupRegistration` boundary; the Windows implementation uses the current-user Run key and requires no elevation. If registration fails, the setting is not persisted and the UI returns to its previous value.

`SettingsViewModel` exposes presentation state and delegates mutations to `AppBehaviorActions`. `SettingsWindow` owns only bindings and status display.

`TrayIconService` owns the native notification icon, menu, and restore callbacks. `App` remains the composition root and owns service disposal. `MainWindow` handles minimize/close policy from current global settings; future timers continue running in the same process while the window is hidden.

Do not add a navigation framework, dependency-injection container, background service, scheduler, or installer in this change.

## Persistence and startup

Global settings save immediately to `%LocalAppData%\wallppr\settings.json` using the current atomic writer.

Startup registration uses `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, value name `wallppr`, pointing to the current executable with a quoted path. Disabling the setting removes only that value.

## Error handling

Settings-window failures appear inline and leave the last persisted value selected. Tray disposal is idempotent. Restore always shows, normalizes, and activates the main window.

## Testing

Automated tests remain non-live:

- global settings round-trip through the JSON store;
- behavior actions persist tray settings;
- startup-registration failure does not persist an incorrect setting;
- registry access uses a fake `IStartupRegistration` in tests.

Release build validation covers WPF resources and the generated icon. Final tray and window behavior remains a manual test because automated tests must not alter the real registry or desktop session.
