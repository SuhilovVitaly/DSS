using System.Text.RegularExpressions;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using static DeepSpaceSaga.Client.Tests.CombatReportMailboxTests;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportFormatterTests
{
    [Fact]
    public void LocalesHaveEqualKeysAndPlaceholders()
    {
        var en = Localization.LoadLocaleFile("English")!.Where(k => k.Key.StartsWith("CombatReports.")).ToDictionary();
        var ru = Localization.LoadLocaleFile("Russian")!.Where(k => k.Key.StartsWith("CombatReports.")).ToDictionary();
        Assert.Equal(en.Keys.Order(), ru.Keys.Order());
        foreach (var pair in en)
            Assert.Equal(Regex.Matches(pair.Value, "\\{[^}]+\\}").Select(m => m.Value).Order(),
                Regex.Matches(ru[pair.Key], "\\{[^}]+\\}").Select(m => m.Value).Order());
    }
}
