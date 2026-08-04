# Immediate Wallpaper Actions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply selected image/folder wallpapers immediately, persist per-display configuration, and expose reusable actions for future in-process timers.

**Architecture:** `WallpaperActions` owns wallpaper-changing use cases. WPF delegates to it; `IWallpaperPlatform` and `ISettingsStore` isolate live Windows and persistence from pure tests.

**Tech Stack:** .NET 10, WPF, `IDesktopWallpaper`, `System.Text.Json`, MSTest

## Global Constraints

- One `wallppr.exe` process; no Windows Service.
- No timer, tray, autorun, or background lifecycle in this POC.
- Image selection, folder selection, and `Next` apply immediately.
- Tests never call `IDesktopWallpaper` or change a live display.
- Settings path: `%LocalAppData%\wallppr\settings.json`.
- No implementation commit or push before manual testing.

---

### Task 1: Persistent profile model and JSON store

**Files:**
- Create: `WallpaperSettings.cs`
- Create: `ISettingsStore.cs`
- Create: `JsonSettingsStore.cs`
- Create: `tests/Wallppr.Tests/JsonSettingsStoreTests.cs`

**Interfaces:**
- Produces: `WallpaperSource`, `WallpaperOrder`, `DisplayProfile`, `WallpprSettings`, `ISettingsStore.Load()`, `ISettingsStore.Save(settings)`.

- [ ] **Step 1: Write failing JSON round-trip test**

```csharp
var expected = new DisplayProfile("display-1", WallpaperSource.Folder, null, @"C:\walls", WallpaperOrder.Random, @"C:\walls\a.jpg", timestamp);
store.Save(new WallpprSettings { Displays = { [expected.DisplayId] = expected } });
Assert.AreEqual(expected, store.Load().Displays[expected.DisplayId]);
```

- [ ] **Step 2: Verify RED**

Run `dotnet test tests/Wallppr.Tests/Wallppr.Tests.csproj -c Release`; expect missing settings types.

- [ ] **Step 3: Implement models and atomic JSON store**

```csharp
public interface ISettingsStore
{
    WallpprSettings Load();
    void Save(WallpprSettings settings);
}

public sealed record DisplayProfile(
    string DisplayId,
    WallpaperSource Source = WallpaperSource.Image,
    string? ImagePath = null,
    string? FolderPath = null,
    WallpaperOrder Order = WallpaperOrder.Sequential,
    string? CurrentFolderImagePath = null,
    DateTimeOffset? LastAppliedUtc = null);
```

Serialize to a sibling `.tmp`, then `File.Move(temp, path, overwrite: true)`.

- [ ] **Step 4: Verify GREEN**

Run focused tests; expect pass.

### Task 2: Reusable immediate actions

**Files:**
- Create: `IWallpaperPlatform.cs`
- Create: `WallpaperActions.cs`
- Create: `tests/Wallppr.Tests/WallpaperActionsTests.cs`
- Modify: `DesktopWallpaperService.cs`

**Interfaces:**
- Produces: `SelectImage`, `SelectFolder`, `Next`, `SetOrder`, `GetProfile`.
- Consumes: `IWallpaperPlatform`, `ISettingsStore`, `WallpprSettings`, seeded `Random`, clock delegate.

- [ ] **Step 1: Write failing image-action test**

```csharp
var profile = actions.SelectImage("display-1", imagePath);
Assert.AreEqual(("display-1", imagePath), platform.LastSet);
Assert.AreEqual(imagePath, profile.ImagePath);
Assert.AreEqual(profile, store.Settings.Displays["display-1"]);
```

- [ ] **Step 2: Verify RED**

Run focused tests; expect missing `WallpaperActions`.

- [ ] **Step 3: Implement image action and shared persistence path**

Apply through platform first. Save copied settings second. Update in-memory settings only after save succeeds.

- [ ] **Step 4: Add failing folder and Next tests**

Use temporary folder with `a.jpg`, `b.png`, and ignored file. Assert first sequential image, wrap, deterministic random, no-repeat, and refreshed folder contents.

- [ ] **Step 5: Implement folder actions**

Enumerate supported extensions non-recursively. Use case-insensitive sort. Random next uses a non-zero offset when more than one image exists.

- [ ] **Step 6: Add order and failure tests**

Assert `SetOrder` persists without platform call. Assert platform exception leaves settings store unchanged.

- [ ] **Step 7: Verify GREEN**

Run full action tests; expect pass.

### Task 3: WPF composition and immediate UI behavior

**Files:**
- Modify: `App.xaml`
- Modify: `App.xaml.cs`
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`
- Modify: `MonitorCardViewModel.cs`
- Modify: `tests/Wallppr.Tests/MonitorCardViewModelTests.cs`

**Interfaces:**
- Consumes: `WallpaperActions`, `IWallpaperPlatform`, `DisplayProfile`.
- Produces: UI delegates every wallpaper change to reusable actions.

- [ ] **Step 1: Write failing presentation-state test**

```csharp
viewModel.ApplyProfile(profile);
Assert.AreEqual(profile.CurrentFolderImagePath, viewModel.FolderPreviewPath);
Assert.IsTrue(viewModel.IsFolderSource);
```

- [ ] **Step 2: Verify RED**

Run focused view-model tests; expect missing `ApplyProfile`.

- [ ] **Step 3: Convert view-model to presentation state**

Remove folder enumeration/random selection. `ApplyProfile` updates source, paths, order, and preview notifications.

- [ ] **Step 4: Compose dependencies in App**

Remove `StartupUri`. `App.OnStartup` constructs platform, settings store, loaded settings, actions, and `MainWindow`; `OnExit` disposes platform.

- [ ] **Step 5: Route UI actions**

- Choose image → `SelectImage`, then `ApplyProfile`.
- Choose folder → `SelectFolder`, then `ApplyProfile`.
- Next → `Next`, then `ApplyProfile`.
- Order toggle → `SetOrder`, then `ApplyProfile`.
- Remove image `Apply` button.

- [ ] **Step 6: Verify full solution**

Run `dotnet test Wallppr.slnx -c Release`; expect all tests pass and zero build errors.

### Task 4: Manual-test handoff

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Update POC scope and settings path**

Document immediate behavior, persistence, and excluded background/timer features.

- [ ] **Step 2: Build Release**

Run `dotnet build Wallppr.slnx -c Release`.

- [ ] **Step 3: Stop before live automation, commit, or push**

Provide executable path and manual test checklist. User performs live wallpaper validation.
