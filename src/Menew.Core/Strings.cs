namespace Menew.Core;

/// <summary>
/// Every UI string, in the same 5 columns as Menew for Linux (<c>strings.rs</c>): en, pt, de, es, zh.
/// Texts follow the Mac app (<c>L10n.swift</c>) where it has them; keys follow the Linux table so the
/// three apps can be compared. A missing key shows the key itself, and a test fails.
/// </summary>
public static class Strings
{
    internal static readonly (string Key, string[] Texts)[] Table =
    [
        // ---- palette (Palette.swift) ----
        ("search_placeholder", ["Search apps, do math…", "Buscar apps, fazer contas…", "Apps suchen, rechnen…", "Buscar apps, hacer cálculos…", "搜索应用、计算…"]),
        ("kind_calc", ["Calculator", "Calculadora", "Rechner", "Calculadora", "计算器"]),
        ("kind_app", ["App", "App", "App", "App", "应用"]),
        ("nothing_found", ["Nothing found for “{}”", "Nada encontrado para “{}”", "Nichts gefunden für „{}“", "No se encontró nada para «{}»", "未找到 “{}”"]),
        ("copied_value", ["Copied: {}", "Copiado: {}", "Kopiert: {}", "Copiado: {}", "已复制：{}"]),
        ("hint_open", ["↩ open", "↩ abrir", "↩ öffnen", "↩ abrir", "↩ 打开"]),
        ("hint_copy", ["↩ copy", "↩ copiar", "↩ kopieren", "↩ copiar", "↩ 复制"]),
        // ---- tray (MenuBar / App.swift) ----
        ("tray_tooltip", ["Menew", "Menew", "Menew", "Menew", "Menew"]),
        ("tray_open_palette", ["Open palette", "Abrir paleta", "Palette öffnen", "Abrir paleta", "打开命令面板"]),
        ("tray_quit", ["Quit Menew", "Sair do Menew", "Menew beenden", "Salir de Menew", "退出 Menew"]),
        ("hotkey_taken", ["Ctrl+Alt+Space is used by another app. Open the palette from the tray icon.", "Ctrl+Alt+Espaço já é usado por outro app. Abra a paleta pelo ícone da bandeja.", "Strg+Alt+Leertaste wird von einer anderen App verwendet. Öffne die Palette über das Symbol im Infobereich.", "Ctrl+Alt+Espacio lo usa otra app. Abre la paleta desde el icono de la bandeja.", "Ctrl+Alt+空格 已被其他应用占用。请从托盘图标打开命令面板。"]),
    ];

    private static readonly Dictionary<string, string[]> ByKey = Table.ToDictionary(r => r.Key, r => r.Texts);

    /// <summary>Translation of <paramref name="key"/> with <c>{}</c> placeholders filled in order.</summary>
    public static string Tr(string key, Language lang, params string[] args)
    {
        if (!ByKey.TryGetValue(key, out var texts)) return key;
        var text = texts[(int)lang];
        foreach (var a in args)
        {
            var i = text.IndexOf("{}", StringComparison.Ordinal);
            if (i < 0) break;
            text = string.Concat(text.AsSpan(0, i), a, text.AsSpan(i + 2));
        }
        return text;
    }

    /// <summary>In the current UI language.</summary>
    public static string L(string key, params string[] args) => Tr(key, Languages.Current, args);
}
