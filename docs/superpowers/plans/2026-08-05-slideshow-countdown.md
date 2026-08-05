# Slideshow Countdown Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace stale scheduler POC footer copy with an accurate, cheap countdown.

**Architecture:** Extend the existing app-owned `SlideshowTimer` with an in-memory deadline and state-change event. MainWindow uses one visible-only WPF timer to format that state once per second.

**Tech Stack:** .NET 10, C#, WPF DispatcherTimer, MSTest 4.

## Global Constraints

- No new persistence or per-second file writes.
- Countdown work is one clock read and text update per visible-window second.
- Hidden/closed main window stops countdown rendering.
- Existing wallpaper timer remains one-shot and app-hosted.

---

### Task 1: Expose scheduler deadline and state

**Files:**
- Modify: `SlideshowTimer.cs`
- Modify: `tests/Wallppr.Tests/SlideshowTimerTests.cs`

- [ ] Add RED tests using an injected clock: Start sets `NextChangeUtc`; `Remaining` decreases from literal clock movement; Reset saves nothing; automatic advance publishes advancing then scheduled state.
- [ ] Run `dotnet test Wallppr.slnx --filter FullyQualifiedName~SlideshowTimerTests` and verify expected compile failure.
- [ ] Add optional `Func<DateTimeOffset> utcNow`, `NextChangeUtc`, `Remaining`, `IsAdvancing`, and `ScheduleChanged`.
- [ ] In Reset, clear/recreate the in-memory deadline and raise one event. Around automatic advance, publish advancing state then Reset in finally.
- [ ] Run focused tests and verify GREEN.

### Task 2: Format countdown copy

**Files:**
- Modify: `SlideshowTimer.cs`
- Modify: `tests/Wallppr.Tests/SlideshowTimerTests.cs`

- [ ] Add RED data-driven tests for `Slideshow off`, `Changing wallpapers…`, `Next change in 00:42`, and `Next change in 01:02:03`.
- [ ] Run focused tests and verify RED because formatter is absent.
- [ ] Add minimal `SlideshowStatus.Format(bool, TimeSpan?)`, rounding positive fractional seconds upward.
- [ ] Run focused tests and verify GREEN.

### Task 3: Replace footer with visible-only countdown

**Files:**
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`

- [ ] Name the footer text `SlideshowStatusText` and remove `No scheduler · POC`.
- [ ] Add one one-second DispatcherTimer in MainWindow. Start it only while the window is visible; stop it while hidden and on close.
- [ ] Subscribe to `ScheduleChanged` for immediate text refresh. Detach all handlers on close.
- [ ] Run `dotnet test Wallppr.slnx`.
- [ ] Run `dotnet build Wallppr.slnx --configuration Debug --no-restore`.
- [ ] Run `git diff --check`, relaunch exact Debug EXE, and hand off manual UI validation.
