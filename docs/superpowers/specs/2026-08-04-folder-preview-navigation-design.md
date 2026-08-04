# Folder preview navigation

## Goal

Folder mode previews an actual image and offers manual `Next` navigation. No timed slideshow or wallpaper application.

## Behavior

- Scan selected folder only; ignore subfolders.
- Support `.bmp`, `.gif`, `.jpeg`, `.jpg`, `.png`, `.tif`, `.tiff`, and `.webp`.
- Sort paths case-insensitively for sequential order.
- Selecting folder shows first sorted image in sequential mode, or random image in random mode.
- `Next` wraps in sequential mode.
- `Next` chooses different image in random mode when folder contains more than one.
- Changing order preserves current image; next click uses new order.
- Empty or inaccessible folder keeps placeholder and disables `Next`.

## UI

- Folder preview uses same image viewport as image mode.
- Folder name remains visible beneath preview.
- `Choose folder` and `Next` share action row.
- Existing order toggle remains below actions.

## Validation

- Unit tests cover filtering, sequential wrap, and random no-repeat.
- Live WPF check covers folder selection and `Next` preview change.
