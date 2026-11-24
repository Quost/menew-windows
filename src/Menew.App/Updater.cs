using Menew.Core;
using Microsoft.UI.Dispatching;
using Velopack;
using Velopack.Sources;

namespace Menew;

/// <summary>
/// Updates, like Sparkle on the Mac and the Linux updater: a quiet check 5 minutes after start and then once a
/// day, against the GitHub releases of this repo (Velopack feed files published by <c>release.yml</c>). A found
/// update is downloaded in the background and installed the next time Menew starts (<see cref="ApplyPendingAtStart"/>),
/// or right away from the tray menu ("Install Menew X and Relaunch"). A scheduled check
/// never opens a window; it only notifies once when the update is ready ("gentle reminder" on the Mac).
///
/// Off in Debug builds and when Menew runs from a build folder instead of an installation (no network at all
/// then). The automatic check is opt-out from the tray menu; a manual check is always possible.
/// </summary>
internal sealed class Updater : IDisposable
{
    public const string RepoUrl = "https://github.com/Quost/menew-windows";
    private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromDays(1);

    private readonly UpdateManager? _manager;
    private readonly DispatcherQueue _queue;
    private readonly Action<string> _notify;
    private readonly Action _beforeRestart;
    private readonly Action _restartFailed;
    private readonly string _optOutFile;
    private readonly string _attemptFile;
    private readonly SemaphoreSlim _busy = new(1, 1);
    private Timer? _timer;
    // set by the background check, read on the UI thread
    private volatile VelopackAsset? _ready;

    /// <param name="notify">Shows a tray notification (called on the UI thread).</param>
    /// <param name="beforeRestart">Cleans up (tray icon) before Velopack exits the process to install.</param>
    /// <param name="restartFailed">Undoes <paramref name="beforeRestart"/> (brings the tray icon back) when the install could not start.</param>
    public Updater(Action<string> notify, Action beforeRestart, Action restartFailed)
    {
        _queue = DispatcherQueue.GetForCurrentThread();
        _notify = notify;
        _beforeRestart = beforeRestart;
        _restartFailed = restartFailed;
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Menew");
        _optOutFile = Path.Combine(dir, "no-auto-update");
        _attemptFile = Path.Combine(dir, "update-attempt");
        _manager = CreateManager();
        _ready = _manager?.UpdatePendingRestart;
    }

    private static UpdateManager? CreateManager()
    {
#if DEBUG
        return null;
#else
        try
        {
            // the installed version decides the feed: a pre-release follows pre-releases too (like Linux)
            var probe = new UpdateManager(new GithubSource(RepoUrl, null, false));
            if (!probe.IsInstalled) return null;
            return probe.CurrentVersion?.IsPrerelease == true ? new UpdateManager(new GithubSource(RepoUrl, null, true)) : probe;
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Menew updater: {e}");
            return null;
        }
#endif
    }

    /// <summary>False in Debug builds and outside an installation: the tray shows no update items.</summary>
    public bool Available => _manager is not null;

    /// <summary>The version downloaded and waiting for a restart, if any.</summary>
    public string? ReadyVersion => _ready?.Version?.ToString();

    public bool AutoCheck
    {
        get => !File.Exists(_optOutFile);
        set
        {
            try
            {
                if (value) File.Delete(_optOutFile);
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_optOutFile)!);
                    File.WriteAllText(_optOutFile, "");
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            Schedule();
        }
    }

    public void Start() => Schedule();

    /// <summary>
    /// At start, before the tray icon appears: installs an update downloaded by the previous run and relaunches.
    /// Done here and not by Velopack's own auto-apply in <c>VelopackApp.Run</c>, because a second launch of
    /// Menew.exe (which only hands over to the running copy) must not start an install. Tried once per version:
    /// if it did not take, the update stays in the tray menu instead of looping on every start.
    /// </summary>
    public bool ApplyPendingAtStart()
    {
        var ready = _ready;
        var version = ready?.Version?.ToString();
        if (_manager is null || ready is null || version is null) return false;
        try
        {
            if (File.Exists(_attemptFile) && File.ReadAllText(_attemptFile) == version) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(_attemptFile)!);
            File.WriteAllText(_attemptFile, version);
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        try
        {
            _manager.ApplyUpdatesAndRestart(ready);
        }
        catch (Exception e)
        {
            // start normally; the update stays in the tray menu
            System.Diagnostics.Debug.WriteLine($"Menew updater: {e}");
            return false;
        }
        return true;
    }

    private void Schedule()
    {
        _timer?.Dispose();
        _timer = null;
        if (!Available || !AutoCheck) return;
        _timer = new Timer(_ => _ = CheckAsync(manual: false), null, FirstCheckDelay, CheckInterval);
    }

    /// <summary>Checks and downloads. A manual check says what happened; a scheduled one only says "ready".</summary>
    public async Task CheckAsync(bool manual)
    {
        if (_manager is null) return;
        if (_ready is not null)
        {
            if (manual) Post(Strings.L("upd_ready", ReadyVersion ?? ""));
            return;
        }
        if (!await _busy.WaitAsync(0).ConfigureAwait(false)) return;
        try
        {
            var update = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (update is null)
            {
                if (manual) Post(Strings.L("upd_uptodate", _manager.CurrentVersion?.ToString() ?? ""));
                return;
            }
            var version = update.TargetFullRelease.Version.ToString();
            if (manual) Post(Strings.L("upd_downloading", version));
            await _manager.DownloadUpdatesAsync(update).ConfigureAwait(false);
            _ready = update.TargetFullRelease;
            Post(Strings.L("upd_ready", version));
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Menew updater: {e}");
            if (manual) Post(Strings.L("upd_check_failed"));
        }
        finally
        {
            _busy.Release();
        }
    }

    /// <summary>Exits, installs the downloaded update and starts the new Menew (tray menu).</summary>
    public void InstallAndRestart()
    {
        var ready = _ready;
        if (_manager is null || ready is null) return;
        _beforeRestart();
        try
        {
            _manager.ApplyUpdatesAndRestart(ready);
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Menew updater: {e}");
            // queued, not inline: this runs inside the (now destroyed) tray window's menu handler
            _queue.TryEnqueue(() => _restartFailed());
            Post(Strings.L("upd_check_failed"));
        }
    }

    private void Post(string text) => _queue.TryEnqueue(() => _notify(text));

    public void Dispose()
    {
        _timer?.Dispose();
        _busy.Dispose();
    }
}
