# Folder Preview Navigation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preview folder images and manually navigate with `Next` per monitor.

**Architecture:** Keep folder enumeration and navigation in `MonitorCardViewModel`. Bind existing WPF preview to folder image state and route `Next` through one click handler.

**Tech Stack:** .NET 10, WPF, MSTest

## Global Constraints

- Scan selected folder only.
- Keep slideshow scheduling and wallpaper application out of scope.
- Use existing image extensions and dependencies only.
- Sequential wraps; random avoids immediate repeat when possible.

---

### Task 1: Folder image navigation state

**Files:**
- Modify: `tests/Wallppr.Tests/MonitorCardViewModelTests.cs`
- Modify: `MonitorCardViewModel.cs`

**Interfaces:**
- Produces: `FolderPreviewPath`, `FolderImageName`, `HasFolderImage`, `HasNoFolderImage`, `MoveNextFolderImage()`.

- [ ] **Step 1: Write failing tests**

Create temporary folder with `b.png`, `a.jpg`, and `ignored.txt`. Assert sequential starts at `a.jpg`, advances to `b.png`, wraps to `a.jpg`; random `Next` changes image when two images exist.

- [ ] **Step 2: Verify RED**

Run `dotnet test tests/Wallppr.Tests/Wallppr.Tests.csproj -c Release` and confirm missing navigation API failure.

- [ ] **Step 3: Implement minimal state**

Enumerate supported files when `SlideshowFolderPath` changes. Keep sorted path array and current index. Expose preview properties and implement sequential/random navigation.

- [ ] **Step 4: Verify GREEN**

Run `dotnet test tests/Wallppr.Tests/Wallppr.Tests.csproj -c Release`; all tests pass.

### Task 2: Folder preview and Next button

**Files:**
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`
- Modify: `wallppr-screenshot.png`

**Interfaces:**
- Consumes: Task 1 view-model properties and method.

- [ ] **Step 1: Bind folder preview**

Show `FolderPreviewPath` image when available; keep existing placeholder for empty folders.

- [ ] **Step 2: Add Next action**

Place `Choose folder` and `Next` side by side. Disable `Next` without image. Handler calls `MoveNextFolderImage()` and updates status.

- [ ] **Step 3: Verify**

Run `dotnet test Wallppr.slnx -c Release`, launch Release app, select folder, click `Next`, and capture updated screenshot.

No commit or push; user did not request either for this increment.
