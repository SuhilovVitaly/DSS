using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

/// <summary>
/// EP-0034-US-0001-TK-0005: RU/EN texts of the passenger capacity panel and every stable reason code,
/// read from the shipped dictionaries copied to the test output.
/// </summary>
public sealed class DemoEp0034Us0001Tk0005Tests
{
    private const string Prefix = "Demo.Passengers.";

    internal static readonly string[] ReasonCodes =
    [
        PassengerCapacityReasons.NoFreeCabins,
        PassengerCapacityReasons.CabinsOvercommitted,
        PassengerCapacityReasons.InsufficientFoodRations,
        DemoCampaignReasons.Unavailable,
    ];

    internal static readonly string[] ScreenKeys = ["Title", "Summary", "Ready", "Empty", "Blocked"];

    private static Dictionary<string, string> Load(string language) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "Locale", language + ".json")))!;

    [Fact]
    public void StoryKeysExistInBothLocales()
    {
        var ru = Load("Russian");
        var en = Load("English");
        var expected = ScreenKeys.Concat(ReasonCodes).Select(k => Prefix + k).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, en.Keys.Where(k => k.StartsWith(Prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal));
        Assert.Equal(expected, ru.Keys.Where(k => k.StartsWith(Prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal));
        foreach (var key in expected)
        {
            Assert.False(string.IsNullOrWhiteSpace(ru[key]), key);
            Assert.False(string.IsNullOrWhiteSpace(en[key]), key);
        }
        Assert.Matches("[А-Яа-я]", ru[Prefix + "Title"]);
    }

    [Fact]
    public void FormatArgumentsMatch()
    {
        var ru = Load("Russian");
        var en = Load("English");
        foreach (var key in en.Keys.Where(k => k.StartsWith(Prefix, StringComparison.Ordinal)))
            Assert.Equal(Regex.Matches(en[key], @"\{\d+\}").Select(m => m.Value).Order(),
                Regex.Matches(ru[key], @"\{\d+\}").Select(m => m.Value).Order());
        // Summary formats five authoritative integers; Ready formats the free-cabin count.
        Assert.Equal(["{0}", "{1}", "{2}", "{3}", "{4}"], Regex.Matches(en[Prefix + "Summary"], @"\{\d+\}").Select(m => m.Value));
        Assert.Equal(["{0}"], Regex.Matches(en[Prefix + "Ready"], @"\{\d+\}").Select(m => m.Value));
        _ = string.Format(en[Prefix + "Summary"], 12, 6, 0, 2, 4);
        _ = string.Format(ru[Prefix + "Summary"], 12, 6, 0, 2, 4);
    }

    [Fact]
    public void NoRawReasonCodeLeaks()
    {
        foreach (var language in new[] { "Russian", "English" })
        {
            var dictionary = Load(language);
            foreach (var code in ReasonCodes)
            {
                string text = dictionary[Prefix + code];
                // "unavailable" is also an ordinary English word; snake_case codes must never appear verbatim.
                if (code.Contains('_'))
                    Assert.DoesNotContain(code, text, StringComparison.Ordinal);
                Assert.DoesNotContain("_", text, StringComparison.Ordinal);
                Assert.NotEqual(code, text);
                Assert.DoesNotContain("PassengerCapacity", text, StringComparison.Ordinal);
            }
        }
    }
}
