using System.Globalization;

namespace Menew.Core;

/// <summary>The 5 languages of every Menew app: English, Portuguese, German, Spanish, Chinese.</summary>
public enum Language
{
    En,
    Pt,
    De,
    Es,
    Zh,
}

public static class Languages
{
    public static string Code(this Language lang) => lang switch
    {
        Language.Pt => "pt",
        Language.De => "de",
        Language.Es => "es",
        Language.Zh => "zh",
        _ => "en",
    };

    /// <summary>"pt-BR", "de_DE.UTF-8", "zh-Hans"... Anything else is English.</summary>
    public static Language FromCode(string? code)
    {
        var c = (code ?? "").Trim().ToLowerInvariant();
        if (c.StartsWith("pt", StringComparison.Ordinal)) return Language.Pt;
        if (c.StartsWith("de", StringComparison.Ordinal)) return Language.De;
        if (c.StartsWith("es", StringComparison.Ordinal)) return Language.Es;
        if (c.StartsWith("zh", StringComparison.Ordinal)) return Language.Zh;
        return Language.En;
    }

    /// <summary>The Windows display language (Settings › Time and language).</summary>
    public static Language Detect() => FromCode(CultureInfo.CurrentUICulture.Name);

    /// <summary>The language the UI uses now. Set once at start (or from Settings later).</summary>
    public static Language Current { get; set; } = Detect();

    /// <summary>Decimal separator: comma in Portuguese, German and Spanish, like the Mac app.</summary>
    public static char DecimalSeparator(this Language lang) =>
        lang is Language.Pt or Language.De or Language.Es ? ',' : '.';
}
