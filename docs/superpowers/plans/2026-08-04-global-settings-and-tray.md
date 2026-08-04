# Global Settings and Tray Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an extendable modeless settings window, working Windows startup and tray behaviors, and consistent Wallppr branding from the provided icon.

**Architecture:** One shared `SettingsRepository` prevents independent action services from overwriting each other's JSON state. `AppBehaviorActions` owns global-setting mutations and the startup-registration boundary; WPF windows remain presentation-only. `App` owns the singleton settings window and concrete tray service.

**Tech Stack:** .NET 10, WPF, Windows Forms `NotifyIcon`, HKCU Run registry key, System.Text.Json, MSTest, Pillow for one-time ICO conversion.

## Global Constraints

- Keep one process; add no Windows service or helper process.
- Persist global settings in `%LocalAppData%\wallppr\settings.json` through the existing atomic JSON store.
- Do not add a navigation framework, dependency-injection container, scheduler, installer, or third-party runtime package.
- Automated tests must not touch the real registry, notification area, or wallpaper API.
- Preserve the supplied icon pixels; conversion may only resize and package them.
- Leave implementation uncommitted until user manual testing.

---

### Task 1: Shared settings state and behavior actions

**Files:**
- Create: `SettingsRepository.cs`
- Create: `AppBehaviorActions.cs`
- Create: `IStartupRegistration.cs`
- Create: `WindowsStartupRegistration.cs`
- Modify: `WallpaperSettings.cs`
- Modify: `WallpaperActions.cs`
- Modify: `App.xaml.cs`
- Modify: `tests/Wallppr.Tests/WallpaperActionsTests.cs`
- Create: `tests/Wallppr.Tests/AppBehaviorActionsTests.cs`
- Modify: `tests/Wallppr.Tests/JsonSettingsStoreTests.cs`

**Interfaces:**
- Produces: `AppBehaviorSettings`, `SettingsRepository.Current`, `SettingsRepository.Save(WallpprSettings)`, and `AppBehaviorActions.Current`.
- Produces: `AppBehaviorActions.SetStartWithWindows(bool)`, `SetMinimizeToTray(bool)`, `SetCloseToTray(bool)`, and `Changed`.
- Produces: `IStartupRegistration.SetEnabled(bool)`; Windows implementation writes only the current-user `wallppr` Run value.

- [ ] **Step 1: Write failing action and persistence tests**

```csharp
[TestMethod]
public void Tray_behavior_persists_without_touching_startup_registration()
{
    var store = new MemorySettingsStore();
    var repository = new SettingsRepository(store, store.Settings);
    var startup = new FakeStartupRegistration();
    var actions = new AppBehaviorActions(repository, startup);

    var behavior = actions.SetCloseToTray(true);

    Assert.IsTrue(behavior.CloseToTray);
    Assert.IsFalse(startup.WasCalled);
    Assert.IsTrue(store.Settings.Behavior.CloseToTray);
}

[TestMethod]
public void Startup_failure_does_not_persist_enabled_state()
{
    var store = new MemorySettingsStore();
    var repository = new SettingsRepository(store, store.Settings);
    var actions = new AppBehaviorActions(repository, new FakeStartupRegistration { ThrowOnSet = true });

    Assert.ThrowsExactly<InvalidOperationException>(() => actions.SetStartWithWindows(true));
    Assert.IsFalse(store.Settings.Behavior.StartWithWindows);
}
```

Extend the JSON round-trip fixture with all three `AppBehaviorSettings` flags. Update existing wallpaper-action tests to construct one shared `SettingsRepository`.

- [ ] **Step 2: Run focused tests RED**

Run:

```powershell
dotnet test .\tests\Wallppr.Tests\Wallppr.Tests.csproj --filter "FullyQualifiedName~AppBehaviorActionsTests|FullyQualifiedName~JsonSettingsStoreTests"
```

Expected: compile failure because the new settings types and actions do not exist.

- [ ] **Step 3: Implement minimal shared settings model**

Add to `WallpaperSettings.cs`:

```csharp
public sealed record AppBehaviorSettings
{
    public bool StartWithWindows { get; init; }
    public bool MinimizeToTray { get; init; }
    public bool CloseToTray { get; init; }
}

public AppBehaviorSettings Behavior { get; init; } = new();
```

`SettingsRepository.Save` writes first, then replaces `Current`. Refactor `WallpaperActions` to consume this shared repository instead of holding a separate settings snapshot.

- [ ] **Step 4: Implement behavior actions and startup boundary**

`SetStartWithWindows` calls `IStartupRegistration.SetEnabled` before persisting. Tray-only setters persist directly. Raise `Changed` only after success.

`WindowsStartupRegistration` uses:

```csharp
const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
const string ValueName = "wallppr";
```

Enable with a quoted `Environment.ProcessPath`; disable with `DeleteValue(ValueName, throwOnMissingValue: false)`.

- [ ] **Step 5: Run focused tests GREEN, then full suite**

```powershell
dotnet test .\tests\Wallppr.Tests\Wallppr.Tests.csproj
```

Expected: all tests pass; no live registry access.

### Task 2: Product icon assets

