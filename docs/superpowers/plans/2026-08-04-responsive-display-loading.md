# Responsive Display Loading Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Render cached displays immediately and keep WPF responsive during first-run or manual display enumeration.

**Architecture:** Add a small `DisplayDiscovery` cache/refresh coordinator. Run COM enumeration on a worker with an object created on that worker; keep loading visuals in `MainWindow`.

**Tech Stack:** .NET 10, WPF, Windows `IDesktopWallpaper`, System.Text.Json, MSTest

## Global Constraints

- Enumerate only when cache is empty or Refresh is clicked.
- Preserve cards and cache when refresh fails.
- Add no dependency or background service.
- Leave implementation uncommitted and unpushed for manual testing.

---

### Task 1: Durable cache and coordinator

**Files:**
- Create: `DisplayDiscovery.cs`
- Modify: `WallpaperSettings.cs`
- Modify: `WallpaperActions.cs`
- Modify: `AppBehaviorActions.cs`
- Modify: `tests/Wallppr.Tests/JsonSettingsStoreTests.cs`
- Modify: `tests/Wallppr.Tests/AppBehaviorActionsTests.cs`
- Create: `tests/Wallppr.Tests/DisplayDiscoveryTests.cs`

**Interfaces:**
- Produces: `Task<IReadOnlyList<MonitorWallpaper>> DisplayDiscovery.LoadAsync(bool refresh)`
- Persists: `WallpprSettings.CachedDisplays`

- [ ] **Step 1: Write failing cache tests**

Add tests proving cached startup skips platform enumeration, refresh returns while blocked enumeration runs, successful refresh persists results, JSON round-trips cache, and other actions preserve cache.

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/Wallppr.Tests/Wallppr.Tests.csproj --filter "DisplayDiscovery|Save_and_load|Action_services"`

Expected: compile failure because `DisplayDiscovery` and `CachedDisplays` do not exist.

- [ ] **Step 3: Implement minimal cache flow**

```csharp
public Task<IReadOnlyList<MonitorWallpaper>> LoadAsync(bool refresh) =>
    !refresh && settings.Current.CachedDisplays.Count > 0
        ? Task.FromResult<IReadOnlyList<MonitorWallpaper>>(settings.Current.CachedDisplays)
        : RefreshAsync();
```

`RefreshAsync` runs `platform.GetMonitors` with `Task.Run`, saves the successful list through record copying, and returns it.

- [ ] **Step 4: Verify GREEN**

Run the focused filter again. Expected: all selected tests pass.

### Task 2: Worker-safe COM enumeration

**Files:**
- Modify: `DesktopWallpaperService.cs`

**Interfaces:**
- Consumes: existing `IWallpaperPlatform.GetMonitors()`
- Produces: worker-local COM activation for enumeration; lazy UI-thread COM activation for wallpaper changes

- [ ] **Step 1: Defer COM activation**

Replace the eager field initializer with a lazy property used only by `SetWallpaper`.

- [ ] **Step 2: Isolate enumeration COM lifetime**

Create `DesktopWallpaperClass` inside `GetMonitors`, call the existing `EnumerateMonitors`, and release it in `finally`.

- [ ] **Step 3: Verify enumeration tests**

Run: `dotnet test tests/Wallppr.Tests/Wallppr.Tests.csproj --filter MonitorEnumerationTests`

Expected: two tests pass.

### Task 3: Responsive loading UI

**Files:**
- Modify: `App.xaml.cs`
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `DisplayDiscovery.LoadAsync(bool refresh)`
- Produces: async startup/refresh handlers and `LoadingOverlay`

- [ ] **Step 1: Inject discovery**

Construct `DisplayDiscovery` in `App` and pass it to `MainWindow`.

- [ ] **Step 2: Add loading overlay**

Wrap the cards `ScrollViewer` in a row-two `Grid`; overlay a translucent surface containing a rotating Segoe Fluent Icons refresh glyph and `Loading displays…`.

- [ ] **Step 3: Make load asynchronous**

Use async `Loaded` and Refresh handlers. Disable Refresh while loading, retain cards until a successful result, and always hide the overlay in `finally`.

- [ ] **Step 4: Reorder header buttons**

Place Refresh immediately left of Settings.

- [ ] **Step 5: Verify all behavior**

Run: `dotnet test tests/Wallppr.Tests/Wallppr.Tests.csproj`

Run: `dotnet build Wallppr.csproj -c Release --no-restore`

Expected: all tests pass; build has zero errors and warnings.
