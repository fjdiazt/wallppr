# Global Slideshow Timer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add one persisted seconds interval that advances every Folder display through the existing wallpaper actions while Wallppr is running.

**Architecture:** `SlideshowTimer` owns one native WPF `DispatcherTimer`, persists only the configured interval, and performs one sequential sweep through current Folder profiles on expiry. `App` owns its lifetime; Settings changes its interval; MainWindow resets it after successful manual Folder changes and refreshes cards from completion results.

**Tech Stack:** .NET 10, C#, WPF `DispatcherTimer`, existing JSON settings repository, MSTest 4.

## Global Constraints

- One global interval in whole seconds; `0` disables it.
- Persist only the interval; restart starts a fresh full interval.
- No per-second polling or timer-state writes.
- Reuse `WallpaperActions.NextAsync`; preserve per-display Sequential or Random order.
- Folder selection and manual Next reset the timer only after success.
- One display failure must not block other Folder displays.
- No service, scheduled task, per-display intervals, countdown, or catch-up.
- Use non-live TDD; desktop wallpaper behavior remains manual verification.

---

### Task 1: Persist the global interval

**Files:**
- Modify: `WallpaperSettings.cs`
- Modify: `tests/Wallppr.Tests/JsonSettingsStoreTests.cs`

**Interfaces:**
- Produces: `SlideshowSettings.IntervalSeconds` and `WallpprSettings.Slideshow`.

- [ ] **Step 1: Write the failing JSON round-trip test**

Add a test that saves `new WallpprSettings { Slideshow = new() { IntervalSeconds = 45 } }`, reloads it, and asserts literal value `45`.

- [ ] **Step 2: Run the focused test and verify RED**

Run: `dotnet test Wallppr.slnx --filter FullyQualifiedName~JsonSettingsStoreTests`

Expected: compile failure because `Slideshow` and `IntervalSeconds` do not exist.

- [ ] **Step 3: Add the minimal settings records**

```csharp
public sealed record SlideshowSettings
{
    public int IntervalSeconds { get; init; }
}

public sealed record WallpprSettings
{
    // existing members
    public SlideshowSettings Slideshow { get; init; } = new();
}
```

- [ ] **Step 4: Run the focused test and verify GREEN**

Run: `dotnet test Wallppr.slnx --filter FullyQualifiedName~JsonSettingsStoreTests`

Expected: all filtered tests pass.

### Task 2: Add the one-shot app timer

**Files:**
- Create: `SlideshowTimer.cs`
- Create: `tests/Wallppr.Tests/SlideshowTimerTests.cs`

**Interfaces:**
- Consumes: `SettingsRepository`, `WallpaperActions`, `WallpprSettings.Slideshow`.
- Produces: `SlideshowTimer.Start()`, `Reset()`, `SetIntervalSeconds(int)`, `AdvanceFolderDisplaysAsync()`, `Completed`, and `Dispose()`.
- Produces: `SlideshowRunResult(IReadOnlyList<DisplayProfile> Changed, IReadOnlyList<string> Errors)`.

- [ ] **Step 1: Write failing scheduling tests**

Using a real injected `DispatcherTimer` and in-memory settings store, cover:

```csharp
scheduler.Start();
Assert.IsFalse(dispatcherTimer.IsEnabled); // interval 0

scheduler.SetIntervalSeconds(30);
Assert.IsTrue(dispatcherTimer.IsEnabled);
Assert.AreEqual(TimeSpan.FromSeconds(30), dispatcherTimer.Interval);
Assert.AreEqual(1, store.SaveCount);

scheduler.Reset();
Assert.AreEqual(1, store.SaveCount); // reset never writes
```

Also assert a negative interval throws `ArgumentOutOfRangeException` and leaves the previous value active.

- [ ] **Step 2: Run focused tests and verify RED**

Run: `dotnet test Wallppr.slnx --filter FullyQualifiedName~SlideshowTimerTests`

Expected: compile failure because `SlideshowTimer` does not exist.

- [ ] **Step 3: Implement minimal scheduling behavior**

Create one concrete `IDisposable` class. Attach one `DispatcherTimer.Tick` handler. `Start` marks the timer active and calls `Reset`. `Reset` stops the timer, reads the in-memory interval, and starts it only when greater than zero. `SetIntervalSeconds` validates non-negative input, saves one updated settings snapshot, and resets. No interface or polling loop.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run: `dotnet test Wallppr.slnx --filter FullyQualifiedName~SlideshowTimerTests`

