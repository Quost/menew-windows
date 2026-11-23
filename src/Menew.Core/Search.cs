using System.Text;

namespace Menew.Core;

/// <summary>
/// Palette search, port of <c>PaletteSearch</c> in the Mac's <c>Palette.swift</c> (same as Linux
/// <c>search.rs</c>): case and diacritic insensitive, with the same scores so the same things come
/// out on top.
/// </summary>
public static class Search
{
    /// <summary>Lowercase and strip diacritics ("Ação" → "acao").</summary>
    public static string Fold(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s.ToLowerInvariant()) sb.Append(Strip(c));
        return sb.ToString();
    }

    public static char Strip(char c) => c switch
    {
        'à' or 'á' or 'â' or 'ã' or 'ä' or 'å' or 'ā' or 'ă' or 'ą' => 'a',
        'ç' or 'ć' or 'ĉ' or 'ċ' or 'č' => 'c',
        'ď' or 'đ' => 'd',
        'è' or 'é' or 'ê' or 'ë' or 'ē' or 'ĕ' or 'ė' or 'ę' or 'ě' => 'e',
        'ĝ' or 'ğ' or 'ġ' or 'ģ' => 'g',
        'ĥ' or 'ħ' => 'h',
        'ì' or 'í' or 'î' or 'ï' or 'ĩ' or 'ī' or 'ĭ' or 'į' or 'ı' => 'i',
        'ĵ' => 'j',
        'ķ' => 'k',
        'ĺ' or 'ļ' or 'ľ' or 'ŀ' or 'ł' => 'l',
        'ñ' or 'ń' or 'ņ' or 'ň' => 'n',
        'ò' or 'ó' or 'ô' or 'õ' or 'ö' or 'ø' or 'ō' or 'ŏ' or 'ő' => 'o',
        'ŕ' or 'ŗ' or 'ř' => 'r',
        'ś' or 'ŝ' or 'ş' or 'š' => 's',
        'ţ' or 'ť' or 'ŧ' => 't',
        'ù' or 'ú' or 'û' or 'ü' or 'ũ' or 'ū' or 'ŭ' or 'ů' or 'ű' or 'ų' => 'u',
        'ŵ' => 'w',
        'ý' or 'ÿ' or 'ŷ' => 'y',
        'ź' or 'ż' or 'ž' => 'z',
        _ => c,
    };

    /// <summary>Words of a text: runs of letters and digits.</summary>
    public static List<string> Words(string s)
    {
        var words = new List<string>();
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (sb.Length > 0) { words.Add(sb.ToString()); sb.Clear(); }
        }
        if (sb.Length > 0) words.Add(sb.ToString());
        return words;
    }

    private static bool IsSubsequence(string q, string t)
    {
        var i = 0;
        foreach (var c in t)
        {
            if (i < q.Length && q[i] == c) i++;
        }
        return i == q.Length;
    }

    /// <summary>
    /// <c>null</c> = no match. The higher, the closer to the top. <paramref name="query"/> must
    /// already be folded.
    /// </summary>
    public static int? Score(string query, string title, string keywords = "")
    {
        var t = Fold(title);
        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length > 1)
        {
            // "janela esquerda": every word must appear
            var hay = Words($"{t} {Fold(keywords)}");
            return tokens.All(tk => hay.Any(w => w.StartsWith(tk, StringComparison.Ordinal))) ? 500 : null;
        }
        var q = query;
        var tlen = t.Length;
        if (t == q) return 1000;
        if (t.StartsWith(q, StringComparison.Ordinal)) return 800 - tlen;
        var ws = Words(t);
        if (ws.Any(w => w.StartsWith(q, StringComparison.Ordinal))) return 650 - tlen;
        if (q.Length >= 2)
        {
            var initials = string.Concat(ws.Select(w => w[0]));
            if (initials.StartsWith(q, StringComparison.Ordinal)) return 600; // "mt" → Microsoft Teams
        }
        if (t.Contains(q, StringComparison.Ordinal)) return 450 - tlen;
        if (Words(Fold(keywords)).Any(w => w.StartsWith(q, StringComparison.Ordinal))) return 350;
        if (q.Length >= 3 && IsSubsequence(q, t)) return 150 - tlen;
        return null;
    }
}
