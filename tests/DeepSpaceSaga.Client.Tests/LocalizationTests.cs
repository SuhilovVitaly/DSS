using DeepSpaceSaga.Client;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Guards the Data\Locale\*.json asset pipeline used by MainMenuScreen's button labels
/// (must resolve at the client's working directory and be registered in the .csproj with
/// CopyToOutputDirectory, mirroring MainMenuScreenTests.Background_image_is_loaded) and
/// the Localization.Get key/value contract.
/// </summary>
public class LocalizationTests
{
    private static readonly string[] RequiredKeys =
    {
        "MainMenu.NewGame", "MainMenu.Load", "MainMenu.Settings", "MainMenu.Exit",

        "Settings.Title", "Settings.Language", "Settings.InterfaceScale",
        "Settings.Monitor", "Settings.RestartNote", "Settings.Exit",

        "ScenarioSelect.Title", "ScenarioSelect.Back", "ScenarioSelect.Play",
        "ScenarioSelect.Difficulty", "ScenarioSelect.DifficultyNormal",
        "ScenarioSelect.Environment", "ScenarioSelect.EnvironmentOpenSpace",
        "ScenarioSelect.Crew", "ScenarioSelect.CrewOnePerson",

        "Trade.Title", "Trade.Docked", "Trade.NotDocked", "Trade.Credits", "Trade.Cargo",
        "Trade.Fuel", "Trade.StationInventory", "Trade.Transaction", "Trade.YourCargo",
        "Trade.UnitPrice", "Trade.Quantity", "Trade.TotalPrice", "Trade.Buy", "Trade.Sell",
        "Trade.Refuel", "Trade.Cancel", "Trade.Exit", "Trade.AccountSummary", "Trade.CurrentCredits",
        "Trade.TransactionTotal", "Trade.ProjectedBalance", "Trade.SelectItemPrompt",
        "Trade.ItemEnergyCells", "Trade.ItemFuel", "Trade.ItemIce",
        "Trade.ItemIronOre", "Trade.ItemSilicon", "Trade.ItemMagnesiumOre", "Trade.ItemWater",
        "Trade.ItemSteel", "Trade.ItemProteinMass", "Trade.ItemFoodRations",
        "Trade.StatusBuySuccess", "Trade.StatusSellSuccess", "Trade.StatusSellPartial",
        "Trade.StatusRefuelSuccess", "Trade.ReasonInsufficientPlayerCredits",
        "Trade.ReasonInsufficientStationStock", "Trade.ReasonCargoCapacityExceeded",
        "Trade.ReasonFuelCapacityExceeded", "Trade.ReasonUnknownItemType",
        "Trade.ReasonNotDocked", "Trade.ReasonInsufficientCargoQuantity",
        "Trade.ReasonInvalidQuantity",

        "Trade.DescriptionIce", "Trade.DescriptionIronOre", "Trade.DescriptionSilicon",
        "Trade.DescriptionMagnesiumOre", "Trade.DescriptionUraniumOre", "Trade.DescriptionCarbonOre",
        "Trade.DescriptionWater", "Trade.DescriptionSteel", "Trade.DescriptionEnergyCells",
        "Trade.DescriptionFuel", "Trade.DescriptionProteinMass", "Trade.DescriptionFoodRations",
    };

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void Locale_file_defines_all_MainMenu_Settings_and_ScenarioSelect_keys(string language)
    {
        var strings = Localization.LoadLocaleFile(language);

        Assert.NotNull(strings);
        foreach (var key in RequiredKeys)
            Assert.True(strings!.ContainsKey(key), $"{language}.json is missing key '{key}'");
    }

    [Fact]
    public void Get_falls_back_to_the_key_itself_when_missing()
    {
        Assert.Equal("MainMenu.DoesNotExist", Localization.Get("MainMenu.DoesNotExist"));
    }

    /// <summary>Quote/receipt flow messages and the placeholder indexes each one takes.</summary>
    private static readonly (string Key, int[] Placeholders)[] TradeQuoteMessages =
    {
        ("TradeUX.QuoteLoading", []),
        ("TradeUX.QuoteStale", []),
        ("TradeUX.QuoteRequired", []),
        ("TradeUX.InvalidQuote", []),
        ("TradeUX.QuoteUnavailable", []),
        ("TradeUX.StationCapacityLimit", []),
        ("TradeUX.PartialPreview", [0, 1, 2]),
        ("TradeUX.ReceiptUnavailable", []),
        ("TradeUX.FuelServiceOnly", []),
        ("TradeUX.StationBudgetLimit", []),
        ("TradeUX.SuccessResult", [0, 1, 2]),
        ("TradeUX.PartialResult", [0, 1, 2, 3]),
    };

