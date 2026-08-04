# Default window height design

## Goal

Show one complete row of monitor cards at startup without clipping the card bottoms.

## Design

- Change the main window default height from `780` to `840` device-independent pixels.
- Keep the current card dimensions, spacing, `MinHeight="560"`, and vertical scrollbar behavior.
- Do not change restored or user-resized window behavior.

## Verification

- Build the WPF project to validate XAML.
- Manually confirm a full folder-mode card row is visible at the default size.
