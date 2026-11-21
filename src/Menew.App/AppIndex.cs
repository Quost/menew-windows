using System.Diagnostics;
using Menew.Core;
using Windows.Management.Deployment;

namespace Menew;

/// <summary>
/// The installed apps, like the Start menu: shortcuts in the Start menu folders (all users and the
/// current user) and the Store / packaged apps (Calculator, Settings, Teams...). The Mac reads
/// <c>/Applications</c>; Linux reads <c>.desktop</c> files.
/// </summary>
internal static class AppIndex
{
    private static readonly string[] Extensions = [".lnk", ".url", ".appref-ms"];

    // shortcuts that are not apps
    private static readonly string[] Skip = ["uninstall", "desinstalar", "deinstallieren", "卸载", "readme", "help", "documentation", "release notes"];

    public static List<AppEntry> Load()
    {
        var byName = new Dictionary<string, AppEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), Environment.GetFolderPath(Environment.SpecialFolder.Programs) })
        {
            foreach (var file in SafeFiles(root))
            {
                if (!Extensions.Contains(Path.GetExtension(file).ToLowerInvariant())) continue;
                var name = Path.GetFileNameWithoutExtension(file);
                var lower = name.ToLowerInvariant();
                if (Skip.Any(s => lower.Contains(s, StringComparison.Ordinal))) continue;
                var folder = Path.GetFileName(Path.GetDirectoryName(file)) ?? "";
                byName.TryAdd(name, new AppEntry(name, file, folder));
            }
        }
        foreach (var app in PackagedApps())
        {
            byName.TryAdd(app.Name, app);
        }
        return [.. byName.Values];
    }

    private static IEnumerable<string> SafeFiles(string root)
    {
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return [];
        try
        {
            return Directory.EnumerateFiles(root, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true });
        }
        catch (IOException)
        {
            return [];
        }
    }

    /// <summary>Apps from MSIX packages of the current user that show in the Start menu.</summary>
    private static List<AppEntry> PackagedApps()
    {
        var list = new List<AppEntry>();
        try
        {
            foreach (var pkg in new PackageManager().FindPackagesForUser(""))
            {
                if (pkg.IsFramework || pkg.IsResourcePackage || pkg.IsBundle) continue;
                try
                {
                    foreach (var entry in pkg.GetAppListEntries())
                    {
                        var name = entry.DisplayInfo.DisplayName;
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        list.Add(new AppEntry(name, $@"shell:AppsFolder\{entry.AppUserModelId}", pkg.Id.Name));
                    }
                }
                catch (Exception e) when (e is UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
                {
                    // some system packages can't be read: skip them
                }
            }
        }
        catch (Exception e) when (e is UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
            Debug.WriteLine($"Menew: packaged apps unavailable: {e.Message}");
        }
        return list;
    }

    /// <summary>Open an app the way the Start menu does.</summary>
    public static void Launch(string target)
    {
        var psi = target.StartsWith("shell:", StringComparison.OrdinalIgnoreCase)
            ? new ProcessStartInfo("explorer.exe", target) { UseShellExecute = true }
            : new ProcessStartInfo(target) { UseShellExecute = true };
        Process.Start(psi);
    }
}
