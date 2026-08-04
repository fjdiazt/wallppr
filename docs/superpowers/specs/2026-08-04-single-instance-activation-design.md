# Single-Instance Activation Design

## Goal

Allow one wallppr process per Windows session. Launching wallppr again restores and activates the existing window, including when it is hidden in the tray.

## Architecture

Add one SingleInstanceCoordinator using native .NET named synchronization objects:

- A named mutex elects the primary process.
- A named auto-reset event carries activation requests from later processes.
- The primary waits for the event off the UI thread and invokes an activation callback.
- The callback dispatches MainWindow.Restore() onto the WPF UI thread.

Names use the Windows Local namespace, so separate Windows sessions do not block each other. No service process, polling, package, registry setting, window-title lookup, or named pipe is needed.

## Startup Flow

1. App.OnStartup creates the coordinator before wallpaper, settings, tray, or window initialization.
2. If the mutex is already owned, the new process signals the activation event and shuts down immediately.
3. If elected primary, the app completes normal initialization and begins listening.
4. An activation received before MainWindow is ready is retained by the auto-reset event and handled once listening begins.
5. Activation uses the existing MainWindow.Restore() behavior.

## Shutdown and Failure Behavior

The primary disposes the coordinator during App.OnExit, waking its listener and releasing the mutex. A crashed process releases mutex ownership through Windows, allowing the next launch to become primary. Failure to signal an exiting primary still results in the second process exiting; it never creates a duplicate window.

## Testing

Tests use unique synchronization names and verify:

- Only the first coordinator becomes primary.
- A secondary coordinator signals the primary callback.
- Disposing the primary permits a later coordinator to become primary.

The implementation remains a concrete class without an interface because there is one platform behavior and no alternate implementation.