Expected: scheduling tests pass.

- [ ] **Step 5: Write failing sweep tests**

Create real temporary image folders and use the existing `WallpaperActions` with a fake `IWallpaperPlatform`. Cover:

- two Folder profiles each advance once;
- one Image profile is ignored;
- a platform failure for the first Folder profile appears in `Errors` and the later Folder profile still changes;
- `Completed` receives the same changed profiles returned by `AdvanceFolderDisplaysAsync`.

- [ ] **Step 6: Run focused tests and verify RED**

Run: `dotnet test Wallppr.slnx --filter FullyQualifiedName~SlideshowTimerTests`

Expected: tests fail because sweep behavior and completion results are absent.

- [ ] **Step 7: Implement the minimal sweep**

On expiry or direct `AdvanceFolderDisplaysAsync`:

```csharp
timer.Stop();
var folderIds = settings.Current.Displays.Values
    .Where(profile => profile.Source == WallpaperSource.Folder)
    .Select(profile => profile.DisplayId)
    .ToArray();
```

Await `WallpaperActions.NextAsync` for each ID in order. Collect profiles and per-display error messages independently. Raise `Completed` once. Restart one full interval in `finally`, preventing overlap even after failure.

- [ ] **Step 8: Run focused and full tests**

Run: `dotnet test Wallppr.slnx --filter FullyQualifiedName~SlideshowTimerTests`

Expected: all timer tests pass.

Run: `dotnet test Wallppr.slnx`

Expected: all tests pass.

### Task 3: Wire Settings, MainWindow, and app lifetime

**Files:**
- Modify: `SettingsViewModel.cs`
- Modify: `tests/Wallppr.Tests/SettingsViewModelTests.cs`
- Modify: `SettingsWindow.xaml`
- Modify: `SettingsWindow.xaml.cs`
- Modify: `MainWindow.xaml.cs`
- Modify: `App.xaml.cs`

**Interfaces:**
- Consumes: `SlideshowTimer.IntervalSeconds`, `SetIntervalSeconds`, `Reset`, and `Completed`.
- Produces: Settings-window whole-seconds input and automatic card refresh.

- [ ] **Step 1: Write the failing view-model test**

Change the existing `Apply` test to apply behavior plus literal interval `45`, then assert all toggles and `IntervalSeconds == 45`.

- [ ] **Step 2: Run focused test and verify RED**

Run: `dotnet test Wallppr.slnx --filter FullyQualifiedName~SettingsViewModelTests`

Expected: compile failure because the interval property/apply argument is absent.

- [ ] **Step 3: Add the minimal view-model property**

Add `int IntervalSeconds { get; private set; }`, accept the interval in `Apply`, and notify the property beside the existing toggles.

- [ ] **Step 4: Run focused test and verify GREEN**

Run: `dotnet test Wallppr.slnx --filter FullyQualifiedName~SettingsViewModelTests`

Expected: focused test passes.

- [ ] **Step 5: Add the Settings-window input**

Add a Slideshow card below Startup & Background. Use a dark native `TextBox` bound one-way to `IntervalSeconds`, labeled `Change every` with suffix `seconds` and helper text `0 disables automatic changes.` Commit on Enter or lost keyboard focus. Parse with `int.TryParse`; on invalid/negative input restore the current value and show `Enter zero or a positive whole number.` Existing status colors and immediate-save behavior remain.

- [ ] **Step 6: Wire manual resets and automatic card updates**

Inject `SlideshowTimer` into `MainWindow`. Extend `RunActionAsync` with `bool resetSlideshow = false`; call `Reset()` only after a successful action when true. Pass true only from folder selection and manual Next.

Subscribe to `Completed`. For each changed profile with a loaded card, apply the profile and load its persisted thumbnail. Report aggregated errors without blocking successful card updates.

- [ ] **Step 7: Own timer lifetime in App**

Create `SlideshowTimer` after `WallpaperActions`, pass it to both windows, call `Start()` after the main window is shown, and dispose it in `OnExit`.

- [ ] **Step 8: Run full verification**

Run: `dotnet test Wallppr.slnx`

Expected: all tests pass with zero failures.

Run: `dotnet build Wallppr.slnx --configuration Debug --no-restore`

Expected: build exits 0 with zero errors.

Run: `git diff --check`

Expected: no whitespace errors.
