using Menew.Core;

namespace Menew.Core.Tests;

public class SearchTests
{
    [Fact]
    public void FoldStripsCaseAndAccents()
    {
        Assert.Equal("acao umlaut", Search.Fold("Ação Ümlaut"));
        Assert.Equal("cafe", Search.Fold("CAFÉ"));
    }

    // the same vectors as Linux search.rs, which follow PaletteSearch.score on the Mac
    [Theory]
    [InlineData("firefox", "Firefox", "", 1000)]
    [InlineData("fire", "Firefox", "", 800 - 7)]
    [InlineData("teams", "Microsoft Teams", "", 650 - 15)]
    [InlineData("mt", "Microsoft Teams", "", 600)]
    [InlineData("soft", "Microsoft Teams", "", 450 - 15)]
    [InlineData("zzz", "Microsoft Teams", "zzzword", 350)]
    [InlineData("mcs", "Microsoft Teams", "", 150 - 15)]
    [InlineData("janela esq", "Janela", "esquerda", 500)]
    public void ScoresFollowTheMacRules(string q, string title, string kw, int expected) =>
        Assert.Equal(expected, Search.Score(q, title, kw));

    [Theory]
    [InlineData("qq", "Microsoft Teams", "")]
    [InlineData("janela dir", "Janela", "esquerda")]
    public void NoMatch(string q, string title, string kw) => Assert.Null(Search.Score(q, title, kw));
}

public class CalculatorTests
{
    private static string? Ev(string q, Language lang = Language.En) => Calculator.Evaluate(q, lang)?.Value;

    [Theory]
    [InlineData("12*7.5", "90")]
    [InlineData("100 / 4 + 15", "40")]
    [InlineData("2 ^ 8", "256")]
    [InlineData("(2 + 3) * 4", "20")]
    [InlineData("2(3+1)", "8")]
    [InlineData("200 + 10%", "220")]
    [InlineData("20% of 340", "68")]
    [InlineData("sqrt 16", "4")]
    [InlineData("5!", "120")]
    [InlineData("1/3", "0.3333333333")]
    public void PlainExpressions(string q, string expected) => Assert.Equal(expected, Ev(q));

    [Fact]
    public void CommaLanguagesUseADecimalComma()
    {
        Assert.Equal("1,5", Ev("3/2", Language.Pt));
        Assert.Equal("68", Ev("20% de 340", Language.Pt));
        Assert.Equal("9", Ev("12*0,75", Language.De));
    }

    [Theory]
    [InlineData("42")]
    [InlineData("firefox")]
    [InlineData("")]
    public void ALoneNumberOrWordIsNotACalculation(string q) => Assert.Null(Ev(q));

    [Theory]
    [InlineData("1/0")]
    [InlineData("(1+")]
    [InlineData("2*")]
    [InlineData("171!")]
    [InlineData("sqrt")]
    public void InvalidInputNeverThrows(string q) => Assert.Null(Ev(q));

    [Fact]
    public void PastedOrDeeplyNestedInputIsRejectedWithoutOverflowingTheStack()
    {
        // Too long: rejected before parsing.
        Assert.Null(Ev(new string('(', 100_000)));
        Assert.Null(Ev(new string('-', 100_000) + "1"));
        Assert.Null(Ev("1+" + string.Concat(Enumerable.Repeat("1+", 300)) + "1"));
        // Short enough, but nested past the parser's depth limit.
        Assert.Null(Ev(new string('(', 250) + "1" + new string(')', 250)));
        Assert.Null(Ev(new string('-', 400) + "1"));
        Assert.Null(Ev(string.Concat(Enumerable.Repeat("1^", 150)) + "1"));
        Assert.Null(Ev(new string('√', 300) + "1"));
    }

    [Fact]
    public void DeepButReasonableNestingStillWorks()
    {
        Assert.Equal("2", Ev(new string('(', 50) + "1+1" + new string(')', 50)));
        Assert.Equal("1", Ev(new string('-', 100) + "1"));
        Assert.Equal("1", Ev(string.Concat(Enumerable.Repeat("1^", 40)) + "1"));
    }

    [Theory]
    [InlineData(1e20, "1e+20")]
    [InlineData(1.5e-12, "1.5e-12")]
    [InlineData(123456789012345678.0, "1.23457e+17")]
    public void HugeAndTinyValuesUseTheGStyle(double v, string expected) =>
        Assert.Equal(expected, NumberFormat.Format(v, 10, Language.En));
}

public class PaletteTests
{
    private static readonly AppEntry[] Apps =
    [
        new("Firefox", @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Firefox.lnk"),
        new("Microsoft Teams", @"shell:AppsFolder\MSTeams_8wekyb3d8bbwe!MSTeams"),
        new("Calculator", @"shell:AppsFolder\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"),
    ];

    [Fact]
    public void TheCalculationComesFirst()
    {
        var r = Palette.Results("2+2", Apps, Language.En);
        Assert.Equal(ItemKind.Calculator, r[0].Kind);
        Assert.Equal("4", r[0].Value);
    }

    [Fact]
    public void AppsAreRankedByScore()
    {
        var r = Palette.Results("mt", Apps, Language.En);
        Assert.Equal("Microsoft Teams", r[0].Title);
        Assert.Single(r);
    }

    [Fact]
    public void EmptyQueryListsTheAppsAlphabetically()
    {
        var r = Palette.Results("", Apps, Language.En);
        Assert.Equal(["Calculator", "Firefox", "Microsoft Teams"], r.Select(i => i.Title));
    }
}

public class StringsTests
{
    [Fact]
    public void EveryKeyIsFilledInAll5LanguagesWithoutEmDash()
    {
        foreach (var (key, texts) in Strings.Table)
        {
            Assert.True(texts.Length == 5, $"{key}: {texts.Length} languages");
            foreach (var t in texts)
            {
                Assert.False(string.IsNullOrWhiteSpace(t), $"{key}: empty text");
                Assert.DoesNotContain('\u2014', t);
            }
            var holes = texts.Select(t => t.Split("{}").Length).Distinct().Count();
            Assert.True(holes == 1, $"{key}: the languages have different placeholders");
        }
    }

    [Fact]
    public void KeysAreUnique() =>
        Assert.Equal(Strings.Table.Length, Strings.Table.Select(r => r.Key).Distinct().Count());

    [Fact]
    public void PlaceholdersAreFilled() =>
        Assert.Equal("Copiado: 42", Strings.Tr("copied_value", Language.Pt, "42"));

    [Theory]
    [InlineData("pt-BR", Language.Pt)]
    [InlineData("de-DE", Language.De)]
    [InlineData("es-419", Language.Es)]
    [InlineData("zh-Hans-CN", Language.Zh)]
    [InlineData("fr-FR", Language.En)]
    public void LanguageFollowsTheCultureName(string code, Language expected) =>
        Assert.Equal(expected, Languages.FromCode(code));
}
