using Menew.Core;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.System;

namespace Menew;

/// <summary>One row of the list (x:Bind needs a type of this assembly).</summary>
public sealed partial class PaletteRow(PaletteItem item)
{
    public PaletteItem Item { get; } = item;
    public string Title => Item.Title;
    public string Subtitle => Item.Subtitle;

    // Segoe Fluent Icons
    public string Glyph => Item.Kind switch
    {
        ItemKind.Calculator => "",
        _ => "",
    };

    // the name screen readers announce for the row (UIA falls back to ToString for data items)
    public override string ToString() => string.IsNullOrEmpty(Subtitle) ? Title : $"{Title}, {Subtitle}";
}

/// <summary>
/// The command palette (the Mac's <c>Palette.swift</c> window): a floating, borderless, always on
/// top window with acrylic, centered on the display under the mouse, hidden when it loses focus.
/// </summary>
public sealed partial class PaletteWindow : Window
{
    private const int WidthDip = 680, HeightDip = 440;

    private IReadOnlyList<AppEntry> _apps = [];

    public PaletteWindow()
    {
        InitializeComponent();
        Query.PlaceholderText = Strings.L("search_placeholder");
        AutomationProperties.SetName(Query, Strings.L("search_placeholder"));
        ExtendsContentIntoTitleBar = true;

        var presenter = OverlappedPresenter.Create();
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        presenter.SetBorderAndTitleBar(true, false);
        AppWindow.SetPresenter(presenter);
        AppWindow.IsShownInSwitchers = false;
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Menew.ico"));

        // Alt+F4 or the system closing it only hides the palette; Quit is in the tray
        AppWindow.Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };
        Activated += (_, e) =>
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated) Hide();
        };
    }

    public void SetApps(IReadOnlyList<AppEntry> apps)
    {
        _apps = apps;
        if (AppWindow.IsVisible) Refresh();
    }

    public void Toggle()
    {
        if (AppWindow.IsVisible) Hide();
        else ShowPalette();
    }

    public void ShowPalette()
    {
        PlaceOnActiveDisplay();
        Query.Text = "";
        Refresh();
        AppWindow.Show(true);
        Activate();
        Native.SetForegroundWindow(Win32Interop.GetWindowFromWindowId(AppWindow.Id));
        Query.Focus(FocusState.Programmatic);
    }

    public void Hide() => AppWindow.Hide();

    /// <summary>Centered horizontally, a quarter down, on the display with the mouse (like the Mac).</summary>
    private void PlaceOnActiveDisplay()
    {
        Native.GetCursorPos(out var pt);
        var area = DisplayArea.GetFromPoint(new PointInt32(pt.X, pt.Y), DisplayAreaFallback.Primary).WorkArea;
        var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        var scale = Native.GetDpiForWindow(hwnd) / 96.0;
        var w = (int)(WidthDip * scale);
        var h = (int)(HeightDip * scale);
        AppWindow.MoveAndResize(new RectInt32(area.X + (area.Width - w) / 2, area.Y + area.Height / 4, w, h));
    }

    private void Refresh()
    {
        var items = Palette.Results(Query.Text, _apps, Languages.Current);
        Results.ItemsSource = items.Select(i => new PaletteRow(i)).ToList();
        if (items.Count > 0) Results.SelectedIndex = 0;
        UpdateFooter();
    }

    private void UpdateFooter()
    {
        var row = Results.SelectedItem as PaletteRow;
        var text = row is null
            ? (Query.Text.Trim().Length > 0 ? Strings.L("nothing_found", Query.Text.Trim()) : "")
            : Strings.L(row.Item.Kind == ItemKind.Calculator ? "hint_copy" : "hint_open");
        if (Footer.Text == text) return;
        Footer.Text = text;
        // the footer is a polite live region (PaletteWindow.xaml): tell screen readers it changed
        FrameworkElementAutomationPeer.FromElement(Footer)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    private void OnQueryChanged(object sender, TextChangedEventArgs e) => Refresh();

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PaletteRow row) Run(row.Item);
    }

    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var count = Results.Items.Count;
        switch (e.Key)
        {
            case VirtualKey.Escape:
                Hide();
                e.Handled = true;
                break;
            case VirtualKey.Down when count > 0:
                Results.SelectedIndex = (Results.SelectedIndex + 1) % count;
                Results.ScrollIntoView(Results.SelectedItem);
                UpdateFooter();
                e.Handled = true;
                break;
            case VirtualKey.Up when count > 0:
                Results.SelectedIndex = (Results.SelectedIndex - 1 + count) % count;
                Results.ScrollIntoView(Results.SelectedItem);
                UpdateFooter();
                e.Handled = true;
                break;
            case VirtualKey.Enter:
                if (Results.SelectedItem is PaletteRow row) Run(row.Item);
                e.Handled = true;
                break;
        }
    }

    private void Run(PaletteItem item)
    {
        Hide();
        switch (item.Kind)
        {
            case ItemKind.Calculator when item.Value is { } value:
                CopyToClipboard(value);
                break;
            case ItemKind.App when item.Target is { } target:
                try
                {
                    AppIndex.Launch(target);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Menew: could not open {target}: {ex.Message}");
                }
                break;
        }
    }

    /// <summary>
    /// The clipboard can be held open by another app for a moment (CLIPBRD_E_CANT_OPEN): try a few times,
    /// then give up quietly instead of crashing.
    /// </summary>
    private static void CopyToClipboard(string value)
    {
        const int Attempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var data = new DataPackage();
                data.SetText(value);
                Clipboard.SetContent(data);
                Clipboard.Flush();
                return;
            }
            catch (COMException ex) when (attempt < Attempts)
            {
                System.Diagnostics.Debug.WriteLine($"Menew: clipboard busy ({ex.HResult:X8}), retrying");
                Thread.Sleep(50);
            }
            catch (COMException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Menew: could not copy to the clipboard: {ex.Message}");
                return;
            }
        }
    }
}
