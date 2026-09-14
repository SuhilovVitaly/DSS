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

}

