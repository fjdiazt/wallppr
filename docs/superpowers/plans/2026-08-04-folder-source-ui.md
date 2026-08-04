# Folder Source UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add per-monitor `Image | Folder` and `Sequential | Random` UI state without implementing slideshow behavior.

**Architecture:** Extend existing `MonitorCardViewModel` with in-memory source, folder, and order state. Reuse current WPF card, native `OpenFolderDialog`, and click-handler pattern. Existing image apply path stays unchanged.

**Tech Stack:** .NET 10, WPF, MSTest, native Windows dialogs

## Global Constraints

- UI only; no slideshow execution, scheduling, persistence, or autorun.
- Folder selections and ordering live only in memory.
- Existing image selection and apply behavior remains unchanged.
- No new dependencies.

---

### Task 1: Per-monitor folder UI state

**Files:**
- Modify: `tests/Wallppr.Tests/MonitorCardViewModelTests.cs`
- Modify: `MonitorCardViewModel.cs`

**Interfaces:**
- Produces: `bool IsFolderSource`, `bool IsImageSource`, `string? SlideshowFolderPath`, `string FolderName`, `bool IsRandomOrder`

- [x] **Step 1: Write failing state test**

```csharp
[TestMethod]
public void Folder_source_preserves_image_and_folder_choices()
{
    var monitor = new MonitorWallpaper(0, "monitor-id", 0, 0, 1920, 1080, "current.jpg");
    var viewModel = new MonitorCardViewModel(monitor)
    {
        PendingWallpaperPath = "next.png",
        SlideshowFolderPath = @"C:\wallpapers",
        IsFolderSource = true,
        IsRandomOrder = true
    };

    Assert.IsTrue(viewModel.IsFolderSource);
    Assert.IsFalse(viewModel.IsImageSource);
    Assert.IsTrue(viewModel.IsRandomOrder);
    Assert.AreEqual("wallpapers", viewModel.FolderName);
    Assert.AreEqual("next.png", viewModel.PendingWallpaperPath);
}
```

- [x] **Step 2: Verify RED**

Run: `dotnet test tests/Wallppr.Tests/Wallppr.Tests.csproj -c Release`

Expected: compile failure because folder source properties do not exist.

- [x] **Step 3: Add minimal observable state**

Add backing fields and notifying properties to `MonitorCardViewModel`. `IsFolderSource` notifies itself and `IsImageSource`; `SlideshowFolderPath` notifies itself and `FolderName`; `IsRandomOrder` notifies itself. `FolderName` returns `No folder selected` or `new DirectoryInfo(path).Name`.

- [x] **Step 4: Verify GREEN**

Run: `dotnet test tests/Wallppr.Tests/Wallppr.Tests.csproj -c Release`

Expected: all tests pass.

### Task 2: Monitor card controls

**Files:**
- Modify: `App.xaml`
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`

**Interfaces:**
- Consumes: Task 1 view-model properties.
- Produces: interactive `Image | Folder`, native folder picker, and `Sequential | Random` controls.

- [x] **Step 1: Add segmented-button style**

Add `SegmentButtonStyle` based on `BaseButtonStyle`. Bind each button `Tag` to selected state; style trigger `Tag=True` uses accent background and border.

- [x] **Step 2: Add card source controls**

Add `Image | Folder` selector. Keep current preview/actions visible for `IsImageSource`. Folder view binds `FolderName` and `SlideshowFolderPath`, offers `Choose folder`, and exposes `Sequential | Random`. No folder apply button.

- [x] **Step 3: Wire UI-only handlers**

Add handlers setting `IsFolderSource` and `IsRandomOrder`. Use `Microsoft.Win32.OpenFolderDialog`; on selection set `SlideshowFolderPath`. Status banner explicitly says slideshow behavior is not implemented.

- [x] **Step 4: Build and test**

Run: `dotnet test Wallppr.slnx -c Release`

Expected: all tests pass with zero warnings and errors.

- [x] **Step 5: Visual smoke test**

Launch `bin/Release/net10.0-windows/Wallppr.exe`. Verify all four cards render; Display 1 switches to Folder; folder and order toggles highlight correctly; Image mode still renders current preview.

No commit or push; user did not request either.
