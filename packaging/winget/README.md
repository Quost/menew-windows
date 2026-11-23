# winget manifests

Menew is published on [winget](https://github.com/microsoft/winget-pkgs) as `Pururucos.Menew`. The `winget` job of
`.github/workflows/release.yml` does it on every stable release (pre-releases are skipped, winget has no beta
channel):

- **First release:** `Pururucos.Menew` is not in winget-pkgs yet, so the job fills these templates (version, the two
  `Menew-win-*-Setup.exe` URLs and their SHA-256) and submits them with `wingetcreate submit`. The filled files are
  also kept as the `winget-manifests` artifact of the run. A winget moderator reviews a new package by hand, so the
  first PR can take a few days.
- **Every release after that:** `wingetcreate update Pururucos.Menew` opens the PR for the new version from the last
  merged manifest; these templates are no longer used (edit the manifest in winget-pkgs instead).

Needs the `WINGET_TOKEN` secret: a classic personal access token with the `public_repo` scope (wingetcreate forks
winget-pkgs to that account and opens the PR from it). Without it the job only prints a notice.

To check a filled copy by hand: `winget validate --manifest <folder>`.
