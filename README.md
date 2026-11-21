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

## Keyboard shortcuts

| Shortcut | Action |
|---|---|
| <kbd>Ctrl</kbd> <kbd>Alt</kbd> <kbd>Space</kbd> | Open or close the palette |
| <kbd>↑</kbd> <kbd>↓</kbd> / <kbd>Enter</kbd> / <kbd>Esc</kbd> | Move / open or copy / close |

## Privacy

No telemetry, no analytics, no account, and today **no network request at all**. The app list is read from your Start menu folders and installed packages, on your computer.

## Building

Runs on Windows 10 2004 (build 19041) or later and Windows 11. Building needs the [.NET 10 SDK](https://dotnet.microsoft.com/download) and, for a Native AOT build, the "Desktop development with C++" workload of Visual Studio.

```powershell
dotnet build src/Menew.App -p:Platform=x64
dotnet test tests/Menew.Core.Tests
dotnet publish src/Menew.App -c Release -r win-x64
```

The core (`src/Menew.Core`) has no Windows APIs, so its tests also run on macOS and Linux.

## License

[GPL-3.0](LICENSE). The name "Menew", the icon and the website are not covered by the license: see [TRADEMARK.md](TRADEMARK.md).
