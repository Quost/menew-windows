# Plan to release 1

Release 1 is the same "core" as Linux release 1 (the **R1** rows of `docs/parity.md`). Each phase is one PR: code, tests (vectors ported from `../menew-linux/docs/*-spec.md`), `Strings.cs` in 5 languages, parity, README and changelog.

| Phase | What | Status |
|---|---|---|
| 0 | Scaffold: solution, core library and tests, WinUI 3 app, tray icon, Ctrl+Alt+Space, palette with apps and math, CI on Windows | in progress |
| 1 | Run on a real Windows 11 machine: hotkey, tray, focus, app list, acrylic. Fix what breaks. App icons in the list. | |
| 2 | Palette engine: port `palette.rs` and run `palette-spec.md` as a test (recents, actions, conversions, currency on Enter, time zones, text transforms, dev tools, quick links, emoji, timers and alarms, focus, script commands) | |
| 3 | Clipboard history (`clipboard-spec.md`): listener, SQLite, images and files, filters, pins, pause, skip passwords, paste with `SendInput`, paste queue, smart actions, OCR, snippets | |
| 4 | Notes (`notes-spec.md`), protected notes with Windows Hello | |
| 5 | Screenshots, annotation editor, screen text, QR codes (`screenshots-spec.md`) | |
| 6 | Windows snapping and quick actions | |
| 7 | System monitor (`monitor-spec.md`) | |
| 8 | Tray flyout (Now / Actions), Menew window, settings, welcome tour, language picker, start at login | |
| 9 | Releases: signed builds on GitHub, updates (`updates-spec.md`), winget | |

Before the first tag: everything checked on a real Windows 11 desktop (and Windows 10 if we keep supporting it), the README and the website.
