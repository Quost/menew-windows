# Menew for Windows: project rules for agents

Native Windows port of [Menew for Mac](https://github.com/Quost/menew) (`../MeuMenu` or `../menew` locally). C# + WinUI 3 (Windows App SDK), unpackaged and self-contained, Native AOT on publish. GPL-3.0-only, like the Mac and [Linux](https://github.com/Quost/menew-linux) apps; the name, icon and site are not covered (`TRADEMARK.md`).

**Parity with the Mac app is the goal.** Before planning or reviewing a feature, read the Mac code (`Sources/Menew/*.swift`) and the Linux port (`../menew-linux`, its `docs/*-spec.md` test vectors), and keep `docs/parity.md` up to date. Don't edit the other two repos from here.

## Layout

- `src/Menew.Core`: pure logic (palette, search, calculator, strings), `net10.0`, no Windows APIs. Builds and tests on any OS.
- `src/Menew.App`: the WinUI 3 app (`Menew.exe`): tray icon and hotkey (`TrayHost.cs`, Win32 through `Native.cs`), palette window, app index.
- `tests/Menew.Core.Tests`: xUnit. Port the Linux vectors with each feature (the Mac is the reference, Linux already checked it).

## Core principles (never violate)

1. **No telemetry or analytics, ever.** No network calls except ones the user starts (currency on Enter, speed test) and the update check (opt-out, GitHub releases only). Document every one in the README's Privacy section.
2. **Native first.** WinUI 3 controls, Mica/Acrylic, Segoe Fluent Icons, the system's own APIs. Win32 through source-generated `LibraryImport` (AOT-safe). No third-party packages without a strong reason; today only the Windows App SDK.
3. **5 languages**: every UI string goes through `Strings.L("key")` with en, pt, de, es, zh in `Strings.cs` (keys follow Linux `strings.rs`). A test fails on a missing language or an em-dash.
4. **No em-dash** in code, comments or docs. Use a regular hyphen.
5. **Conventional Commits in English** (`feat(palette): ...`, `fix(tray): ...`), signed with the maintainer's SSH key. If `git commit` hangs, ask for `ssh-add --apple-use-keychain ~/.ssh/id_ed25519`. Never force-push.

## Building and testing

The maintainer works on a Mac, so the app itself builds only in CI (`windows-latest`). Locally:

```bash
docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test tests/Menew.Core.Tests
```

On Windows: `dotnet build src/Menew.App -p:Platform=x64` and run `src/Menew.App/bin/x64/Debug/net10.0-windows10.0.19041.0/win-x64/Menew.exe`; release build: `dotnet publish src/Menew.App -c Release -r win-x64` (Native AOT). Nothing that needs a real Windows desktop (hotkey, tray, focus, clipboard) is verified until someone runs it on Windows: say so in PRs.

## When a change is visible to users

Update in the same change: `Strings.cs` (5 languages), `docs/parity.md`, `README.md` (features, shortcuts, privacy) and `CHANGELOG.md` (`## Unreleased`, for users, in English).
