using System.Globalization;

namespace DeepSpaceSaga.Client.UI.Screens.Trade;
internal static class TradeItemPresentation
{
    internal static string ItemDisplayName(string itemTypeId) => itemTypeId switch
    {
        "item.ice" => Localization.Get("Trade.ItemIce"),
        "item.iron-ore" => Localization.Get("Trade.ItemIronOre"),
        "item.silicon" => Localization.Get("Trade.ItemSilicon"),
        "item.magnesium-ore" => Localization.Get("Trade.ItemMagnesiumOre"),
        "item.uranium-ore" => Localization.Get("Trade.ItemUraniumOre"),
        "item.carbon-ore" => Localization.Get("Trade.ItemCarbonOre"),
        "item.water" => Localization.Get("Trade.ItemWater"),
        "item.steel" => Localization.Get("Trade.ItemSteel"),
        "item.energy-cells" => Localization.Get("Trade.ItemEnergyCells"),
        "item.electronics" => Localization.Get("Trade.ItemElectronics"),
        "item.fuel" => Localization.Get("Trade.ItemFuel"),
        "item.protein-mass" => Localization.Get("Trade.ItemProteinMass"),
        "item.food-rations" => Localization.Get("Trade.ItemFoodRations"),
        _ => itemTypeId
    };

    /// <summary>Flavor/trade description for the item preview panel (<see cref="DrawItemPreviewPanel"/>) — same id-to-key mapping convention as <see cref="ItemDisplayName"/>, falls back to empty for any future/unknown item type.</summary>
    internal static string ItemDescription(string itemTypeId) => itemTypeId switch
    {
        "item.ice" => Localization.Get("Trade.DescriptionIce"),
        "item.iron-ore" => Localization.Get("Trade.DescriptionIronOre"),
        "item.silicon" => Localization.Get("Trade.DescriptionSilicon"),
        "item.magnesium-ore" => Localization.Get("Trade.DescriptionMagnesiumOre"),
        "item.uranium-ore" => Localization.Get("Trade.DescriptionUraniumOre"),
        "item.carbon-ore" => Localization.Get("Trade.DescriptionCarbonOre"),
        "item.water" => Localization.Get("Trade.DescriptionWater"),
        "item.steel" => Localization.Get("Trade.DescriptionSteel"),
        "item.energy-cells" => Localization.Get("Trade.DescriptionEnergyCells"),
        "item.electronics" => Localization.Get("Trade.DescriptionElectronics"),
        "item.fuel" => Localization.Get("Trade.DescriptionFuel"),
        "item.protein-mass" => Localization.Get("Trade.DescriptionProteinMass"),
        "item.food-rations" => Localization.Get("Trade.DescriptionFoodRations"),
        _ => string.Empty
    };

    /// <summary>Icon asset path for the item preview panel (<see cref="DrawItemPreviewPanel"/>) — same id-to-value mapping convention as <see cref="ItemDisplayName"/>/<see cref="ItemDescription"/>, one subfolder per <see cref="TradeItemCategories"/> value; null for any future/unknown item type (the panel then falls back to the bare frame).</summary>
    internal static string? ItemImagePath(string itemTypeId) => itemTypeId switch
    {
        "item.ice" => "Images/Items/Resource/ice.png",
        "item.iron-ore" => "Images/Items/Resource/iron-ore.png",
        "item.silicon" => "Images/Items/Resource/silicon.png",
        "item.magnesium-ore" => "Images/Items/Resource/magnesium-ore.png",
        "item.uranium-ore" => "Images/Items/Resource/uranium-ore.png",
        "item.carbon-ore" => "Images/Items/Resource/carbon-ore.png",
        "item.water" => "Images/Items/Good/water.png",
        "item.steel" => "Images/Items/Good/steel.png",
        "item.energy-cells" => "Images/Items/Good/energy-cells.png",
        "item.fuel" => "Images/Items/Good/fuel.png",
        "item.protein-mass" => "Images/Items/Good/protein-mass.png",
        "item.food-rations" => "Images/Items/Good/food-rations.png",
        _ => null
    };

    internal static string ItemUnitLabel(string itemTypeId, bool singular = false) => itemTypeId switch
    {
        "item.food-rations" => Localization.Get(singular ? "Trade.UnitRationSingle" : "Trade.UnitRation"),
        "item.energy-cells" => Localization.Get(singular ? "Trade.UnitCellSingle" : "Trade.UnitCell"),
        "item.electronics" => Localization.Get(singular ? "Trade.UnitBlockSingle" : "Trade.UnitBlock"),
        "item.ice" or "item.iron-ore" or "item.silicon" or "item.magnesium-ore" or "item.carbon-ore"
            or "item.water" or "item.steel" or "item.protein-mass" or "item.fuel" or "item.uranium-ore"
            => Localization.Get("Trade.UnitKg"),
        _ => Localization.Get(singular ? "Trade.UnitGenericSingle" : "Trade.UnitGeneric")
    };

    internal static string FormatQuantity(string itemTypeId, long quantity) => string.Format(
        CultureInfo.CurrentCulture,
        Localization.Get("Trade.AmountWithUnit"),
        quantity,
        ItemUnitLabel(itemTypeId));

    internal static string FormatUnitMass(string itemTypeId, long unitMassKg) => string.Format(
        CultureInfo.CurrentCulture,
        Localization.Get("Trade.QuantityMass"),
        ItemUnitLabel(itemTypeId, singular: true),
        unitMassKg);

}

