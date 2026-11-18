using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    internal static int ModuleSalvageChance(int baseChanceBasisPoints, string category) =>
        checked(baseChanceBasisPoints * (category == "weapon" ? 95 : category == "engine" ? 105 : 100) / 100);

    private ImmutableArray<LootModuleData> InitializeLootModules(string battleId, TacticalDrone drone, ulong seed, ref long draws, int baseChance)
    {
        var definitions = _demoDefinitions.Loot!.ModuleItems ?? throw new ScenarioException("Missing demo module loot mappings.");
        var modules = ImmutableArray.CreateBuilder<LootModuleData>();
        long cursor = draws;
        void Add(string category, string catalogId, int slot)
        {
            var map = definitions.Single(m => m.Category == category && m.CatalogId == catalogId);
            bool destroyed = LootDestroyed(LootRoll(seed, cursor++));
            modules.Add(new(battleId + "/" + drone.Id + "/" + category + "/" + slot, category, catalogId, map.ItemTypeId,
                destroyed, ModuleSalvageChance(baseChance, category)));
        }
        Add("weapon", drone.Weapon.Id, 0);
        Add("armor", drone.Plates.Id, 0);
        if (drone.Shield is { } shield)
            for (int i = 0; i < drone.ShieldCount; i++) Add("shield", shield.Id, i);
        for (int i = 0; i < drone.Engines.Length; i++)
            if (drone.Engines[i].Factor > 0) Add("engine", drone.Engines[i].Definition.Id, i);
        if (drone.Decoy is { } decoy) Add("decoy", decoy.Id, 0);
        draws = cursor;
        return modules.ToImmutable();
    }

    private void PrepareModuleSalvage(AttemptModuleSalvageCommand command, ref BattleLootData loot,
        ref LootSourceData source, ref string? reason, ref Action? commit)
    {
        var module = source.Modules.FirstOrDefault(m => m.InstanceId == command.InstanceId && !m.Destroyed);
        if (module is null) { reason = BattleLootReasons.InvalidTarget; return; }
        if (module.State != LootAttemptStates.NotAttempted) { reason = BattleLootReasons.AlreadyAttempted; return; }
        if (!TryPrepareLootCargo(module.ItemTypeId, 1, out var grant)) { reason = BattleLootReasons.CargoFull; return; }
        bool success = LootAttemptSucceeded(LootRoll(loot.Seed, loot.DrawCount), module.ChanceBasisPoints);
        loot = loot with { DrawCount = checked(loot.DrawCount + 1) };
        source = source with { Modules = source.Modules.Replace(module, module with { State = success ? LootAttemptStates.Collected : LootAttemptStates.Failed }) };
        commit = success ? grant : null;
    }

    private ModuleLootSnapshot ProjectLootModule(LootModuleData module, bool open, Func<string, long, bool> canFit)
    {
        var item = _registry.ItemTypes.GetDefinition(_registry.ItemTypes.GetIndex(module.ItemTypeId));
        string? reason = !open ? BattleLootReasons.LootClosed : module.State != LootAttemptStates.NotAttempted ? BattleLootReasons.AlreadyAttempted :
            !canFit(module.ItemTypeId, 1) ? BattleLootReasons.CargoFull : null;
        return new(module.InstanceId, module.Category, module.CatalogId, module.ItemTypeId, item.DisplayName, "intact",
            item.BasePriceCredits ?? 0, module.ChanceBasisPoints, module.State, reason is null, reason, item.UnitMassKg);
    }
}
