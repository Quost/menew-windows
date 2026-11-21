namespace Menew.Core;

public enum ItemKind
{
    Calculator,
    App,
}

/// <summary>An installed app as the Start menu lists it.</summary>
/// <param name="Name">The name shown ("Microsoft Teams").</param>
/// <param name="Target">What is opened: a <c>.lnk</c>/<c>.url</c> path or a <c>shell:AppsFolder\…</c> id.</param>
/// <param name="Keywords">Extra words to match (the file name, the publisher...).</param>
public sealed record AppEntry(string Name, string Target, string Keywords = "");

/// <summary>One row of the palette.</summary>
/// <param name="Id">Stable id (<c>app:&lt;target&gt;</c>, <c>calc</c>), used later for recents.</param>
/// <param name="Value">For a calculator row, the text copied on Enter.</param>
public sealed record PaletteItem(string Id, ItemKind Kind, string Title, string Subtitle, string? Target = null, string? Value = null);

/// <summary>
/// The results for the typed text. First step of the port of <c>PaletteModel.update</c> in the Mac's
/// <c>Palette.swift</c> (Linux <c>palette.rs</c>): the calculator on top, then the apps by score, at
/// most <see cref="MaxResults"/>. Menew actions, conversions, time zones, recents and the rest of the
/// Mac palette come in the next steps (docs/plan.md), checked against docs/palette-spec.md.
/// </summary>
public static class Palette
{
    public const int MaxResults = 40;

    public static List<PaletteItem> Results(string raw, IReadOnlyList<AppEntry> apps, Language lang)
    {
        var items = new List<PaletteItem>();
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
        {
            // empty query: the apps in alphabetical order, like an app list
            items.AddRange(apps.OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase).Take(MaxResults).Select(a => AppItem(a, lang)));
            return items;
        }
        if (Calculator.Evaluate(trimmed, lang) is { } calc)
        {
            items.Add(new PaletteItem("calc", ItemKind.Calculator, calc.Value, $"{calc.Detail} · {Strings.Tr("kind_calc", lang)}", Value: calc.Value));
        }
        var q = Search.Fold(trimmed);
        var ranked = apps
            .Select(a => (App: a, Score: Search.Score(q, a.Name, a.Keywords)))
            .Where(x => x.Score is not null)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.App.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(MaxResults - items.Count)
            .Select(x => AppItem(x.App, lang));
        items.AddRange(ranked);
        return items;
    }

    private static PaletteItem AppItem(AppEntry a, Language lang) =>
        new($"app:{a.Target}", ItemKind.App, a.Name, Strings.Tr("kind_app", lang), Target: a.Target);
}
