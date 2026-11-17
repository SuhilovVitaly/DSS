using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

/// <summary>
/// EP-0034-US-0006-TK-0005: RU/EN texts of the job board and journal (terms, payout explanation, deadlines,
/// pending/confirmed states, empty states) and of every stable rejection code an acceptance can return,
/// read from the shipped dictionaries copied to the test output.
/// </summary>
public sealed class DemoEp0034Us0006Tk0005Tests
{
    internal const string Prefix = "Demo.Jobs.";

    /// <summary>Every code the engine can return for an acceptance or publish as an offer verdict.</summary>
    internal static readonly string[] ReasonCodes =
    [
        StationJobReasons.OfferNotFound,
        StationJobReasons.OfferExpired,
        StationJobReasons.OfferAlreadyAccepted,
        StationJobReasons.NotDockedAtOrigin,
        StationJobReasons.InsufficientCargoSpace,
        PassengerCapacityReasons.NoFreeCabins,
        PassengerCapacityReasons.CabinsOvercommitted,
        PassengerCapacityReasons.InsufficientFoodRations,
        DemoCampaignReasons.StaleRevision,
        DemoCampaignReasons.Unavailable,
        DemoCampaignReasons.Unsupported,
        DemoCampaignReasons.InvalidIdentity,
        DemoCampaignReasons.IdentityConflict,
        DemoCampaignReasons.CarrierDestroyed,
        DemoCampaignReasons.BattleActive,
    ];

    internal static readonly string[] ScreenKeys =
    [
        "Title", "Journal", "BoardEmpty", "JournalEmpty", "Unavailable", "InFlight", "Accept", "Pending", "Accepted",
        "Rejected", "OfferPassengers", "OfferCargo", "OfferTerms", "OfferPayout", "ObligationLine", "Duration", "Time",
    ];

    internal static IEnumerable<string> AllKeys() => ScreenKeys
        .Concat(new[] { StationJobTypes.Passenger, StationJobTypes.UrgentPassenger, StationJobTypes.Cargo, StationJobTypes.Bounty }.Select(t => "Type." + t))
        .Concat(new[] { StationJobRiskBands.Safe, StationJobRiskBands.Normal, StationJobRiskBands.Elevated }.Select(r => "Risk." + r))
        .Concat(new[] { StationJobStatuses.Active, StationJobStatuses.Completed, StationJobStatuses.Failed, StationJobStatuses.Cancelled }.Select(s => "Status." + s))
        .Concat(ReasonCodes.Select(c => "Reason." + c))
        .Select(k => Prefix + k);

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
        }
        Assert.Matches("[А-Яа-я]", ru[Prefix + "Title"]);
        Assert.Matches("[А-Яа-я]", ru[Prefix + "Reason." + StationJobReasons.NotDockedAtOrigin]);
    }

    [Fact]
    public void FormatArgumentsMatch()
    {
        var ru = Load("Russian");
        var en = Load("English");
        foreach (var key in en.Keys.Where(k => k.StartsWith(Prefix, StringComparison.Ordinal)))
            Assert.Equal(Regex.Matches(en[key], @"\{\d+\}").Select(m => m.Value).Order(),
                Regex.Matches(ru[key], @"\{\d+\}").Select(m => m.Value).Order());

        static string[] Args(string text) => Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().Order().ToArray();
        // Exactly the parameters the board screen supplies, in both languages.
        var expected = new Dictionary<string, int>
        {
            ["OfferPassengers"] = 2, ["OfferCargo"] = 2, ["OfferTerms"] = 3, ["OfferPayout"] = 6,
            ["ObligationLine"] = 6, ["Duration"] = 2, ["Time"] = 3,
        };
        foreach (var (key, count) in expected)
            foreach (var dictionary in new[] { en, ru })
                Assert.Equal(Enumerable.Range(0, count).Select(i => "{" + i + "}"), Args(dictionary[Prefix + key]));
        foreach (var key in AllKeys().Where(k => !expected.ContainsKey(k[Prefix.Length..])))
            Assert.Empty(Args(en[key]));
        foreach (var dictionary in new[] { en, ru })
        {
            _ = string.Format(dictionary[Prefix + "OfferPayout"], 150, 180, 45, 15, "×1.5", "×1.15");
            _ = string.Format(dictionary[Prefix + "ObligationLine"], "Passengers", "Transit", "Mining", 435, "day 1, 05:00", "in progress");
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
                Assert.DoesNotContain("StationJob", text, StringComparison.Ordinal);
                Assert.DoesNotContain("jobs:", text, StringComparison.Ordinal);
            }
            foreach (var code in ReasonCodes)
                Assert.NotEqual(code, dictionary[Prefix + "Reason." + code]);
        }
    }
}
