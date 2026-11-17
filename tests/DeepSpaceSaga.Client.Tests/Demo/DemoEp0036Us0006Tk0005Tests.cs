using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0006Tk0005Tests
{
    private static IEnumerable<string> Items()
    {
        var loot = DemoCampaignDefinitions.Load(AppContext.BaseDirectory).Loot!;
        return loot.ModuleItems!.Value.Select(m => m.ItemTypeId).Concat(loot.ConsumableItems.Select(m => m.ItemTypeId)).Append(loot.ScrapItemTypeId).Distinct();
    }

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void EveryLootItemHasRuEnName(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        foreach (var id in Items())
        {
            Assert.NotEqual(id, TradeItemPresentation.ItemDisplayName(id));
            Assert.False(string.IsNullOrWhiteSpace(TradeItemPresentation.ItemDescription(id)));
            Assert.DoesNotContain("item.", TradeItemPresentation.ItemDisplayName(id));
            if (id.StartsWith("item.trophy.", StringComparison.Ordinal))
                Assert.Contains(language == "Russian" ? "Только для продажи" : "For sale only", TradeItemPresentation.ItemDescription(id));
        }
        Assert.Equal(Localization.Get("Trade.ItemWater"), TradeItemPresentation.ItemDisplayName("item.water"));
        Assert.Equal(Localization.Get("Trade.DescriptionWater"), TradeItemPresentation.ItemDescription("item.water"));
    }

    [Fact]
    public void RuEnKeyParity()
    {
        Dictionary<string, string> Read(string language) => JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(AppContext.BaseDirectory + "/Data/Locale/" + language + ".json"))!;
        var en = Read("English"); var ru = Read("Russian");
        foreach (var id in Items())
            foreach (var prefix in new[] { "Trade.Item.", "Trade.Description." })
            { Assert.True(en.ContainsKey(prefix + id)); Assert.True(ru.ContainsKey(prefix + id)); }
    }
}
