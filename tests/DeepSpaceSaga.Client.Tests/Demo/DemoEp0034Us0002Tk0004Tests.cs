using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

/// <summary>
/// EP-0034-US-0002-TK-0004: RU/EN texts of the passenger contracts (people aboard and life support, route, payout,
/// deadline, boarded confirmation, delivery result, every delivery state and the empty/unavailable states), read
/// from the shipped dictionaries copied to the test output. Acceptance refusals are the board texts of US-0006.
/// </summary>
public sealed class DemoEp0034Us0002Tk0004Tests
{
    internal const string Prefix = "Demo.PassengerContracts.";

    internal static readonly string[] States =
    [
        PassengerDeliveryStates.Aboard,
        PassengerDeliveryStates.Overdue,
        PassengerDeliveryStates.Delivered,
        PassengerDeliveryStates.Failed, // EP-0034-US-0003
        DemoCampaignReasons.Unavailable,
    ];

    internal static readonly string[] ScreenKeys = ["Title", "Empty", "Unavailable", "Aboard", "Line", "Boarded", "Delivered"];

    internal static IEnumerable<string> AllKeys() =>
        ScreenKeys.Concat(States.Select(s => "State." + s)).Select(k => Prefix + k);

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
        // Every acceptance refusal of a passenger offer already has a board text (US-0006), so the screen never
        // falls back to a raw code.
        foreach (var code in new[] { PassengerCapacityReasons.NoFreeCabins, PassengerCapacityReasons.InsufficientFoodRations,
                     PassengerCapacityReasons.CabinsOvercommitted, StationJobReasons.NotDockedAtOrigin, StationJobReasons.OfferAlreadyAccepted,
                     DemoCampaignReasons.Unavailable, DemoCampaignReasons.StaleRevision })
        {
            Assert.True(en.ContainsKey("Demo.Jobs.Reason." + code), code);
            Assert.True(ru.ContainsKey("Demo.Jobs.Reason." + code), code);
        }
    }

    [Fact]
    public void FormatArgumentsMatch()
    {
        var ru = Load("Russian");
        var en = Load("English");
        static string[] Args(string text) => Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().Order().ToArray();
        // Exactly the parameters the Contracts screen supplies, in both languages.
        var expected = new Dictionary<string, int> { ["Aboard"] = 1, ["Line"] = 6, ["Boarded"] = 3, ["Delivered"] = 2 };
        foreach (var dictionary in new[] { en, ru })
        {
            foreach (var key in AllKeys())
            {
                string suffix = key[Prefix.Length..];
                int count = expected.GetValueOrDefault(suffix);
                Assert.Equal(Enumerable.Range(0, count).Select(i => "{" + i + "}"), Args(dictionary[key]));
            }
            _ = string.Format(dictionary[Prefix + "Line"], 3, "Transit", "Mining", 435, "day 1, 05:00", "aboard");
            _ = string.Format(dictionary[Prefix + "Boarded"], 3, "Mining", "day 1, 05:00");
            _ = string.Format(dictionary[Prefix + "Delivered"], "day 1, 04:10", 435);
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
                Assert.DoesNotContain("pax-", text, StringComparison.Ordinal);
                Assert.DoesNotContain("PassengerContract", text, StringComparison.Ordinal);
            }
            foreach (var state in States)
                Assert.NotEqual(state, dictionary[Prefix + "State." + state]);
        }
    }
}
