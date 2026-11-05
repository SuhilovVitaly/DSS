using System.Text.RegularExpressions;

namespace DeepSpaceSaga.Motion.Tests.TacticalBattle;

public sealed class ForbiddenCallsTests
{
    // Keep newlines/positions so diagnostics point to the original source line.
    private static readonly Regex NonCode = new("//[^\\r\\n]*|/\\*[\\s\\S]*?\\*/|@\"(?:\"\"|[^\"])*\"|\"(?:\\\\.|[^\"\\\\])*\"|'(?:\\\\.|[^'\\\\])*'", RegexOptions.CultureInvariant);
    private static readonly Regex Forbidden = new(@"\b(?:Math\s*\.\s*(?:Clamp|Min|Max|IEEERemainder)|Array\s*\.\s*Sort)\s*\(|\.\s*(?:Sort|Distinct|DistinctBy)\s*(?:<[^>]*>)?\s*\(|\bHashSet\s*<", RegexOptions.CultureInvariant);
    private static readonly Regex RoundedString = new("\\.\\s*ToString\\s*\\(\\s*\"[FN]0\"", RegexOptions.CultureInvariant);
    private static readonly Regex DoubleBefore = new(@"(?:\(\s*double\s*\)\s*\w+(?:\.\w+)*|\b\d+\.\d*(?:[eE][+-]?\d+)?|\b\d+[dD])\s*$", RegexOptions.CultureInvariant);
    private static readonly Regex DoubleAfter = new(@"^\s*(?:\(\s*double\s*\)|\d+\.\d*(?:[eE][+-]?\d+)?|\d+[dD])", RegexOptions.CultureInvariant);

    [Fact]
    public void ProductionTacticalBattleSourceUsesOnlyApprovedOperations()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DeepSpaceSaga.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var files = Directory.GetFiles(Path.Combine(dir!.FullName, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(p => p.Replace('\\', '/').Contains("/TacticalBattle/", StringComparison.Ordinal)
                && !p.Replace('\\', '/').Contains("/obj/", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(files);
        var findings = files.SelectMany(p => Find(File.ReadAllText(p)).Select(f => $"{Path.GetRelativePath(dir.FullName, p)}:{f.Line}: {f.Reason}"));
        Assert.Empty(findings);
    }

    [Theory]
    [InlineData("x = Math.Min(a, b);", "forbidden")]
    [InlineData("x = Math.Max(a, b);", "forbidden")]
    [InlineData("x = Math.Clamp(a, b, c);", "forbidden")]
    [InlineData("x = Math.IEEERemainder(a, b);", "forbidden")]
    [InlineData("items.Sort();", "forbidden")]
    [InlineData("Array.Sort(items);", "forbidden")]
    [InlineData("x = items.Distinct();", "forbidden")]
    [InlineData("x = new HashSet<string>();", "forbidden")]
    [InlineData("x = a.ToString(\"F0\");", "format")]
    [InlineData("x = a.ToString(\"N0\");", "format")]
    [InlineData("x = 1 / 2;", "division")]
    [InlineData("x = damage / count;", "division")]
    public void GuardFindsViolationsAndOriginalLine(string expression, string reason)
    {
        var finding = Assert.Single(Find("// heading\n" + expression));
        Assert.Equal(2, finding.Line);
        Assert.Contains(reason, finding.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("x = a / 2.0;")]
    [InlineData("x = (double)a / b;")]
    [InlineData("x = a / (double)(b - c);")]
    [InlineData("x = 1d / b;")]
    [InlineData("x = items.OrderBy(i => i.Key).ThenBy(i => i.Id);")]
    [InlineData("// Math.Min(a,b); 1 / 2\n/* items.Sort(); */ x = \"HashSet<int> 1 / 2\";")]
    public void GuardAllowsExplicitDoubleStableSortingAndNonCode(string expression) => Assert.Empty(Find(expression));

    private static IEnumerable<(int Line, string Reason)> Find(string source)
    {
        string code = NonCode.Replace(source, m => new string(m.Value.Select(c => c is '\n' or '\r' ? c : ' ').ToArray()));
        foreach (Match match in Forbidden.Matches(code)) yield return (Line(match.Index), "forbidden operation: " + match.Value);
        foreach (Match match in RoundedString.Matches(source))
            if (code.AsSpan(match.Index, 1)[0] == '.') yield return (Line(match.Index), "format must use explicit ToEven rounding");
        for (int i = 0; i < code.Length; i++)
            if (code[i] == '/' && !DoubleBefore.IsMatch(code[..i]) && !DoubleAfter.IsMatch(code[(i + 1)..]))
                yield return (Line(i), "division requires an explicit double operand");

        int Line(int offset) => 1 + source.AsSpan(0, offset).Count('\n');
    }
}
