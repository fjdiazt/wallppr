# Persistent Display Thumbnails Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Open wallppr immediately by loading small local per-display previews instead of decoding full-resolution NAS wallpaper files during startup.

**Architecture:** `WallpaperActions` remains the reusable action boundary for image, folder, Next, and future timer calls. After applying and persisting a wallpaper, it asks a small `WallpaperThumbnailCache` to create one atomic JPEG thumbnail per display; profiles record which source the thumbnail represents. The window renders cached local thumbnails after its first frame and regenerates missing legacy thumbnails asynchronously.

**Tech Stack:** .NET 10, WPF imaging, MSTest, `%LOCALAPPDATA%\wallppr\thumbnails`.

## Global Constraints

- Wallpaper application happens before thumbnail generation.
- Full-resolution wallpaper paths are never bound to WPF `Image.Source`.
- Thumbnail failure must not undo a successfully applied wallpaper.
- No new package, service, database, or thumbnail framework.

---

### Task 1: Persistent thumbnail cache

**Files:**
- Create: `WallpaperThumbnailCache.cs`
- Create: `tests/Wallppr.Tests/WallpaperThumbnailCacheTests.cs`

**Interfaces:**
- Produces: `CreateAsync(string displayId, string sourcePath)`, `GetPath(string displayId)`, and `GetExistingPath(string displayId)`.

- [ ] Write tests proving deterministic per-display paths, atomic thumbnail creation, downsampling, and frozen-image loading.
- [ ] Run focused tests and confirm failure because `WallpaperThumbnailCache` does not exist.
- [ ] Implement the cache with native WPF `BitmapImage`, `DecodePixelWidth`, `JpegBitmapEncoder`, and atomic `File.Move`.
- [ ] Run focused tests and confirm pass.

### Task 2: Reusable wallpaper actions and lazy UI

**Files:**
- Modify: `WallpaperSettings.cs`
- Modify: `WallpaperActions.cs`
- Modify: `App.xaml.cs`
- Modify: `MonitorCardViewModel.cs`
- Modify: `MainWindow.xaml.cs`
- Modify: `MainWindow.xaml`
- Modify: `tests/Wallppr.Tests/WallpaperActionsTests.cs`
- Modify: `tests/Wallppr.Tests/MonitorCardViewModelTests.cs`

**Interfaces:**
- Consumes: `WallpaperThumbnailCache`.
- Produces: async image/folder/Next actions and local `ThumbnailPath` card binding.

- [ ] Write failing tests proving applied wallpaper profiles persist the represented thumbnail source and cards expose only local thumbnail paths.
- [ ] Run focused tests and confirm expected failures.
- [ ] Make image, folder, and Next action methods async; clear stale thumbnail identity before generation and persist the new identity after success.
- [ ] Populate monitor card shells after first render, load valid local thumbnails immediately, and lazily regenerate missing thumbnails without blocking the UI.
- [ ] Replace full wallpaper `Image.Source` bindings with `ThumbnailPath`.
- [ ] Run all tests and Release build.
- [ ] Launch and measure shell readiness plus startup file access.

## Self-Review

- Covers startup, selection, folder selection, Next, future timer reuse, persistence, cache invalidation, and legacy cache misses.
- No placeholders or speculative scheduler/service work.
- One concrete cache class; no single-implementation interface.
