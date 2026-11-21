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

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Languages.Current = Languages.Detect();
        _palette = new PaletteWindow();
        var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "Menew.ico");
        _tray = new TrayHost(icon, () => _palette.Toggle(), Quit);
        if (!_tray.HotkeyRegistered)
        {
            _tray.Notify("Menew", Strings.L("hotkey_taken"));
        }
        LoadApps();
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

    private void Quit()
    {
        _tray?.Dispose();
        _tray = null;
        Exit();
    }
}
