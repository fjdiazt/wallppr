# Four-card Preview Actions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fit four monitor cards in the default window and make preview clicks open the picker for the active source.

**Architecture:** Reuse the existing picker paths in `MainWindow`. Expose the active source and accessible action label from `MonitorCardViewModel`; use a native WPF button for pointer and keyboard behavior.

**Tech Stack:** .NET 10, WPF, MSTest

## Global Constraints

- No new dependencies or services.
- Keep existing picker buttons and wrapping behavior.
- Do not commit or push implementation before manual testing.

---

### Task 1: Source-aware preview action

**Files:**
- Modify: `tests/Wallppr.Tests/MonitorCardPresentationTests.cs`
- Modify: `MonitorCardViewModel.cs`

**Interfaces:**
- Produces: `WallpaperSource Source` and `string PreviewActionText`

- [ ] **Step 1: Write the failing test**

Add literal assertions after applying a Folder profile:

```csharp
Assert.AreEqual(WallpaperSource.Folder, viewModel.Source);
Assert.AreEqual("Choose wallpaper folder", viewModel.PreviewActionText);
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Wallppr.Tests/Wallppr.Tests.csproj --filter Applying_profile_updates_folder_presentation_state`

Expected: FAIL because the properties do not exist.

- [ ] **Step 3: Write minimal implementation**

```csharp
public WallpaperSource Source => IsFolderSource ? WallpaperSource.Folder : WallpaperSource.Image;
public string PreviewActionText => IsFolderSource ? "Choose wallpaper folder" : "Choose wallpaper image";
```

Notify both properties whenever `IsFolderSource` changes.

- [ ] **Step 4: Run test to verify it passes**

Run the focused test again. Expected: PASS.

### Task 2: Four-card clickable preview

**Files:**
- Modify: `App.xaml`
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `MonitorCardViewModel.Source`, `PreviewActionText`
- Produces: `Preview_Click`, `ChooseImage`, `ChooseFolder`

- [ ] **Step 1: Add the native preview button style**

Use a flat `Button` template with `Cursor="Hand"`, hover, pressed, and keyboard-focus states.

- [ ] **Step 2: Make preview interactive and widen the window**

Set `Width="1540"`; replace the preview border with the styled button using `Click="Preview_Click"`, tooltip, and automation label bindings.

- [ ] **Step 3: Reuse picker methods**

Extract the current dialog bodies to `ChooseImage(MonitorCardViewModel)` and `ChooseFolder(MonitorCardViewModel)`. Dispatch preview clicks by `monitor.Source`.

- [ ] **Step 4: Verify**

Run: `dotnet test tests/Wallppr.Tests/Wallppr.Tests.csproj`

Run: `dotnet build Wallppr.csproj -c Release --no-restore -o C:\tmp\wallppr-four-card-build`

Expected: all tests pass; build has zero errors.
