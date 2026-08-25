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
        "Trade.ReasonInvalidQuantity", "Trade.ReasonInvalidPackageQuantity",
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
}
