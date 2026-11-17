using System.Text.Json;
using System.Text.RegularExpressions;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0002Tk0004Tests
{
    [Fact]
    public void PaymentKeys_ParityAndReasons()
    {
        Dictionary<string, string> Read(string language) => JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data/Locale", language + ".json")))!;
        var en = Read("English");
        var ru = Read("Russian");
        foreach (var key in new[] { "claim", "due", "paid", "payment_confirmed", "bounty_not_found",
            "bounty_no_verified_victory", "bounty_wrong_station", "bounty_already_paid", "bounty_credit_overflow" })
        {
            Assert.False(string.IsNullOrWhiteSpace(en["bounty." + key]));
            Assert.False(string.IsNullOrWhiteSpace(ru["bounty." + key]));
            Assert.Equal(Regex.Matches(en["bounty." + key], @"\{\d+\}").Select(m => m.Value),
                Regex.Matches(ru["bounty." + key], @"\{\d+\}").Select(m => m.Value));
        }
    }
}
