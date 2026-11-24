using Menew.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Menew;

/// <summary>
/// Menew runs in the notification area like it runs in the Mac menu bar: no window at start, the
/// palette opens with Ctrl+Alt+Space or a click on the tray icon, Quit is in the tray menu.
/// </summary>
public partial class App : Application
{
    private PaletteWindow? _palette;
    private TrayHost? _tray;
    private Updater? _updater;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Languages.Current = Languages.Detect();
        _updater = new Updater(text => _tray?.Notify("Menew", text), RemoveTrayIcon, AddTrayIcon);
        if (_updater.ApplyPendingAtStart()) return;
        _palette = new PaletteWindow();
        AddTrayIcon();
        LoadApps();
        _updater.Start();
    }

    /// <summary>The tray icon and hotkey; also brings them back when an update install could not start.</summary>
    private void AddTrayIcon()
    {
        if (_tray is not null || _palette is null) return;
        var palette = _palette;
        var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "Menew.ico");
        _tray = new TrayHost(icon, palette.Toggle, Quit, _updater);
        if (!_tray.HotkeyRegistered)
        {
            _tray.Notify("Menew", Strings.L("hotkey_taken"));
        }
    }

    /// <summary>A second launch of Menew.exe (Program.cs) opens the palette in this one.</summary>
    public void OnRedirected() => _palette?.ShowPalette();

    /// <summary>Scan the installed apps off the UI thread; the palette gets them when ready.</summary>
    private void LoadApps()
    {
        var queue = DispatcherQueue.GetForCurrentThread();
        Task.Run(() =>
        {
            var apps = AppIndex.Load();
            queue.TryEnqueue(() => _palette?.SetApps(apps));
        });
    }

    private void RemoveTrayIcon()
    {
        _tray?.Dispose();
        _tray = null;
    }

    private void Quit()
    {
        RemoveTrayIcon();
        _updater?.Dispose();
        Exit();
    }
}
