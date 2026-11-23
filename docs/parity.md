# Parity with Menew for Mac

The Mac app ([Quost/menew](https://github.com/Quost/menew)) is the specification. Menew for Linux ([Quost/menew-linux](https://github.com/Quost/menew-linux)) already ported most of release 1 and has executable test vectors (`docs/palette-spec.md`, `docs/clipboard-spec.md`...) that this port reuses.

Status: ✅ done · 🟡 partial · ⬜ to port · 🔁 port with a different technology · ❌ no Windows equivalent (explained)

**Release 1 ("core")**: the same rows as Linux release 1 (marked **R1**). Everything is checked on a real Windows 11 machine before the first public release carries the name Menew.

## Command palette (`Palette.swift`, `Calculator.swift`, `TextTransforms.swift`, `QuickLinks.swift`, `Emoji.swift`, `Timers.swift`, `ScriptCommands.swift`)

| Feature | Mac | Windows | R1 | Notes |
|---|---|---|---|---|
| Open with a global shortcut (⌃⌥Space) | ✅ | 🟡 | R1 | `RegisterHotKey` Ctrl+Alt+Space (`TrayHost.cs`); a balloon says so when another app holds it. Not run on a real machine yet. |
| Find and launch apps | ✅ | 🟡 | R1 | Start menu shortcuts (all users + current user) and packaged apps through `PackageManager` + `GetAppListEntries` (`AppIndex.cs`); same scores as the Mac (`Search.cs`). No app icons yet. |
| Recent picks first | ✅ | ⬜ | R1 | |
| Menew actions in the palette | ✅ | ⬜ | R1 | |
| Math (`12*7.5`, `20% of 340`) | ✅ | ✅ | R1 | `Calculator.cs`, port of `Calculator.swift` with the Linux vectors; Enter copies the result. |
| Unit conversions (`5 km to mi`, `72 f to c`) | ✅ | ⬜ | R1 | Port `conversions.rs`. |
| Currency conversion (`100 usd to brl`, fetched on ↩) | ✅ | ⬜ | R1 | frankfurter.app, only on Enter. |
| Time zones (`3pm ny to lisbon`) and world clocks | ✅ | ⬜ | R1 | `TimeZoneInfo` (IANA ids work on Windows 10 1903+ with ICU). |
| Text transforms with live preview | ✅ | ⬜ | R1 | |
| Developer tools (JWT decode, UUID, Unix timestamp) | ✅ | ⬜ | R1 | |
| Quick links, typed addresses, web search | ✅ | ⬜ | R1 | |
| Emoji and symbols | ✅ | ⬜ | R1 | |
| Timers and alarms, countdown in the tray | ✅ | ⬜ | R1 | Countdown in the tray tooltip; toast and sound at the end. |
| Focus timer (`focus 50`) | ✅ | ⬜ | R1 | Can also turn on Windows "Do not disturb" later. |
| Script commands (Raycast headers) | ✅ | ⬜ | R1 | PowerShell, Python, Node, `.cmd`/`.bat`. |
| Snippets in the palette | ✅ | ⬜ | R1 | |
| Notes in the palette | ✅ | ⬜ | R1 | |
| Quick reminders, translation, Ask AI | ✅ | ⬜ | | After release 1, like Linux. |

## Clipboard (`ClipboardManager.swift`, `PasteQueue.swift`, `SmartClip.swift`, `Snippets.swift`)

| Feature | Mac | Windows | R1 | Notes |
|---|---|---|---|---|
| Text history with search, pins | ✅ | ⬜ | R1 | `AddClipboardFormatListener` (no polling), SQLite through `Microsoft.Data.Sqlite` or a plain file. |
| Images and files in the history, rich preview | ✅ | ⬜ | R1 | |
| Dedicated clipboard shortcut | ✅ | ⬜ | R1 | Ctrl+Alt+V (Win+V is the system clipboard). |
| Paste automatically after picking | ✅ | ⬜ | R1 | `SendInput` Ctrl+V into the app you came from: works on Windows without special permission (not into elevated apps). |
| Skip passwords | ✅ | ⬜ | R1 | Clipboard formats `ExcludeClipboardContentFromMonitorProcessing`, `CanIncludeInClipboardHistory = 0` (used by password managers). |
| Ignore chosen apps | ✅ | ⬜ | R1 | `GetClipboardOwner` → process. |
| Pause, filters, paste queue, smart actions | ✅ | ⬜ | R1 | |
| Text inside copied images (OCR) | ✅ | 🔁 | R1 | `Windows.Media.Ocr` (built into Windows, offline). |
| Snippets with variables | ✅ | ⬜ | R1 | |
| Snippet abbreviations while typing | ✅ | ⬜ | | Needs a low-level keyboard hook; later. |

## Notes (`Notes.swift`, `NotesViews.swift`, `Markdown.swift`)

| Feature | Mac | Windows | R1 | Notes |
|---|---|---|---|---|
| Many notes, #tags, search, Markdown with checklists | ✅ | ⬜ | R1 | Same `notes.json` format as the Mac and Linux. |
| Protected notes | ✅ | 🔁 | R1 | Content in the Windows Credential Manager / DPAPI, opened after Windows Hello (`UserConsentVerifier`), like Touch ID on the Mac. |
| Password notes (hidden values, copy, cleared after 30 s) | ✅ | ⬜ | R1 | |

## Screenshots and screen (`Capture.swift`, `TextRecognizer.swift`, `QRCode.swift`)

| Feature | Mac | Windows | R1 | Notes |
|---|---|---|---|---|
| Capture area / window / screen with a shortcut | ✅ | 🔁 | R1 | `Windows.Graphics.Capture` (or GDI for the area overlay). |
| Annotate (pen, arrows, rectangles, text, blur), copy, save, drag | ✅ | ⬜ | R1 | Win2D or WinUI `InkCanvas`. |
| Copy text from the screen (OCR) | ✅ | 🔁 | R1 | `Windows.Media.Ocr`. |
| QR codes | ✅ | ⬜ | R1 | Needs a small QR encoder/decoder (no system API). |
| Pin, recording, scrolling capture, ask about the screen | ✅ | ⬜ | | After release 1. |

## Windows and files (`Windows.swift`, `Shelf.swift`, `ImageTools.swift`, `PDFTools.swift`)

| Feature | Mac | Windows | R1 | Notes |
|---|---|---|---|---|
| Snap the front window (halves, corners, thirds, full, other display) | ✅ | 🔁 | R1 | `SetWindowPos` on the foreground window: easier than on Wayland. The Mac's 17 positions. |
| Drag to snap with preview | ✅ | ❌ | | Windows has Snap Layouts built in. |
| Shelf, image and PDF tools, profiles | ✅ | ⬜ | | After release 1. |

## Quick actions (`Toolbox.swift`, `AppControl.swift`, `Ports.swift`, `Hardware.swift`)

| Feature | Mac | Windows | R1 | Notes |
|---|---|---|---|---|
| Mute microphone / sound | ✅ | 🔁 | R1 | Core Audio `IAudioEndpointVolume`. |
| Switch audio output and input | ✅ | 🔁 | R1 | `IPolicyConfig` (undocumented, used by every audio switcher) or open the Settings page. |
| Dark mode toggle | ✅ | 🔁 | R1 | `HKCU\...\Themes\Personalize` `AppsUseLightTheme` / `SystemUsesLightTheme` + broadcast. |
| Keep awake | ✅ | 🔁 | R1 | `PowerCreateRequest` / `SetThreadExecutionState`. |
| Lock screen | ✅ | 🔁 | R1 | `LockWorkStation`. |
| Empty trash | ✅ | 🔁 | R1 | `SHEmptyRecycleBin`, with confirmation. |
| Color picker | ✅ | ⬜ | R1 | Full-screen overlay with a magnifier. |
| Plain-text clipboard | ✅ | ⬜ | R1 | |
| Eject disks | ✅ | 🔁 | R1 | `CM_Request_Device_Eject`. |
| Display brightness (built-in + DDC/CI) | ✅ | 🔁 | R1 | WMI `WmiMonitorBrightnessMethods` + `SetMonitorBrightness` (dxva2). |
| Quit apps / force quit / never-quit list | ✅ | 🔁 | R1 | `WM_CLOSE` to the app's windows (a polite quit, unlike Wayland), `TerminateProcess` to force. |
| Ports in use (stop a server) | ✅ | 🔁 | R1 | `GetExtendedTcpTable`. |

## System health (`SystemMonitor.swift`, `MonitorViews.swift` and the rest)

| Feature | Mac | Windows | R1 | Notes |
|---|---|---|---|---|
| CPU, GPU, memory, disk, battery, network live | ✅ | 🔁 | R1 | `GetSystemTimes`, `GlobalMemoryStatusEx`, PDH counters (GPU engine), `GetSystemPowerStatus`. |
| Top processes (quit in one click) | ✅ | ⬜ | R1 | |
| Metrics in the tray | ✅ | 🟡 | R1 | One tray icon per metric is possible (drawn icons); tooltip for the rest. |
| History, temperatures, alerts, network, startup items, cleaning, uninstall | ✅ | ⬜ | | After release 1. |

## Recordings, memory, AI

| Feature | Mac | Windows | R1 | Notes |
|---|---|---|---|---|
| Recordings, transcription, minutes, live captions, Memory, rewrite | ✅ | ⬜ | | After release 1. WASAPI loopback for computer audio; local models through Ollama like Linux. |

## Made for macOS → made for Windows

| Feature | Mac | Windows | R1 | Notes |
|---|---|---|---|---|
| Menu bar icon | ✅ | 🟡 | R1 | Notification-area icon: left click opens the palette, right click has Open palette and Quit (`TrayHost.cs`). |
| Menu bar popover ("Now", "Actions") | ✅ | ⬜ | R1 | A flyout window next to the tray. |
| Menew window (sidebar) | ✅ | ⬜ | R1 | WinUI `NavigationView` with Mica. |
| Settings, welcome tour | ✅ | ⬜ | R1 | |
| 5 languages, follow system or pick | ✅ | 🟡 | R1 | `Strings.cs` in 5 languages, follows the Windows display language; no picker yet. |
| Start at login | ✅ | ⬜ | R1 | `HKCU\...\Run` (opt-in, like Linux). |
| Updates | ✅ | 🟡 | R1 | Velopack (`Updater.cs`): GitHub releases checked 5 min after start and daily (like Linux), background download with deltas, installed on the next start or from the tray ("Install Menew X and Relaunch"), Check for Updates and an automatic-check toggle in the tray. Releases fully automatic (release-please, `release.yml`, winget). Unlike Mac and Linux: no update window with the notes and no Skip This Version yet, and builds are not code-signed yet (Velopack checks the SHA-256 of the packages from the feed; no separate signing key like Sparkle or Linux). Not run on a real install against a real release yet. |
| Liquid Glass design | ✅ | 🔁 | | Acrylic on the palette, Mica on windows: the Windows 11 materials. |
| Live notch, widgets, Siri/Shortcuts, Continuity Camera | ✅ | ❌ | | No equivalent; Windows widgets could come later. |
| Backup / restore | ✅ | ⬜ | | |