**Files:**
- Move: `icon.png` → `Assets/wallppr.png`
- Create: `Assets/wallppr.ico`
- Modify: `Wallppr.csproj`
- Modify: `MainWindow.xaml`
- Later Task 3 sets: `SettingsWindow.xaml`

**Interfaces:**
- Produces embedded `/Assets/wallppr.ico` used by executable metadata, WPF windows, and `TrayIconService`.

- [ ] **Step 1: Move the canonical PNG and generate the ICO**

Create `Assets`, move the supplied 1024×1024 ARGB PNG, then run:

```powershell
python -c "from PIL import Image; im=Image.open(r'Assets\wallppr.png').convert('RGBA'); im.save(r'Assets\wallppr.ico', sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])"
```

- [ ] **Step 2: Register application and WPF resources**

Add `UseWindowsForms`, `ApplicationIcon`, and an embedded `Resource` entry to `Wallppr.csproj`. Set `Icon="/Assets/wallppr.ico"` on both windows.

- [ ] **Step 3: Verify icon packaging**

```powershell
dotnet build .\Wallppr.csproj -c Release
Get-Item .\bin\Release\net10.0-windows\Wallppr.exe | Select-Object Name,Length
```

Expected: build succeeds and `Wallppr.exe` exists.

### Task 3: Extendable settings window

**Files:**
- Create: `SettingsViewModel.cs`
- Create: `SettingsWindow.xaml`
- Create: `SettingsWindow.xaml.cs`
- Create: `tests/Wallppr.Tests/SettingsViewModelTests.cs`
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`
- Modify: `App.xaml.cs`

**Interfaces:**
- Consumes: `AppBehaviorActions.Current` and its three setters.
- Produces: a singleton modeless Settings window and `MainWindow.SettingsRequested` event.

- [ ] **Step 1: Write the failing presentation test**

```csharp
[TestMethod]
public void Apply_updates_all_behavior_toggles()
{
    var viewModel = new SettingsViewModel();
    viewModel.Apply(new AppBehaviorSettings
    {
        StartWithWindows = true,
        MinimizeToTray = true,
        CloseToTray = true
    });

    Assert.IsTrue(viewModel.StartWithWindows);
    Assert.IsTrue(viewModel.MinimizeToTray);
    Assert.IsTrue(viewModel.CloseToTray);
}
```

- [ ] **Step 2: Run presentation test RED**

Expected: compile failure because `SettingsViewModel` does not exist.

- [ ] **Step 3: Implement presentation model and window**

Build one dark, modeless window with header **Settings**, one **Startup & background** card, three accessible toggle controls, inline status text, and the embedded icon. Toggle handlers call `AppBehaviorActions`; on failure they re-apply `Current` and show the exception message.

- [ ] **Step 4: Add main-header gear and singleton ownership**

Place the gear beside **Refresh**. `MainWindow` raises `SettingsRequested`. `App.ShowSettings` activates the existing window or creates one with `Owner = MainWindow`; closing the settings window clears the cached reference.

- [ ] **Step 5: Run tests and build**

```powershell
dotnet test .\tests\Wallppr.Tests\Wallppr.Tests.csproj
dotnet build .\Wallppr.csproj -c Release
```

Expected: tests and XAML compilation pass with no warnings.

### Task 4: Tray and window lifecycle

**Files:**
- Create: `TrayIconService.cs`
- Modify: `MainWindow.xaml.cs`
- Modify: `App.xaml.cs`
- Modify: `README.md`

**Interfaces:**
- Consumes: embedded `/Assets/wallppr.ico` and `AppBehaviorActions.Changed`.
- Produces: `TrayIconService.SetVisible(bool)`, `MainWindow.Restore()`, and `MainWindow.AllowExit()`.

- [ ] **Step 1: Implement concrete tray ownership**

Use `System.Windows.Forms.NotifyIcon` with menu items **Open Wallppr**, **Settings**, and **Exit**. Double-click invokes the same restore callback. `Dispose` hides and disposes the icon.

- [ ] **Step 2: Apply minimize and close policies**

On minimize with `MinimizeToTray`, hide and normalize the main window. On close with `CloseToTray`, cancel and hide unless `AllowExit` was called. Restore shows, normalizes, and activates.

When neither tray behavior is enabled, hide the tray icon. If both are disabled while the main window is hidden, restore the main window before hiding the tray icon.

- [ ] **Step 3: Wire application exit and refresh documentation**

Tray **Exit** calls `AllowExit`, closes the settings window, and shuts down. `App.OnExit` disposes tray and wallpaper services. README documents the settings path, Run-key behavior, tray controls, and missing scheduler/autorun installer scope.

- [ ] **Step 4: Run final automated verification**

```powershell
dotnet test .\tests\Wallppr.Tests\Wallppr.Tests.csproj -c Release
dotnet build .\Wallppr.csproj -c Release --no-restore
git diff --check
```

Expected: all tests pass, build has zero warnings/errors, and diff check is clean.

- [ ] **Step 5: Prepare manual-test handoff**

Do not launch the GUI automatically. Provide `C:\src\wallppr\bin\Release\net10.0-windows\Wallppr.exe` and ask the user to verify the icon, singleton settings window, three toggles, minimize/close-to-tray, tray restore/settings/exit, and Windows-startup registration. Leave implementation uncommitted and unpushed.
