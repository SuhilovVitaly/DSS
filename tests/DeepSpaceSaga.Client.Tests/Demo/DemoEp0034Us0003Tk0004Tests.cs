using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

/// <summary>
/// EP-0034-US-0003-TK-0004: RU/EN texts of transport failures — calendar deadline rule, consequences before the
/// action (cancellation and expiry penalties), the cancel action with confirmation and pending state, the confirmed
/// result, every outcome, passengers waiting to disembark and the disembarkation, contract debt, every stable
/// cancellation refusal and the empty/unavailable states — read from the shipped dictionaries.
/// </summary>
public sealed class DemoEp0034Us0003Tk0004Tests
{
    internal const string Prefix = "Demo.TransportFailures.";

    internal static readonly string[] Outcomes =
        [TransportFailureReasons.Cancelled, TransportFailureReasons.Expired, TransportFailureReasons.CarrierLost];

    /// <summary>Every code the cancel command or the terms verdict can return.</summary>
    internal static readonly string[] Reasons =
    [
        TransportFailureReasons.ContractNotFound, TransportFailureReasons.ContractNotActive, TransportFailureReasons.NotCancellable,
        DemoCampaignReasons.StaleRevision, DemoCampaignReasons.Unavailable, DemoCampaignReasons.Unsupported,
        DemoCampaignReasons.InvalidIdentity, DemoCampaignReasons.IdentityConflict, DemoCampaignReasons.CarrierDestroyed,
        DemoCampaignReasons.BattleActive,
    ];

    /// <summary>Screen keys with the number of format parameters the Contracts screen supplies.</summary>
    internal static readonly Dictionary<string, int> ScreenKeys = new()
    {
        ["Title"] = 0, ["Empty"] = 0, ["Unavailable"] = 0, ["Calendar"] = 0, ["Terms"] = 3, ["Cancel"] = 0,
        ["CancelConfirm"] = 1, ["CancelNo"] = 0, ["Pending"] = 0, ["Cancelled"] = 3, ["Line"] = 6, ["DisembarkPending"] = 1,
        ["Disembarked"] = 3, ["Lost"] = 1, ["Debt"] = 1,
    };

    internal static IEnumerable<string> AllKeys() =>
        ScreenKeys.Keys.Concat(Outcomes.Select(o => "Outcome." + o)).Concat(Reasons.Select(r => "Reason." + r)).Select(k => Prefix + k);

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
        // A failed passenger contract has its own delivery-state text next to the US-0002 states.
        Assert.True(en.ContainsKey("Demo.PassengerContracts.State." + PassengerDeliveryStates.Failed));
        Assert.True(ru.ContainsKey("Demo.PassengerContracts.State." + PassengerDeliveryStates.Failed));
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
            _ = string.Format(dictionary[Prefix + "Terms"], 94, "day 1, 02:30", 94);
            _ = string.Format(dictionary[Prefix + "CancelConfirm"], 94);
            _ = string.Format(dictionary[Prefix + "Cancelled"], 94, 80, 14);
            _ = string.Format(dictionary[Prefix + "Line"], "3 passengers Transit → Mining", "cancelled", 94, 80, 0, 14);
            _ = string.Format(dictionary[Prefix + "DisembarkPending"], 3);
            _ = string.Format(dictionary[Prefix + "Disembarked"], 3, "Mining", "day 1, 04:10");
            _ = string.Format(dictionary[Prefix + "Lost"], 3);
            _ = string.Format(dictionary[Prefix + "Debt"], 14);
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
                Assert.DoesNotContain("penalty:", text, StringComparison.Ordinal);
                Assert.DoesNotContain("TransportFailure", text, StringComparison.Ordinal);
            }
            foreach (var code in Outcomes)
                Assert.NotEqual(code, dictionary[Prefix + "Outcome." + code]);
            foreach (var code in Reasons)
                Assert.NotEqual(code, dictionary[Prefix + "Reason." + code]);
        }
    }
}
