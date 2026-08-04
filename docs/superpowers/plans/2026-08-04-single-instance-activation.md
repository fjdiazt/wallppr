# Single-Instance Activation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** Prevent duplicate wallppr processes and restore the existing window when wallppr launches again.

**Architecture:** A concrete SingleInstanceCoordinator owns a per-session named mutex and activation event. App checks it before expensive initialization; a secondary signals and exits, while the primary starts an event listener after its MainWindow exists.

**Tech Stack:** .NET 10, WPF, System.Threading named synchronization primitives, MSTest.

## Global Constraints

- One wallppr process per Windows session.
- Second launch restores the existing window, including from tray.
- Detection happens before settings, wallpaper, tray, or window initialization.
- No service process, polling, package, registry setting, window-title lookup, named pipe, or single-implementation interface.

---

### Task 1: Native single-instance coordinator

**Files:**
- Create: SingleInstanceCoordinator.cs
- Create: tests/Wallppr.Tests/SingleInstanceCoordinatorTests.cs

**Interfaces:**
- Produces: SingleInstanceCoordinator(string applicationId), IsPrimary, ActivationRequested, StartListening(), SignalPrimary(), Dispose().

- [ ] **Step 1: Write failing ownership and activation tests**

Create coordinators with unique application IDs. Hold the primary mutex on the test thread, construct secondary coordinators on a dedicated thread, and assert only the primary owns the mutex. Start listening, signal from the secondary thread, and assert a ManualResetEventSlim callback fires.

- [ ] **Step 2: Verify RED**

Run:

    dotnet test tests/Wallppr.Tests/Wallppr.Tests.csproj --filter SingleInstanceCoordinatorTests --no-restore

Expected: compilation fails because SingleInstanceCoordinator does not exist.

- [ ] **Step 3: Implement the coordinator**

Use Mutex.WaitOne(0) and treat AbandonedMutexException as successful acquisition. Use an EventWaitHandle with EventResetMode.AutoReset and ThreadPool.RegisterWaitForSingleObject for activation. Release the mutex only when IsPrimary is true.

- [ ] **Step 4: Verify GREEN**

Run the focused test command again. Expected: ownership, activation, and post-disposal acquisition tests pass.

### Task 2: WPF startup integration

**Files:**
- Modify: App.xaml.cs

**Interfaces:**
- Consumes: SingleInstanceCoordinator.
- Produces: early secondary exit and primary MainWindow.Restore dispatch.

- [ ] **Step 1: Add coordinator before existing startup work**

Create SingleInstanceCoordinator("wallppr") immediately after base.OnStartup. If not primary, call SignalPrimary(), Shutdown(), and return.

- [ ] **Step 2: Start activation after MainWindow exists**

After mainWindow.Show(), subscribe ActivationRequested to Dispatcher.BeginInvoke(mainWindow.Restore), then call StartListening(). Dispose the coordinator in OnExit.

- [ ] **Step 3: Verify app**

Run:

    dotnet test tests/Wallppr.Tests/Wallppr.Tests.csproj --no-restore
    dotnet build Wallppr.csproj -c Release --no-restore

Launch Release twice. Confirm exactly one Wallppr process remains and the existing window becomes visible and active.

## Self-Review

- Covers ownership, activation, tray restoration, startup ordering, shutdown, and abandoned mutex recovery.
- Signatures match between tasks.
- No placeholders or speculative IPC framework.
- Implementation commit and push remain unperformed until explicitly requested.
