# Menew for Windows

The Windows port of [Menew](https://github.com/Quost/menew), the menu bar app for macOS: a command palette, clipboard history, notes and the small tools you reach for all day, in the notification area. Native: C# and WinUI 3, with Windows 11's own controls and materials. There is also [Menew for Linux](https://github.com/Quost/menew-linux).

> **Work in progress.** Nothing is released yet and nothing here has run on a real Windows machine so far. See [docs/parity.md](docs/parity.md) for what is done and [docs/plan.md](docs/plan.md) for what comes next.

## What works so far

| | |
|---|---|
| ⌨️ **Command palette** | <kbd>Ctrl</kbd> <kbd>Alt</kbd> <kbd>Space</kbd> or a click on the tray icon. A floating acrylic window, centered on the display with the mouse. |
| 🚀 **Apps** | Start menu shortcuts and Store apps, ranked like the Mac (exact, prefix, word, initials like `mt` for Microsoft Teams). <kbd>Enter</kbd> opens. |
| 🧮 **Math** | `12*7.5`, `20% of 340`, `200 + 10%`, `2(3+4)`, `sqrt 16`, `5!`, with a decimal comma in Portuguese, German and Spanish. <kbd>Enter</kbd> copies the result. |
| 🔔 **Tray icon** | Left click opens the palette; right click has Open palette and Quit. Menew runs once: starting it again opens the palette. |
| 🌍 **5 languages** | English, Português, Deutsch, Español and 中文, following the Windows display language. |

## Install

Once the first release is out (none yet):

```powershell
winget install Pururucos.Menew
```

or download the installer from the [latest release](https://github.com/Quost/menew-windows/releases/latest): [Menew-win-x64-Setup.exe](https://github.com/Quost/menew-windows/releases/latest/download/Menew-win-x64-Setup.exe) for most PCs, [Menew-win-arm64-Setup.exe](https://github.com/Quost/menew-windows/releases/latest/download/Menew-win-arm64-Setup.exe) for ARM ones (Snapdragon). It installs for your user only, in `%LocalAppData%\Menew`, without admin rights, and puts Menew in the Start menu. Uninstall from Settings > Apps.

> **SmartScreen.** Menew is not code-signed yet, so the first time Windows may say "Windows protected your PC": click **More info**, then **Run anyway**. Updates after that install without it.

Menew **updates itself**, like the Mac app with Sparkle: it checks the GitHub releases 5 minutes after it starts and then once a day, downloads a new version in the background (usually a small delta) and installs it the next time it starts, or right away with **Install Menew X and Relaunch** in the tray menu. **Check for Updates…** and **Check for updates automatically** are in the tray menu too. A beta (pre-release) version also gets the next betas; a stable one only stable versions.

## Keyboard shortcuts

| Shortcut | Action |
|---|---|
| <kbd>Ctrl</kbd> <kbd>Alt</kbd> <kbd>Space</kbd> | Open or close the palette |
| <kbd>↑</kbd> <kbd>↓</kbd> / <kbd>Enter</kbd> / <kbd>Esc</kbd> | Move / open or copy / close |

## Privacy

No telemetry, no analytics, no account. The app list is read from your Start menu folders and installed packages, on your computer.

The only network request is the **update check**: the list of releases of this repository from the GitHub API (`api.github.com/repos/Quost/menew-windows/releases`), and the update itself from GitHub when there is one. Nothing about you or your computer is sent. Turn it off with **Check for updates automatically** in the tray menu; development (Debug) builds and copies run from a build folder never check.

## Building

Runs on Windows 10 2004 (build 19041) or later and Windows 11. Building needs the [.NET 10 SDK](https://dotnet.microsoft.com/download) and, for a Native AOT build, the "Desktop development with C++" workload of Visual Studio.

```powershell
dotnet build src/Menew.App -p:Platform=x64
dotnet test tests/Menew.Core.Tests
dotnet publish src/Menew.App -c Release -r win-x64
```

The core (`src/Menew.Core`) has no Windows APIs, so its tests also run on macOS and Linux.

`./scripts/package.ps1 -Rid win-x64` builds the release version and packs it with [Velopack](https://velopack.io) into `artifacts/releases`, exactly as CI and the Release workflow do.

## Releasing

Merge the **`chore(release): X.Y.Z`** pull request. That's all: [release-please](https://github.com/googleapis/release-please) keeps that PR up to date on every push to `main`, with the next version worked out from the [Conventional Commits](https://www.conventionalcommits.org) (`feat` = minor, `fix` = patch, `!` = major) in `Directory.Build.props` and the `## Unreleased` notes of `CHANGELOG.md` turned into `## X.Y.Z` (write the notes for users under Unreleased as you go; without any, the PR lists the `feat`/`fix` commits). Merging it tags `vX.Y.Z`, and the Release workflow checks the tag against the version and the changelog, builds and packs both architectures, publishes the GitHub release with that changelog section as notes (a version like `1.2.0-beta.1` is a pre-release) and opens the winget PR. Installed copies pick the update up by themselves. To force a version, add `Release-As: X.Y.Z` to a commit message.

What the maintainer sets once: the `WINGET_TOKEN` secret (classic token, `public_repo` scope) for winget, see [packaging/winget](packaging/winget/README.md); and later, for code signing, the Azure Trusted Signing secrets and variables listed in `.github/workflows/release.yml` (the signing step turns itself on when they exist).

### Release assets

Every release has these files, with the same names each time (the website and the updater rely on them; `releases/latest/download/<name>` always points to the newest stable one):

| File | What it is |
|---|---|
| `Menew-win-x64-Setup.exe`, `Menew-win-arm64-Setup.exe` | Installer (x64 / ARM64) |
| `Menew-win-x64-Portable.zip`, `Menew-win-arm64-Portable.zip` | Portable copy, unzip and run (updates itself too) |
| `Menew-X.Y.Z-win-x64-full.nupkg`, `Menew-X.Y.Z-win-arm64-full.nupkg` | Full update package (Velopack) |
| `Menew-X.Y.Z-win-x64-delta.nupkg`, `Menew-X.Y.Z-win-arm64-delta.nupkg` | Delta from the previous release (from the second release on) |
| `releases.win-x64.json`, `releases.win-arm64.json` | Velopack update feed of each architecture (channel = runtime) |
| `SHA256SUMS` | SHA-256 of every file above |

## License

[GPL-3.0](LICENSE). The name "Menew", the icon and the website are not covered by the license: see [TRADEMARK.md](TRADEMARK.md).