    private static int[] PlaceholderIndexes(string text) =>
        System.Text.RegularExpressions.Regex.Matches(text, @"\{(\d+)\}")
            .Select(m => int.Parse(m.Groups[1].Value))
            .Distinct()
            .Order()
            .ToArray();

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void Trade_quote_keys_exist_in_both_locales(string language)
    {
        var strings = Localization.LoadLocaleFile(language);

        Assert.NotNull(strings);
        foreach (var (key, _) in TradeQuoteMessages)
        {
            Assert.True(strings!.TryGetValue(key, out var text), $"{language}.json is missing key '{key}'");
            Assert.False(string.IsNullOrWhiteSpace(text), $"{language}.json has an empty '{key}'");
            Assert.NotEqual(key, text);
        }

        var english = Localization.LoadLocaleFile("English")!;
        var russian = Localization.LoadLocaleFile("Russian")!;
        Assert.Equal(
            english.Keys.Where(k => k.StartsWith("TradeUX.", StringComparison.Ordinal)).Order(),
            russian.Keys.Where(k => k.StartsWith("TradeUX.", StringComparison.Ordinal)).Order());
    }

    [Fact]
    public void Trade_quote_messages_have_matching_expected_placeholders()
    {
        var english = Localization.LoadLocaleFile("English")!;
        var russian = Localization.LoadLocaleFile("Russian")!;

        foreach (var (key, placeholders) in TradeQuoteMessages)
        {
            Assert.True(placeholders.SequenceEqual(PlaceholderIndexes(english[key])), $"English '{key}' placeholders");
            Assert.True(placeholders.SequenceEqual(PlaceholderIndexes(russian[key])), $"Russian '{key}' placeholders");
        }

        // The exact approved copy — no QuoteId, revision or hidden station cash leaks into the text.
        Assert.Equal("Market conditions changed. Review the new quote and confirm again.", english["TradeUX.QuoteStale"]);
        Assert.Equal("Условия торговли изменились. Проверьте новую котировку и подтвердите снова.", russian["TradeUX.QuoteStale"]);
        Assert.Equal("Fuel is available through refuelling only.", english["TradeUX.FuelServiceOnly"]);
        Assert.Equal("Топливо доступно только через заправку.", russian["TradeUX.FuelServiceOnly"]);
        Assert.Equal("На складе станции нет места для этого товара.", russian["TradeUX.StationCapacityLimit"]);
    }

    [Theory]
    [InlineData(1L, 10L, 25L)]
    [InlineData(6L, 10L, 431L)]
    [InlineData(999L, 1000L, 1234567L)]
    public void Partial_preview_and_result_format_requested_actual_and_total(long actual, long requested, long total)
    {
        var english = Localization.LoadLocaleFile("English")!;
        var russian = Localization.LoadLocaleFile("Russian")!;

        Assert.Equal(
            $"Will sell {actual} of {requested} for {total} tokens. The remainder stays in cargo.",
            string.Format(english["TradeUX.PartialPreview"], actual, requested, total));
        Assert.Equal(
            $"Будет продано {actual} из {requested} за {total} токенов. Остаток останется в трюме.",
            string.Format(russian["TradeUX.PartialPreview"], actual, requested, total));

        // Existing result semantics: {0}=item, {1}=actual, {2}=total, {3}=requested.
        Assert.Equal(
            $"Ice: {actual} of {requested} completed · {total} tokens",
            string.Format(english["TradeUX.PartialResult"], "Ice", actual, total, requested));
        Assert.Equal(
            $"Лёд: исполнено {actual} из {requested} · {total} токенов",
            string.Format(russian["TradeUX.PartialResult"], "Лёд", actual, total, requested));
        Assert.Equal(
            $"Ice: {actual} completed · {total} tokens",
            string.Format(english["TradeUX.SuccessResult"], "Ice", actual, total));
    }
}
