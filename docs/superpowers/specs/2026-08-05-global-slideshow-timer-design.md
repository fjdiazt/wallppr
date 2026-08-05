# Global Slideshow Timer Design

## Goal

Add one global interval, entered in seconds, that advances every display using a folder source. Keep the timer inside the existing Wallppr process and reuse the existing wallpaper actions.

## Scope

- One global interval for all displays.
- `0` disables automatic changes.
- Sequential and random behavior remain per display.
- Folder selection and manual Next restart the global timer.
- Restarting Wallppr starts a fresh interval.
- No service, scheduled task, per-display interval, countdown, or catch-up behavior.

## Settings and UI

Persist only `IntervalSeconds` in the existing JSON settings file. The active deadline stays in memory. Wallppr never writes timer state every second.

Add a Slideshow section to the existing Settings window with a whole-number seconds field. Save a valid value immediately when the user presses Enter or moves focus away. Reject negative or non-numeric input without replacing the last valid setting. Show `0 disables` beside the field.

## Runtime Design

Add one concrete app-owned slideshow timer. Do not add an interface or background service.

The timer uses WPF's native `DispatcherTimer` as a one-shot schedule:

1. Start it with the configured interval when Wallppr starts.
2. Stop it before processing an expiry.
3. Read the current display profiles.
4. Call the existing `WallpaperActions.NextAsync(displayId)` for each profile whose source is Folder.
5. Continue when one display fails so other displays still advance.
6. Refresh changed cards and thumbnails.
7. Start one fresh interval after processing finishes.

Stopping and restarting the timer prevents overlapping runs. It also avoids per-second polling. The WPF dispatcher remains active while the main window is minimized or hidden in the notification area.

Successful manual folder selection and manual Next restart the same in-memory timer. Image selection, source/order toggles, display refresh, and thumbnail loading do not restart it.

## Data Flow

`App` creates the timer beside `WallpaperActions` and owns its lifetime. `SettingsWindow` changes the interval through the timer. `MainWindow` resets it after successful manual folder actions and receives completed automatic results to update only affected cards.

The timer invokes `WallpaperActions.NextAsync`; it does not duplicate folder enumeration, random selection, wallpaper application, persistence, or thumbnail generation.

## Error Handling

- Invalid interval input leaves the prior interval active and reports the validation error in Settings.
- A missing or empty folder fails only that display during an automatic run.
- Other eligible displays continue.
- The next global interval always starts after the run, even when every display fails.
- Existing wallpaper and settings-save errors remain unchanged.

## Testing

Use non-live TDD with fake wallpaper and settings dependencies. Cover:

- `0` leaves the timer disabled.
- A positive interval schedules one expiry without polling.
- Changing the interval and successful manual folder actions restart the schedule.
- Expiry advances every current Folder profile once through existing actions.
- Image profiles are ignored.
- One display failure does not block later displays.
- Automatic card updates use the returned profiles.
- Restart creates a fresh full interval because no deadline is persisted.

Live desktop wallpaper behavior remains a manual test.
