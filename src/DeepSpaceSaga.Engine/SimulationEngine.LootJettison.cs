using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private ImmutableArray<LootCargoSnapshot> BuildLootCargo()
    {
        var ship = _objects.FirstOrDefault(s => s.InitialMotion.ObjectId == PlayerShipObjectId && !s.IsDestroyed);
        if (ship is null) return [];
        return ship.Modules.SelectMany(m => m.Cargo).GroupBy(c => c.ItemTypeIndex).OrderBy(g => g.Key).Select(g =>
        {
            var item = _registry.ItemTypes.GetDefinition(g.Key);
            return item.StorageKind != ItemStorageKind.Cargo ? null : new LootCargoSnapshot(item.TypeId, g.Sum(c => c.Quantity), item.UnitMassKg, item.SellQuantityStep);
        }).Where(c => c is not null).Select(c => c!).ToImmutableArray();
    }
    private void PrepareJettisonCargo(JettisonCargoCommand command, ref DemoCampaignData candidate, ref string? reason, ref Action? commit)
    {
        if (!_registry.ItemTypes.Contains(command.ItemTypeId)) { reason = BattleLootReasons.NotCargoItem; return; }
        int itemIndex = _registry.ItemTypes.GetIndex(command.ItemTypeId);
        var item = _registry.ItemTypes.GetDefinition(itemIndex);
        if (item.StorageKind != ItemStorageKind.Cargo) { reason = BattleLootReasons.NotCargoItem; return; }
        if (command.Quantity <= 0 || command.Quantity % item.SellQuantityStep != 0) { reason = BattleLootReasons.InvalidQuantity; return; }
        int index = _objects.FindIndex(s => s.InitialMotion.ObjectId == PlayerShipObjectId && !s.IsDestroyed);
        if (index < 0) { reason = DemoCampaignReasons.CarrierDestroyed; return; }
        var ship = _objects[index];
        long available = ship.Modules.SelectMany(m => m.Cargo).Where(c => c.ItemTypeIndex == itemIndex).Sum(c => c.Quantity);
        if (available < command.Quantity) { reason = BattleLootReasons.NotEnoughQuantity; return; }
        long remaining = command.Quantity;
        var modules = ship.Modules;
        for (int i = 0; i < modules.Length && remaining > 0; i++)
        {
            var module = modules[i]; var cargo = module.Cargo;
            int stackIndex = FindCargoStackIndex(cargo, itemIndex);
            if (stackIndex < 0) continue;
            long amount = Math.Min(remaining, cargo[stackIndex].Quantity);
            var removed = RemoveCargoCost(cargo[stackIndex], amount).Remaining;
            cargo = removed is null ? cargo.RemoveAt(stackIndex) : cargo.SetItem(stackIndex, removed);
            modules = modules.SetItem(i, module with { Cargo = cargo, AvailableCapacityKg = ComputeAvailableCapacityKg(_registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex), cargo) });
            remaining -= amount;
        }
        var finance = PrepareVoyageCargoRemoval(_voyageLedgers, itemIndex, command.Quantity);
        candidate = candidate with
        {
            LootSession = candidate.LootSession! with
            { Jettison = candidate.LootSession.Jettison.Add(new(command.CommandId, command.ItemTypeId, command.Quantity)) }
        };
        commit = () => { _objects[index] = ship with { Modules = modules }; _voyageLedgers = finance; };
    }
}
