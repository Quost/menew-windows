using System.Runtime.InteropServices;
using Menew.Core;

namespace Menew;

/// <summary>
/// The notification-area icon (Menew's menu bar icon on the Mac) and the global hotkey. Both talk to
/// a hidden Win32 window created on the UI thread, so its messages arrive there through the
/// WinUI message loop. A hidden top-level window (not message-only) also gets "TaskbarCreated",
/// so the icon comes back when Explorer restarts.
/// </summary>
internal sealed unsafe class TrayHost : IDisposable
{
    private const uint CallbackMessage = Native.WM_APP + 1;
    private const int HotkeyPalette = 1;
    private const int MenuOpen = 1, MenuQuit = 2, MenuCheckUpdates = 3, MenuAutoUpdate = 4, MenuInstallUpdate = 5;

    private static TrayHost? _current;
    private static char* _className;

    private readonly nint _hwnd;
    private readonly nint _icon;
    private readonly uint _taskbarCreated;
    private readonly Action _openPalette;
    private readonly Action _quit;
    private readonly Updater? _updater;
    private bool _added;

    public bool HotkeyRegistered { get; private set; }

    public TrayHost(string iconPath, Action openPalette, Action quit, Updater? updater = null)
    {
        _openPalette = openPalette;
        _quit = quit;
        _updater = updater;
        _current = this;
        _taskbarCreated = Native.RegisterWindowMessage("TaskbarCreated");
        var instance = Native.GetModuleHandle(null);
        if (_className == null)
        {
            _className = (char*)Marshal.StringToHGlobalUni("MenewTrayHost");
            var wc = new Native.WNDCLASSEXW
            {
                cbSize = (uint)sizeof(Native.WNDCLASSEXW),
                lpfnWndProc = &WndProc,
                hInstance = instance,
                lpszClassName = _className,
            };
            Native.RegisterClassEx(&wc);
        }
        const uint WS_EX_TOOLWINDOW = 0x80;
        _hwnd = Native.CreateWindowEx(WS_EX_TOOLWINDOW, _className, _className, 0, 0, 0, 0, 0, 0, 0, instance, 0);
        _icon = Native.LoadImage(0, iconPath, Native.IMAGE_ICON,
            Native.GetSystemMetrics(Native.SM_CXSMICON), Native.GetSystemMetrics(Native.SM_CYSMICON), Native.LR_LOADFROMFILE);
        AddIcon();
        HotkeyRegistered = Native.RegisterHotKey(_hwnd, HotkeyPalette, Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT, Native.VK_SPACE);
    }

    private void AddIcon()
    {
        var data = NewData(Native.NIF_MESSAGE | Native.NIF_ICON | Native.NIF_TIP | Native.NIF_SHOWTIP);
        Native.Fill(data.szTip, 128, Strings.L("tray_tooltip"));
        _added = Native.Shell_NotifyIcon(Native.NIM_ADD, &data);
    }

    private Native.NOTIFYICONDATAW NewData(uint flags) => new()
    {
        cbSize = (uint)sizeof(Native.NOTIFYICONDATAW),
        hWnd = _hwnd,
        uID = 1,
        uFlags = flags,
        uCallbackMessage = CallbackMessage,
        hIcon = _icon,
    };

    /// <summary>A notification balloon from the tray icon (Windows shows it as a toast).</summary>
    public void Notify(string title, string text)
    {
        var data = NewData(Native.NIF_INFO);
        Native.Fill(data.szInfoTitle, 64, title);
        Native.Fill(data.szInfo, 256, text);
        data.dwInfoFlags = Native.NIIF_INFO | Native.NIIF_NOSOUND;
        Native.Shell_NotifyIcon(Native.NIM_MODIFY, &data);
    }

    private void ShowMenu()
    {
        var menu = Native.CreatePopupMenu();
        Native.AppendMenu(menu, Native.MF_STRING, MenuOpen, Strings.L("tray_open_palette") + "\tCtrl+Alt+Space");
        Native.AppendMenu(menu, Native.MF_SEPARATOR, 0, null);
        // updates (Updater.cs): only in an installed Release build
        if (_updater is { Available: true } updater)
        {
            if (updater.ReadyVersion is { } ready)
            {
                Native.AppendMenu(menu, Native.MF_STRING, MenuInstallUpdate, Strings.L("upd_install_version", ready));
            }
            Native.AppendMenu(menu, Native.MF_STRING, MenuCheckUpdates, Strings.L("upd_check"));
            Native.AppendMenu(menu, updater.AutoCheck ? Native.MF_CHECKED : Native.MF_STRING, MenuAutoUpdate, Strings.L("upd_auto"));
            Native.AppendMenu(menu, Native.MF_SEPARATOR, 0, null);
        }
        Native.AppendMenu(menu, Native.MF_STRING, MenuQuit, Strings.L("tray_quit"));
        Native.GetCursorPos(out var pt);
        // the menu closes when clicking elsewhere only if our window is in front (KB135788)
        Native.SetForegroundWindow(_hwnd);
        var cmd = Native.TrackPopupMenuEx(menu, Native.TPM_RIGHTBUTTON | Native.TPM_RETURNCMD | Native.TPM_BOTTOMALIGN, pt.X, pt.Y, _hwnd, 0);
        Native.PostMessage(_hwnd, Native.WM_NULL, 0, 0);
        Native.DestroyMenu(menu);
        if (cmd == MenuOpen) _openPalette();
        else if (cmd == MenuQuit) _quit();
        else if (cmd == MenuCheckUpdates) _ = _updater?.CheckAsync(manual: true);
        else if (cmd == MenuAutoUpdate && _updater is not null) _updater.AutoCheck = !_updater.AutoCheck;
        else if (cmd == MenuInstallUpdate) _updater?.InstallAndRestart();
    }

    private nint Handle(uint msg, nint wParam, nint lParam)
    {
        if (msg == Native.WM_HOTKEY && wParam == HotkeyPalette)
        {
            _openPalette();
            return 0;
        }
        if (msg == CallbackMessage)
        {
            var ev = (uint)(lParam & 0xFFFF);
            if (ev == Native.WM_LBUTTONUP) _openPalette();
            else if (ev is Native.WM_RBUTTONUP or Native.WM_CONTEXTMENU) ShowMenu();
            return 0;
        }
        if (msg == _taskbarCreated && _taskbarCreated != 0)
        {
            AddIcon();
            return 0;
        }
        return Native.DefWindowProc(_hwnd, msg, wParam, lParam);
    }

    [UnmanagedCallersOnly]
    private static nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        var host = _current;
        if (host is null || host._hwnd != hwnd) return Native.DefWindowProc(hwnd, msg, wParam, lParam);
        try
        {
            return host.Handle(msg, wParam, lParam);
        }
        catch (Exception e)
        {
            // an exception must never cross into native code
            System.Diagnostics.Debug.WriteLine($"Menew tray: {e}");
            return 0;
        }
    }

    public void Dispose()
    {
        if (HotkeyRegistered) Native.UnregisterHotKey(_hwnd, HotkeyPalette);
        if (_added)
        {
            var data = NewData(0);
            Native.Shell_NotifyIcon(Native.NIM_DELETE, &data);
            _added = false;
        }
        if (_icon != 0) Native.DestroyIcon(_icon);
        Native.DestroyWindow(_hwnd);
        if (_current == this) _current = null;
    }
}
