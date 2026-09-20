namespace DeepSpaceSaga.Engine.Content;

internal static class ItemCatalogValidation
{
    public static void Validate(ItemTypeDefinition item, string source)
    {
        void Reject(string field, string reason) =>
            throw new ContentException($"{source}: item '{item.TypeId}', {field}: {reason}.");

        if (string.IsNullOrWhiteSpace(item.TypeId)) Reject("typeId", "must not be empty");
        if (string.IsNullOrWhiteSpace(item.DisplayName)) Reject("displayName", "must not be empty");
        if (!Enum.IsDefined(item.Category)) Reject("tradeCategory", "unknown value");
        if (!Enum.IsDefined(item.TradeUnit)) Reject("tradeUnit", "unknown value");
        if (!Enum.IsDefined(item.StorageKind)) Reject("storageKind", "unknown value");
        if (item.CatalogCode is not null && string.IsNullOrWhiteSpace(item.CatalogCode))
            Reject("catalogCode", "must be nonempty when supplied");
        if (item.BasePriceCredits is <= 0) Reject("basePriceCredits", "must be positive or null for nontradeable content");
        if (item.StorageKind == ItemStorageKind.Cargo && item.UnitMassKg <= 0)
            Reject("unitMassKg", "cargo mass must be positive");
        if (item.UnitMassKg < 0) Reject("unitMassKg", "must not be negative");
        if (item.BuyQuantityStep != 1) Reject("buyQuantityStep", "current rules require one trade unit");
        if (item.SellQuantityStep != 1) Reject("sellQuantityStep", "current rules require one trade unit");
        if (item.StorageKind == ItemStorageKind.FuelTank &&
            (item.TradeUnit != TradeUnit.Kilogram || item.Category != TradeCategory.Good))
            Reject("storageKind", "FuelTank requires Good measured in Kilogram");
        if (item.StorageKind == ItemStorageKind.Cargo && item.TradeUnit == TradeUnit.Kilogram && item.UnitMassKg != 1)
            Reject("unitMassKg", "a Kilogram cargo unit must weigh exactly 1 kg");
        if ((item.TradeUnit is TradeUnit.Ration or TradeUnit.EnergyCell) &&
            (item.Category != TradeCategory.Good || item.StorageKind != ItemStorageKind.Cargo))
            Reject("tradeUnit", "Ration and EnergyCell require Good stored in Cargo");
        if (item.TypeId == "item.fuel" && item.StorageKind != ItemStorageKind.FuelTank)
            Reject("storageKind", "item.fuel must use FuelTank");
        if (item.TypeId == "item.food-rations" && item.TradeUnit != TradeUnit.Ration)
            Reject("tradeUnit", "item.food-rations must use Ration");
        if (item.TypeId == "item.energy-cells" && item.TradeUnit != TradeUnit.EnergyCell)
            Reject("tradeUnit", "item.energy-cells must use EnergyCell");
    }
}
