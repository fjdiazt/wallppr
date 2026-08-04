# Folder source UI

## Goal

Expose WallP-style folder selection and sequential/random ordering per monitor. UI only; no slideshow execution, scheduling, persistence, or autorun.

## Card design

- Add `Image | Folder` segmented toggle beneath monitor header.
- Image mode keeps current preview, selected filename, `Choose image`, and `Apply` flow unchanged.
- Folder mode shows folder icon/placeholder instead of image preview, selected folder name/path, and `Choose folder` button.
- Folder mode adds `Sequential | Random` segmented toggle. Default: `Sequential`.
- Folder selections and ordering live only in memory for this POC.
- Folder mode has no apply action. Status banner states slideshow behavior is not implemented yet.

## Interaction

- Switching source mode preserves both pending image and folder selections while window remains open.
- `Choose folder` opens native Windows folder picker.
- Selecting folder updates card immediately.
- Switching order updates card state only.

## Validation

- App builds.
- Existing image selection tests remain green.
- Manual UI check covers all four monitors, source toggle, folder picker, and order toggle.
