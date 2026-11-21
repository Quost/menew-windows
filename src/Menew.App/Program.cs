using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace Menew;

/// <summary>
/// Our own entry point (DISABLE_XAML_GENERATED_MAIN): Menew runs once. Launching Menew.exe again
/// (Start menu, a shortcut, autostart) hands over to the running copy, which opens the palette.
/// </summary>
public static class Program
{
    private const string InstanceKey = "app.menew.Menew";

    [STAThread]
    public static int Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        var instance = AppInstance.FindOrRegisterForKey(InstanceKey);
        if (!instance.IsCurrent)
        {
            var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
            // wait for the hand-over before this process exits
            instance.RedirectActivationToAsync(activation).AsTask().Wait();
            return 0;
        }
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            var app = new App();
            instance.Activated += (_, _) => context.Post(_ => app.OnRedirected(), null);
        });
        return 0;
    }
}
