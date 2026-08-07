# GitHub Release Package Design

## Goal

When a GitHub Release is published manually, build Wallppr and attach a ready-to-run Windows download automatically.

## Package

- Target Windows x64 only.
- Publish a Release, self-contained, single-file `Wallppr.exe`.
- Bundle native runtime libraries into the executable.
- Put the executable in `wallppr-<tag>-win-x64.zip`.
- Users do not need to install .NET.

## Workflow

- Add one workflow under `.github/workflows/`.
- Trigger on `release: published`, covering normal and prerelease releases.
- Run on `windows-latest`.
- Check out the release tag, install .NET 10, run tests, then publish.
- Create the zip with PowerShell `Compress-Archive`.
- Upload the zip to the existing release with `gh release upload`.
- Grant only `contents: write`, required to attach the release asset.

The asset name comes from `github.event.release.tag_name`. No separate version file or release script is needed.

## Failure behavior

Any restore, test, publish, zip, or upload failure fails the workflow. The release remains published without a binary until the failed run is retried.

## Validation

- Validate the workflow YAML locally by inspection.
- Run the same test and publish commands locally.
- Confirm the publish directory contains one executable before zipping.
- The first real end-to-end upload is verified by publishing a GitHub Release.

## Out of scope

- Installer or MSIX packaging.
- Code signing.
- Windows ARM64 or x86 builds.
- Automatic release creation.
- Automatic updates.
