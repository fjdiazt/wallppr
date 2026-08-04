# Four-card preview actions design

## Goal

Open wallppr wide enough for four monitor cards in one row. Make each wallpaper preview act as the existing source picker.

## Design

- Set the main window default width to `1540`; retain wrapping and horizontal resizing for smaller screens.
- Replace the passive preview border with a native WPF `Button` styled as the same flat preview surface.
- Use `Cursor="Hand"`, hover, pressed, and keyboard-focus states.
- In Image mode, preview click opens the existing image picker.
- In Folder mode, preview click opens the existing folder picker.
- Keep the existing explicit picker buttons.

## Verification

- Add one focused view-model test for the source-specific preview action.
- Build WPF XAML and run the full test suite.
