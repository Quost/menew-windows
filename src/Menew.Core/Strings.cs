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
        // ---- updates (Updater.swift; Linux updates.rs) ----
        ("upd_check", ["Check for Updates…", "Procurar atualizações…", "Nach Updates suchen…", "Buscar actualizaciones…", "检查更新…"]),
        ("upd_auto", ["Check for updates automatically", "Procurar atualizações automaticamente", "Automatisch nach Updates suchen", "Buscar actualizaciones automáticamente", "自动检查更新"]),
        ("upd_uptodate", ["Menew is up to date ({})", "O Menew está atualizado ({})", "Menew ist auf dem neuesten Stand ({})", "Menew está al día ({})", "Menew 已是最新版本（{}）"]),
        ("upd_check_failed", ["Could not check for updates", "Não foi possível procurar atualizações", "Die Suche nach Updates ist fehlgeschlagen", "No se pudo buscar actualizaciones", "无法检查更新"]),
        ("upd_downloading", ["Downloading Menew {}…", "Baixando o Menew {}…", "Menew {} wird geladen …", "Descargando Menew {}…", "正在下载 Menew {}…"]),
        ("upd_ready", ["Menew {} is ready. It installs the next time Menew starts, or now from the tray menu.", "O Menew {} está pronto. Ele é instalado na próxima vez que o Menew abrir, ou agora pelo menu da bandeja.", "Menew {} ist bereit. Es wird beim nächsten Start von Menew installiert oder jetzt über das Menü im Infobereich.", "Menew {} está listo. Se instala la próxima vez que se abra Menew, o ahora desde el menú de la bandeja.", "Menew {} 已准备就绪。将在下次启动 Menew 时安装，也可以现在从托盘菜单安装。"]),
        ("upd_install_version", ["Install Menew {} and Relaunch", "Instalar o Menew {} e reabrir", "Menew {} installieren und neu starten", "Instalar Menew {} y reabrir", "安装 Menew {} 并重新打开"]),
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
