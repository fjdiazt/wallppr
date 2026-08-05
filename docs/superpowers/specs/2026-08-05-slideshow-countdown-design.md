# Slideshow Countdown Design

## Goal

Replace the stale `Settings & tray · No scheduler · POC` footer with accurate slideshow state.

## Behavior

- Timer enabled: `Next change in 00:42`.
- Timer disabled: `Slideshow off`.
- Automatic change running: `Changing wallpapers…`.
- Intervals of one hour or more use `HH:MM:SS`.

## Runtime Design

`SlideshowTimer` keeps its next deadline in memory and exposes its remaining time and advancing state. Resetting the timer updates the deadline immediately. Nothing new is persisted.

`MainWindow` owns one native WPF `DispatcherTimer` that updates only the footer text once per second. It runs only while the main window is visible and stops when hidden or closed. Each tick performs one clock subtraction and one text assignment; it does not enumerate displays, read files, write settings, or change wallpapers.

The scheduler raises one state-change event on enable, disable, reset, automatic-run start, and automatic-run finish so the footer updates immediately instead of waiting for the next display tick.

## Testing

Use non-live TDD for in-memory deadline/reset behavior, advancing-state transitions, and status formatting. Existing timer tests continue proving resets perform no settings writes. Desktop rendering remains a manual check.
