using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

/// <summary>
/// EP-0034-US-0004-TK-0004: RU/EN texts of cargo contracts — the hold split (sealed contract cargo, own goods, free
/// space), each contract line with item, route, payout and deadline, every hold state, the confirmed sealing, delivery
/// and return results, the "not for sale" rule and the empty/unavailable states — read from the shipped dictionaries.
/// Acceptance refusals and cancellation reuse the board (US-0006) and failure (US-0003) texts.
/// </summary>
public sealed class DemoEp0034Us0004Tk0004Tests
{
    internal const string Prefix = "Demo.CargoContracts.";

    internal static readonly string[] States =
    [
        CargoHoldStates.Aboard, CargoHoldStates.Overdue, CargoHoldStates.Delivered, CargoHoldStates.ReturnPending,
        CargoHoldStates.Returned, CargoHoldStates.Lost, DemoCampaignReasons.Unavailable,
    ];

    /// <summary>Screen keys with the number of format parameters the Contracts screen supplies.</summary>
    internal static readonly Dictionary<string, int> ScreenKeys = new()
    {
        ["Title"] = 0, ["Empty"] = 0, ["Unavailable"] = 0, ["Hold"] = 3, ["NotForSale"] = 0, ["Line"] = 7, ["Sealed"] = 4,
        ["Delivered"] = 2, ["Returned"] = 2,
    };

    internal static IEnumerable<string> AllKeys() =>
        ScreenKeys.Keys.Concat(States.Select(s => "State." + s)).Select(k => Prefix + k);

    private static Dictionary<string, string> Load(string language) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "Locale", language + ".json")))!;

    [Fact]
    public void StoryKeysExistInBothLocales()
    {
        var ru = Load("Russian");
        var en = Load("English");
        var expected = AllKeys().Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, en.Keys.Where(k => k.StartsWith(Prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal));
        Assert.Equal(expected, ru.Keys.Where(k => k.StartsWith(Prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal));
        foreach (var key in expected)
        {
            Assert.False(string.IsNullOrWhiteSpace(ru[key]), key);
            Assert.False(string.IsNullOrWhiteSpace(en[key]), key);
            Assert.Matches("[А-Яа-я]", ru[key]);
        }
        // Refusals of cargo acceptance and cancellation are the shared board/failure texts.
        foreach (var dictionary in new[] { ru, en })
        {
            Assert.True(dictionary.ContainsKey("Demo.Jobs.Reason." + StationJobReasons.InsufficientCargoSpace));
            Assert.True(dictionary.ContainsKey("Demo.Jobs.Type." + StationJobTypes.Cargo));
            Assert.True(dictionary.ContainsKey("Demo.TransportFailures.Reason." + TransportFailureReasons.ContractNotActive));
        }
    }

    [Fact]
    public void FormatArgumentsMatch()
    {
        static string[] Args(string text) => Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().Order().ToArray();
        foreach (var dictionary in new[] { Load("English"), Load("Russian") })
        {
            foreach (var key in AllKeys())
            {
                int count = ScreenKeys.GetValueOrDefault(key[Prefix.Length..]);
                Assert.Equal(Enumerable.Range(0, count).Select(i => "{" + i + "}"), Args(dictionary[key]));
            }
            _ = string.Format(dictionary[Prefix + "Hold"], "6", "3.2", "90.8");
            _ = string.Format(dictionary[Prefix + "Line"], "6", "Hull Alloy", "Industrial", "Mining", 640, "day 1, 13:30", "aboard");
            _ = string.Format(dictionary[Prefix + "Sealed"], "6", "Hull Alloy", "Mining", "day 1, 13:30");
            _ = string.Format(dictionary[Prefix + "Delivered"], "day 1, 09:10", 640);
            _ = string.Format(dictionary[Prefix + "Returned"], "Mining", "day 1, 15:00");
        }
    }

    [Fact]
    public void NoRawReasonCodeLeaks()
    {
        foreach (var language in new[] { "Russian", "English" })
        {
            var dictionary = Load(language);
            foreach (var key in AllKeys())
            {
                string text = dictionary[key];
                Assert.DoesNotContain("_", text, StringComparison.Ordinal);
                Assert.DoesNotContain("contract:", text, StringComparison.Ordinal);
                Assert.DoesNotContain("cargo:", text, StringComparison.Ordinal);
                Assert.DoesNotContain("item.", text, StringComparison.Ordinal);
                Assert.DoesNotContain("CargoContract", text, StringComparison.Ordinal);
            }
            foreach (var state in States)
                Assert.NotEqual(state, dictionary[Prefix + "State." + state]);
        }
    }
}
