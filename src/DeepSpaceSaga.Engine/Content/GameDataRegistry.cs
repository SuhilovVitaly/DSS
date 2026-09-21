using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Content;

internal sealed class GameDataRegistry
{
    private GameDataRegistry(
        TypeRegistry<ModuleCategoryDefinition> moduleCategories,
        TypeRegistry<ModuleTypeDefinition> moduleTypes,
        TypeRegistry<ItemTypeDefinition> itemTypes,
        TypeRegistry<CommandDefinition> commandDefinitions,
        TypeRegistry<FactoryTypeDefinition> factoryTypes,
        TypeRegistry<RecipeDefinition> recipes,
        TypeRegistry<DialogueDefinition>? dialogues = null,
        TypeRegistry<QuestDefinition>? quests = null,
        int catalogVersion = 1,
        string? legacyCatalogFingerprint = null,
        TypeRegistry<StationMarketProfileDefinition>? stationMarketProfiles = null)
    {
        ModuleCategories = moduleCategories;
        ModuleTypes = moduleTypes;
        ItemTypes = itemTypes;
        CommandDefinitions = commandDefinitions;
        FactoryTypes = factoryTypes;
        Recipes = recipes;
        Dialogues = dialogues ?? TypeRegistry<DialogueDefinition>.Empty;
        Quests = quests ?? TypeRegistry<QuestDefinition>.Empty;
        StationMarketProfiles = stationMarketProfiles ?? TypeRegistry<StationMarketProfileDefinition>.Empty;
        CatalogVersion = catalogVersion;
        LegacyCatalogFingerprint = legacyCatalogFingerprint;
        var economicItems = Enumerable.Range(0, itemTypes.Count).Select(itemTypes.GetDefinition)
            .OrderBy(item => item.TypeId, StringComparer.Ordinal)
            .Select(item => new
            {
                item.TypeId,
                item.UnitMassKg,
                item.BasePriceCredits,
                item.Category,
                item.TradeUnit,
                item.StorageKind,
                item.BuyQuantityStep,
                item.SellQuantityStep
            });
        CatalogCompatibility = new(catalogVersion, EconomyTimeData.CurrentRulesVersion,
            Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(economicItems))));
    }

    public TypeRegistry<ModuleCategoryDefinition> ModuleCategories { get; }
    public TypeRegistry<ModuleTypeDefinition> ModuleTypes { get; }
    public TypeRegistry<ItemTypeDefinition> ItemTypes { get; }
    public TypeRegistry<CommandDefinition> CommandDefinitions { get; }
    public TypeRegistry<FactoryTypeDefinition> FactoryTypes { get; }
    public TypeRegistry<RecipeDefinition> Recipes { get; }
    public TypeRegistry<DialogueDefinition> Dialogues { get; }
    public TypeRegistry<QuestDefinition> Quests { get; }
    public TypeRegistry<StationMarketProfileDefinition> StationMarketProfiles { get; }
    public int CatalogVersion { get; }
    public CatalogCompatibilityData CatalogCompatibility { get; }
    public string? LegacyCatalogFingerprint { get; }

    public static GameDataRegistry Empty { get; } = new(
        TypeRegistry<ModuleCategoryDefinition>.Empty,
        TypeRegistry<ModuleTypeDefinition>.Empty,
        TypeRegistry<ItemTypeDefinition>.Empty,
        TypeRegistry<CommandDefinition>.Empty,
        TypeRegistry<FactoryTypeDefinition>.Empty,
        TypeRegistry<RecipeDefinition>.Empty);

    public static GameDataRegistry Create(
        IEnumerable<ModuleCategoryDefinition> moduleCategories,
        IEnumerable<ModuleTypeDefinition> moduleTypes,
        IEnumerable<ItemTypeDefinition> itemTypes,
        IEnumerable<CommandDefinition> commandDefinitions,
        IEnumerable<FactoryTypeDefinition>? factoryTypes = null,
        IEnumerable<RecipeDefinition>? recipes = null,
        IEnumerable<DialogueDefinition>? dialogues = null,
        IEnumerable<QuestDefinition>? quests = null,
        int catalogVersion = 1,
        string? legacyCatalogFingerprint = null,
        IEnumerable<StationMarketProfileDefinition>? stationMarketProfiles = null)
    {
        if (catalogVersion != 1) throw new ContentException($"Unsupported catalogVersion: {catalogVersion}.");
        var commandRegistry = TypeRegistry<CommandDefinition>.Create(commandDefinitions, "command definitions");
        var categoryRegistry = TypeRegistry<ModuleCategoryDefinition>.Create(moduleCategories, "module types");
        var moduleRegistry = TypeRegistry<ModuleTypeDefinition>.Create(moduleTypes, "module implementations");
        var itemRegistry = TypeRegistry<ItemTypeDefinition>.Create(itemTypes, "item types");
        var catalogCodes = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < itemRegistry.Count; i++)
        {
            var item = itemRegistry.GetDefinition(i);
            ItemCatalogValidation.Validate(item, "item registry");
            if (item.CatalogCode is { } code && !catalogCodes.Add(code))
                throw new ContentException($"Item '{item.TypeId}': duplicate catalogCode '{code}'.");
        }
        var profiles = (stationMarketProfiles ?? []).ToArray();
        ValidateStationMarketProfiles(profiles, itemRegistry);
        var profileRegistry = TypeRegistry<StationMarketProfileDefinition>.Create(profiles, "station market profiles");
        var factoryRegistry = TypeRegistry<FactoryTypeDefinition>.Create(factoryTypes ?? [], "factory types");
        var recipeRegistry = TypeRegistry<RecipeDefinition>.Create(recipes ?? [], "recipes");
        var allRecipes = Enumerable.Range(0, recipeRegistry.Count).Select(recipeRegistry.GetDefinition)
            .Concat(Enumerable.Range(0, factoryRegistry.Count).Select(i => factoryRegistry.GetDefinition(i).Recipe));
        foreach (var recipe in allRecipes)
        {
            foreach (var material in recipe.Inputs.Concat(recipe.Outputs))
                if (!itemRegistry.Contains(material.ItemTypeId))
                    throw new ContentException($"Recipe '{recipe.TypeId}' references unknown itemTypeId '{material.ItemTypeId}'.");
        }

        for (int i = 0; i < categoryRegistry.Count; i++)
        {
            var category = categoryRegistry.GetDefinition(i);
            foreach (string commandTypeId in category.CommandTypeIds)
            {
                if (!commandRegistry.Contains(commandTypeId))
                {
                    throw new ContentException(
                        $"Module type '{category.TypeId}' references unknown command definition '{commandTypeId}'.");
                }

                var command = commandRegistry.GetDefinition(commandRegistry.GetIndex(commandTypeId));
                if (!string.Equals(command.Type, category.TypeId, StringComparison.Ordinal))
                {
                    throw new ContentException(
                        $"Command definition '{commandTypeId}' declares owning module type " +
                        $"'{command.Type}' but is referenced by module type '{category.TypeId}' " +
                        $"via commandTypeIds — the two must match.");
                }
            }
        }

        var dialogueRegistry = TypeRegistry<DialogueDefinition>.Create(dialogues ?? [], "dialogues");
        var questRegistry = TypeRegistry<QuestDefinition>.Create(quests ?? [], "quests");
        for (int i = 0; i < dialogueRegistry.Count; i++)
        {
            var dialogue = dialogueRegistry.GetDefinition(i);
            DialogueContentLoader.Validate(dialogue);
            foreach (var effect in dialogue.Nodes.SelectMany(n => n.Choices).SelectMany(c => c.Effects.IsDefault ? [] : c.Effects))
            {
                if (effect.ItemTypeId is not null && !itemRegistry.Contains(effect.ItemTypeId))
                    throw new ContentException($"Unknown dialogue item: {effect.ItemTypeId}");
                if (effect.QuestId is not null)
                {
                    if (!questRegistry.Contains(effect.QuestId)) throw new ContentException($"Unknown dialogue quest: {effect.QuestId}");
                    if (effect.ObjectiveId is not null && !questRegistry.GetDefinition(questRegistry.GetIndex(effect.QuestId)).Objectives.Contains(effect.ObjectiveId))
                        throw new ContentException($"Unknown dialogue objective: {effect.ObjectiveId}");
                }
            }
        }
        return new GameDataRegistry(categoryRegistry, moduleRegistry, itemRegistry, commandRegistry, factoryRegistry, recipeRegistry,
            dialogueRegistry, questRegistry, catalogVersion, legacyCatalogFingerprint, profileRegistry);
    }

    // Also used before a profile file is combined with its item catalog.
    internal static void ValidateStationMarketProfiles(
        IEnumerable<StationMarketProfileDefinition> profiles, TypeRegistry<ItemTypeDefinition>? items = null)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in profiles)
        {
            if (profile is null) throw new ContentException("Station market profiles: profiles contains null.");
            [DoesNotReturn]
            void Reject(string field, string reason) =>
                throw new ContentException($"Market profile '{profile.TypeId}', {field}: {reason}.");

            if (string.IsNullOrWhiteSpace(profile.TypeId) || !ids.Add(profile.TypeId))
                Reject("typeId", "must be nonempty and unique");
            if (string.IsNullOrWhiteSpace(profile.DisplayName) || !names.Add(profile.DisplayName))
                Reject("displayName", $"must be nonempty and unique ('{profile.DisplayName}')");
            if (profile.SizeFactors is null || profile.SizeFactors.Count != Enum.GetValues<StationSize>().Length)
                Reject("sizeFactors", "must contain exactly Outpost, Medium, Large and Huge");
            foreach (var size in Enum.GetValues<StationSize>())
            {
                if (!profile.SizeFactors.TryGetValue(size, out int factor) || factor <= 0)
                    Reject("sizeFactors", $"{size} must have a positive int factor");
                if (size == StationSize.Medium && factor != 1000)
                    Reject("sizeFactors", "Medium must equal 1000");
            }

            void ValidateAmount(long amount, string field)
            {
                if (amount < 0) Reject(field, "must be nonnegative");
                foreach (var (size, factor) in profile.SizeFactors)
                {
                    try
                    {
                        _ = checked((long)decimal.Round(checked((decimal)amount * factor) / 1000m,
                            0, MidpointRounding.AwayFromZero));
                    }
                    catch (OverflowException)
                    {
                        Reject(field, $"scaled amount overflows Int64 for {size}");
                    }
                }
            }

            void ValidateItem(string itemId, string field, bool fuel = false)
            {
                if (string.IsNullOrWhiteSpace(itemId)) Reject(field, $"empty itemTypeId '{itemId}'");
                if (!fuel && itemId == "item.fuel") Reject(field, $"item '{itemId}' is reserved for refuelStockKg");
                if (items is null) return;
                if (!items.Contains(itemId)) Reject(field, $"unknown item '{itemId}'");
                var item = items.GetDefinition(items.GetIndex(itemId));
                if (item.BasePriceCredits is not > 0) Reject(field, $"item '{itemId}' must have a positive BasePriceCredits");
                if (fuel)
                {
                    if (item.Category != TradeCategory.Good || item.TradeUnit != TradeUnit.Kilogram ||
                        item.StorageKind != ItemStorageKind.FuelTank)
                        Reject(field, $"item '{itemId}' must be Good/Kilogram/FuelTank");
                }
                else if (item.StorageKind != ItemStorageKind.Cargo)
                    Reject(field, $"item '{itemId}' must use Cargo storage");
            }

            HashSet<string> ValidateList(ImmutableArray<string> list, string field)
            {
                if (list.IsDefault) Reject(field, "array is required");
                var result = new HashSet<string>(StringComparer.Ordinal);
                foreach (string itemId in list)
                {
                    ValidateItem(itemId, field);
                    if (!result.Add(itemId)) Reject(field, $"duplicate item '{itemId}'");
                }
                return result;
            }

            var supply = ValidateList(profile.SupplyItemTypeIds, "supplyItemTypeIds");
            var demand = ValidateList(profile.DemandItemTypeIds, "demandItemTypeIds");
            if (demand.Count == 0) Reject("demandItemTypeIds", "must not be empty");
            foreach (string itemId in demand)
                if (!supply.Add(itemId)) Reject("demandItemTypeIds", $"item '{itemId}' overlaps supplyItemTypeIds");
            if (profile.InitialInventory.IsDefault) Reject("initialInventory", "array is required");
            var inventory = new HashSet<string>(StringComparer.Ordinal);
            foreach (var stock in profile.InitialInventory)
            {
                if (stock is null) Reject("initialInventory", "entry must not be null");
                ValidateItem(stock.ItemTypeId, "initialInventory");
                if (!inventory.Add(stock.ItemTypeId)) Reject("initialInventory", $"duplicate item '{stock.ItemTypeId}'");
                if (!supply.Contains(stock.ItemTypeId)) Reject("initialInventory", $"item '{stock.ItemTypeId}' is outside supply/demand union");
                ValidateAmount(stock.Quantity, $"initialInventory item '{stock.ItemTypeId}' quantity");
            }
            foreach (string itemId in supply)
                if (!inventory.Contains(itemId)) Reject("initialInventory", $"missing item '{itemId}' from supply/demand union");
            ValidateAmount(profile.InitialCredits, "initialCredits");
            ValidateAmount(profile.RefuelStockKg, "refuelStockKg");
            ValidateItem("item.fuel", "refuelStockKg", fuel: true);

            if (profile.Economy is { } economy)
                ValidateEconomy(economy, profile, supply, demand, inventory, ValidateItem, Reject);
        }
    }

    // supply/demand are the pre-existing US-0001 sets: after the caller's union pass, `supply`
    // already contains supply ∪ demand (see the `if (!supply.Add(itemId))` overlap check above),
    // while `demand` still holds only the profile's own demandItemTypeIds. `inventory` is the
    // profile's InitialInventory item set — always equal to supply ∪ demand once the caller's own
    // "missing item from union" check has passed.
    private static void ValidateEconomy(
        StationMarketEconomyDefinition economy,
        StationMarketProfileDefinition profile,
        HashSet<string> supplyUnion,
        HashSet<string> demand,
        HashSet<string> inventory,
        Action<string, string, bool> validateItem,
        Action<string, string> reject)
    {
        void RejectEconomy(string field, string reason) => reject($"economy.{field}", reason);

        if (!Enum.IsDefined(economy.ProductionSource))
            RejectEconomy("productionSource", "must be Profile or Modules");
        if (economy.HourlyInputs.IsDefault) RejectEconomy("hourlyInputs", "array is required");
        if (economy.HourlyOutputs.IsDefault) RejectEconomy("hourlyOutputs", "array is required");
        if (economy.HourlyConsumption.IsDefault) RejectEconomy("hourlyConsumption", "array is required");
        if (economy.StockTargets.IsDefault) RejectEconomy("stockTargets", "array is required");

        HashSet<string> ValidateRateList(ImmutableArray<StationMarketStockDefinition> list, string field)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in list)
            {
                if (entry is null) { RejectEconomy(field, "entry must not be null"); continue; }
                validateItem(entry.ItemTypeId, $"economy.{field}", false);
                if (!seen.Add(entry.ItemTypeId)) RejectEconomy(field, $"duplicate item '{entry.ItemTypeId}'");
                if (entry.Quantity <= 0) RejectEconomy(field, $"item '{entry.ItemTypeId}' quantity must be positive");
            }
            return seen;
        }

        var outputs = ValidateRateList(economy.HourlyOutputs, "hourlyOutputs");
        var inputs = ValidateRateList(economy.HourlyInputs, "hourlyInputs");
        var consumption = ValidateRateList(economy.HourlyConsumption, "hourlyConsumption");

        foreach (string itemId in inputs)
            if (consumption.Contains(itemId)) RejectEconomy("hourlyConsumption", $"item '{itemId}' also appears in hourlyInputs");

        if (economy.ProductionSource == StationMarketProductionSource.Profile)
        {
            if (!outputs.SetEquals(supplyUnion.Except(demand)))
                RejectEconomy("hourlyOutputs", "must exactly match supplyItemTypeIds");
            var demandCoverage = new HashSet<string>(inputs, StringComparer.Ordinal);
            demandCoverage.UnionWith(consumption);
            if (!demandCoverage.SetEquals(demand))
                RejectEconomy("hourlyInputs", "hourlyInputs and hourlyConsumption together must exactly cover demandItemTypeIds");
            if (outputs.Count == 0 && inputs.Count != 0)
                RejectEconomy("hourlyInputs", "must be empty when hourlyOutputs is empty (all demand goes to hourlyConsumption)");
            if (outputs.Count != 0 && inputs.Count == 0)
                RejectEconomy("hourlyInputs", "must not be empty when hourlyOutputs is not empty (one shared hourly batch)");
        }
        else
        {
            if (outputs.Count != 0) RejectEconomy("hourlyOutputs", "must be empty for Modules production source");
            if (inputs.Count != 0) RejectEconomy("hourlyInputs", "must be empty for Modules production source");
            foreach (string itemId in consumption)
                if (!demand.Contains(itemId)) RejectEconomy("hourlyConsumption", $"item '{itemId}' is outside demandItemTypeIds");
        }

        var targetIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var target in economy.StockTargets)
        {
            if (target is null) { RejectEconomy("stockTargets", "entry must not be null"); continue; }
            validateItem(target.ItemTypeId, "economy.stockTargets", false);
            if (!targetIds.Add(target.ItemTypeId)) RejectEconomy("stockTargets", $"duplicate item '{target.ItemTypeId}'");
            if (target.TargetStock <= 0) RejectEconomy("stockTargets", $"item '{target.ItemTypeId}' targetStock must be positive");
        }
        if (!targetIds.SetEquals(inventory))
            RejectEconomy("stockTargets", "must exactly cover initialInventory items");

        var rates = economy.HourlyOutputs.Concat(economy.HourlyInputs).Concat(economy.HourlyConsumption).ToArray();
        foreach (var rate in rates)
            if (!targetIds.Contains(rate.ItemTypeId))
                RejectEconomy("stockTargets", $"item '{rate.ItemTypeId}' has an hourly rate but no matching target");

        if (economy.ShortageThresholdPermille <= 0 || economy.ShortageThresholdPermille >= 1000)
            RejectEconomy("shortageThresholdPermille", "must be strictly between 0 and 1000");
        if (economy.SurplusThresholdPermille <= 1000 || economy.SurplusThresholdPermille >= 2000)
            RejectEconomy("surplusThresholdPermille", "must be strictly between 1000 and 2000");
        if (economy.BudgetRegenerationDivisorPerDay < 24)
            RejectEconomy("budgetRegenerationDivisorPerDay", "must be at least 24");
        if (profile.InitialCredits <= 0)
            reject("initialCredits", "must be positive when economy is configured");

        long ScaleForSize(long amount, int factor, StationSize size, string field)
        {
            try
            {
                return checked((long)decimal.Round(checked((decimal)amount * factor) / 1000m,
                    0, MidpointRounding.AwayFromZero));
            }
            catch (OverflowException)
            {
                reject(field, $"scaled amount overflows Int64 for {size}");
                return 0; // unreachable: reject always throws
            }
        }

        long DoubleChecked(long amount, StationSize size, string field)
        {
            try { return checked(2 * amount); }
            catch (OverflowException)
            {
                reject(field, $"doubled amount overflows Int64 for {size}");
                return 0; // unreachable: reject always throws
            }
        }

        foreach (var (size, factor) in profile.SizeFactors)
        {
            long scaledCredits = ScaleForSize(profile.InitialCredits, factor, size, "initialCredits");
            if (scaledCredits <= 0) reject("initialCredits", $"scaled amount is not positive for {size}");
            DoubleChecked(scaledCredits, size, "initialCredits"); // maxBudget = 2×scaledCredits overflow check

            foreach (var target in economy.StockTargets)
            {
                long scaledTarget = ScaleForSize(target.TargetStock, factor, size, "economy.stockTargets");
                if (scaledTarget <= 0)
                    RejectEconomy("stockTargets", $"item '{target.ItemTypeId}' scaled target is not positive for {size}");
                long maxStock = DoubleChecked(scaledTarget, size, "economy.stockTargets");

                var initial = profile.InitialInventory.First(stock => stock.ItemTypeId == target.ItemTypeId);
                long scaledInitial = ScaleForSize(initial.Quantity, factor, size, "initialInventory");
                if (scaledInitial > maxStock)
                    reject("initialInventory", $"item '{target.ItemTypeId}' scaled stock exceeds capacity for {size}");

                foreach (var rate in rates)
                    if (rate.ItemTypeId == target.ItemTypeId && rate.Quantity > maxStock)
                        RejectEconomy("stockTargets", $"item '{target.ItemTypeId}' hourly rate exceeds capacity for {size}");
            }
        }
    }
}
