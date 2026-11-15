using DeepSpaceSaga.Client;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0009Tk0004Tests
{
    private static readonly string[] RearmReasons =
    [
        "unsupported", "invalid_request", "wrong_ship", "carrier_not_ready", "battle_active",
        "stale_revision", "unknown_drone", "destroyed_drone", "hangar_not_ready", "invalid_quantity",
        "no_capacity", "no_cargo", "duplicate_command", "identity_conflict",
    ];

    [Fact]
    public void ShipLocaleParity()
    {
        var english = Localization.LoadLocaleFile("English")!;
        var russian = Localization.LoadLocaleFile("Russian")!;

        var shipKeys = english.Keys.Where(k => k.StartsWith("Ship.", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(shipKeys);
        Assert.Equal(shipKeys, russian.Keys.Where(k => k.StartsWith("Ship.", StringComparison.Ordinal)).Order(StringComparer.Ordinal));
        foreach (var key in shipKeys)
        {
            Assert.False(string.IsNullOrWhiteSpace(english[key]), $"English '{key}'");
            Assert.False(string.IsNullOrWhiteSpace(russian[key]), $"Russian '{key}'");
        }
    }

    [Fact]
    public void ReservedTextNeverClaimsCaptives()
    {
        foreach (var language in new[] { "English", "Russian" })
        {
            var strings = Localization.LoadLocaleFile(language)!;
            // Reserved cabins are held for future prisoners — the text never claims existing captives.
            Assert.DoesNotContain("prisoner", strings["Ship.ReservedCabins"], StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("пленн", strings["Ship.ReservedCabins"], StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("captive", strings["Ship.ReservedCabins"], StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void RearmFailureKeysExist()
    {
        foreach (var language in new[] { "English", "Russian" })
        {
            var strings = Localization.LoadLocaleFile(language)!;
            foreach (var reason in RearmReasons)
            {
                string key = "Ship.Rearm." + reason;
                Assert.True(strings.TryGetValue(key, out var text), $"{language}.json is missing '{key}'");
                Assert.False(string.IsNullOrWhiteSpace(text), $"{language}.json has an empty '{key}'");
            }
        }
    }
}
