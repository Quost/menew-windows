using System.Globalization;

namespace Menew.Core;

/// <summary>Numbers as the Mac and Linux apps show them (port of Linux <c>format.rs</c>).</summary>
public static class NumberFormat
{
    /// <summary>
    /// At most <paramref name="digits"/> decimals, trailing zeros removed, the language's decimal
    /// separator. Very large or very small values use the <c>%g</c> style.
    /// </summary>
    public static string Format(double v, int digits, Language lang)
    {
        string s;
        if (v != 0 && (Math.Abs(v) >= 1e15 || Math.Abs(v) < 1e-9))
        {
            s = FormatG6(v);
        }
        else
        {
            s = v.ToString("F" + digits, CultureInfo.InvariantCulture);
            if (s.Contains('.')) s = s.TrimEnd('0').TrimEnd('.');
            if (s == "-0") s = "0";
        }
        return lang.DecimalSeparator() == '.' ? s : s.Replace('.', ',');
    }

    /// <summary>C's <c>%g</c>: 6 significant digits, exponent with a sign and two digits.</summary>
    internal static string FormatG6(double v)
    {
        var exp = (int)Math.Floor(Math.Log10(Math.Abs(v)));
        if (exp < -4 || exp >= 6)
        {
            var mantissa = v / Math.Pow(10, exp);
            if (Math.Abs(Math.Round(mantissa, 5)) >= 10) { mantissa /= 10; exp++; }
            var m = mantissa.ToString("F5", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            return $"{m}e{(exp < 0 ? '-' : '+')}{Math.Abs(exp):00}";
        }
        var decimals = Math.Max(0, 5 - exp);
        return v.ToString("F" + decimals, CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
    }
}
