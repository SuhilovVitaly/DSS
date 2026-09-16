using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private long NextProductionTime() => _objects.SelectMany(o => o.ProducingModules.IsDefault ? [] : o.ProducingModules)
        .Where(m => m.Active && m.NextProductionDueGameTimeMs is not null)
        .Select(m => m.NextProductionDueGameTimeMs!.Value).DefaultIfEmpty(long.MaxValue).Min();

    private void StartAvailableProduction(long time)
    {
        for (int i = 0; i < _objects.Count; i++)
        {
            var station = _objects[i];
            if (station.ObjectType != SpaceObjectType.Station || station.ProducingModules.IsDefaultOrEmpty) continue;
            var modules = station.ProducingModules.ToBuilder();
            var inventory = station.Inventory.ToBuilder();
            for (int m = 0; m < modules.Count; m++)
            {
                var module = modules[m];
                if (!module.Active || module.NextProductionDueGameTimeMs is not null) continue;
                var recipe = _registry.FactoryTypes.GetDefinition(module.FactoryTypeIndex).Recipe;
                if (!recipe.Inputs.All(input => inventory.Any(s => s.ItemTypeIndex == _registry.ItemTypes.GetIndex(input.ItemTypeId)
                    && s.StockQuantity >= input.Count))) continue;
                long next = checked(time + recipe.CycleDurationMs);
                foreach (var input in recipe.Inputs) ChangeStationStock(inventory, input, -1);
                modules[m] = module with { NextProductionDueGameTimeMs = next };
            }
            _objects[i] = station with { Inventory = inventory.ToImmutable(), ProducingModules = modules.ToImmutable() };
        }
    }

    private void CompleteProduction(long time)
    {
        for (int i = 0; i < _objects.Count; i++)
        {
            var station = _objects[i];
            if (station.ProducingModules.IsDefaultOrEmpty) continue;
            var modules = station.ProducingModules.ToBuilder();
            var inventory = station.Inventory.ToBuilder();
            for (int m = 0; m < modules.Count; m++)
            {
                var module = modules[m];
                if (!module.Active || module.NextProductionDueGameTimeMs is not { } due || due > time) continue;
                foreach (var output in _registry.FactoryTypes.GetDefinition(module.FactoryTypeIndex).Recipe.Outputs)
                    ChangeStationStock(inventory, output, 1);
                modules[m] = module with { NextProductionDueGameTimeMs = null };
            }
            _objects[i] = station with { Inventory = inventory.ToImmutable(), ProducingModules = modules.ToImmutable() };
        }
    }

    private void ChangeStationStock(ImmutableArray<StationInventoryItemRuntime>.Builder inventory, RecipeMaterial material, int sign)
    {
        int item = _registry.ItemTypes.GetIndex(material.ItemTypeId);
        int index = -1;
        for (int i = 0; i < inventory.Count; i++) if (inventory[i].ItemTypeIndex == item) { index = i; break; }
        if (index < 0) inventory.Add(new(item, checked(material.Count * sign)));
        else inventory[index] = inventory[index] with { StockQuantity = checked(inventory[index].StockQuantity + material.Count * sign) };
    }
}
