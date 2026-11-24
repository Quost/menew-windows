using System.Globalization;

namespace Menew.Core;

/// <param name="Value">What is shown and copied.</param>
/// <param name="Detail">The calculation as understood.</param>
public sealed record CalcResult(string Value, string Detail);

/// <summary>
/// Math for the palette, port of the Mac's <c>Calculator.swift</c> (same as Linux
/// <c>calculator.rs</c>): "12*7,5", "20% de 340", "2(3+1)", "sqrt 16". Own evaluator: it never throws
/// on invalid input. Unit and currency conversions come in a later step (see docs/parity.md).
/// </summary>
public static class Calculator
{
    private static readonly (string Name, Func<double, double> Fn)[] Functions =
    [
        ("sqrt", Math.Sqrt),
        ("√", Math.Sqrt),
        ("raiz", Math.Sqrt),
        ("sin", Math.Sin),
        ("sen", Math.Sin),
        ("cos", Math.Cos),
        ("tan", Math.Tan),
        ("log", Math.Log10),
        ("ln", Math.Log),
        ("abs", Math.Abs),
        ("round", v => Math.Round(v, MidpointRounding.AwayFromZero)),
    ];

    /// <summary>Longer input is not a calculation: keeps a pasted wall of text from going through the parser.</summary>
    private const int MaxLength = 512;

    /// <summary><c>null</c> when the text is not a calculation (a lone number is not one either).</summary>
    public static CalcResult? Evaluate(string raw, Language lang)
    {
        var q = raw.Trim().ToLowerInvariant();
        if (q.Length == 0 || q.Length > MaxLength) return null;
        if (PercentOf(q, lang) is { } p) return p;
        var hasOp = q.Any(c => "+-*/^x×÷%()√!".Contains(c));
        var hasFn = Functions.Any(f => q.Contains(f.Name, StringComparison.Ordinal)) || q.Contains("pi", StringComparison.Ordinal) || q.Contains('π');
        if (!hasOp && !hasFn) return null;
        var v = new Parser(q).Parse();
        if (v is not { } value || !double.IsFinite(value)) return null;
        return new CalcResult(NumberFormat.Format(value, 10, lang), raw.Trim());
    }

    /// <summary>"20% de 340"</summary>
    private static CalcResult? PercentOf(string q, Language lang)
    {
        var parts = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || !parts[0].EndsWith('%') || !new[] { "de", "of", "von", "del", "的" }.Contains(parts[1])) return null;
        if (Number(parts[0][..^1]) is not { } p) return null;
        if (new Parser(string.Join(' ', parts[2..])).Parse() is not { } b) return null;
        return new CalcResult(NumberFormat.Format(p / 100 * b, 10, lang), q);
    }

    /// <summary>"1,5" and "1.5" are both 1.5.</summary>
    public static double? Number(string s)
    {
        var t = s.Replace(',', '.');
        if (t.Length == 0 || t.Contains("inf", StringComparison.Ordinal) || t.Contains("nan", StringComparison.Ordinal)) return null;
        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static Func<double, double>? Function(string name) =>
        Functions.FirstOrDefault(f => f.Name == name).Fn;

    /// <summary>expr = term (+|- term)* · term = power (*|/ power)* · power = unary (^ power)?</summary>
    private sealed class Parser(string s)
    {
        /// <summary>
        /// Nesting limit for the recursive rules ("(", unary signs, "^", functions). A stack overflow cannot be
        /// caught in .NET, so deeper input is rejected (null) instead.
        /// </summary>
        private const int MaxDepth = 200;

        private readonly string _c = s.Replace(" ", "", StringComparison.Ordinal);
        private int _i;
        private int _depth;

        private char? Peek => _i < _c.Length ? _c[_i] : null;

        public double? Parse()
        {
            var v = Expr();
            return v is not null && _i == _c.Length ? v : null;
        }

        private double? Expr()
        {
            if (Term() is not { } v) return null;
            while (Peek is '+' or '-')
            {
                var op = _c[_i++];
                var start = _i;
                if (Term() is not { } r) return null;
                // "200 + 10%" = 220, like calculators do
                var isPercent = _i > start && _c[_i - 1] == '%';
                var amount = isPercent ? v * r : r;
                v = op == '+' ? v + amount : v - amount;
            }
            return v;
        }

        private double? Term()
        {
            if (Power() is not { } v) return null;
            while (Peek is { } c)
            {
                if (c is '*' or '×' or 'x')
                {
                    _i++;
                    if (Power() is not { } r) return null;
                    v *= r;
                }
                else if (c is '/' or '÷')
                {
                    _i++;
                    if (Power() is not { } r) return null;
                    v /= r;
                }
                else if (c == '(' || char.IsLetter(c) || c == '√')
                {
                    // "2(3+1)", "2pi"
                    if (Power() is not { } r) return null;
                    v *= r;
                }
                else break;
            }
            return v;
        }

        private double? Power()
        {
            if (++_depth > MaxDepth) return null;
            try
            {
                if (Unary() is not { } b) return null;
                if (Peek == '^')
                {
                    _i++;
                    if (Power() is not { } e) return null;
                    return Math.Pow(b, e);
                }
                return b;
            }
            finally { _depth--; }
        }

        private double? Unary()
        {
            if (++_depth > MaxDepth) return null;
            try
            {
                switch (Peek)
                {
                    case '-':
                        _i++;
                        return -Unary();
                    case '+':
                        _i++;
                        return Unary();
                    default:
                        return Postfix();
                }
            }
            finally { _depth--; }
        }

        private double? Postfix()
        {
            if (Primary() is not { } v) return null;
            while (Peek is { } c)
            {
                if (c == '%')
                {
                    _i++;
                    v /= 100;
                }
                else if (c == '!')
                {
                    _i++;
                    if (v < 0 || v > 170 || v != Math.Round(v)) return null;
                    var n = Math.Max(1, (long)v);
                    double f = 1;
                    for (long k = 1; k <= n; k++) f *= k;
                    v = f;
                }
                else break;
            }
            return v;
        }

        private double? Primary()
        {
            if (++_depth > MaxDepth) return null;
            try { return PrimaryCore(); }
            finally { _depth--; }
        }

        private double? PrimaryCore()
        {
            if (Peek is not { } c) return null;
            if (c == '(')
            {
                _i++;
                var v = Expr();
                if (v is null || Peek != ')') return null;
                _i++;
                return v;
            }
            if (char.IsDigit(c) || c is '.' or ',')
            {
                var start = _i;
                while (Peek is { } d && (char.IsDigit(d) || d is '.' or ',')) _i++;
                return Number(_c[start.._i]);
            }
            if (c == '√')
            {
                _i++;
                return Primary() is { } a ? Math.Sqrt(a) : null;
            }
            if (char.IsLetter(c) || c == 'π')
            {
                var start = _i;
                while (Peek is { } d && (char.IsLetter(d) || d == 'π')) _i++;
                var name = _c[start.._i];
                if (name is "pi" or "π") return Math.PI;
                if (name == "e") return Math.E;
                if (Function(name) is not { } f) return null;
                return Primary() is { } arg ? f(arg) : null;
            }
            return null;
        }
    }
}
