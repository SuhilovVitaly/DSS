using System.Collections.Immutable;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Covers "GameDataRegistry.Create" cross-checking a command's owning "Type" against the
/// module type (category) that references it via "CommandTypeIds" (module→command remains the
/// source of truth; "Type" is the physical-organization field being cross-validated against it)
/// — story-20260818-085340, Batch 1, U3/U5.
/// </summary>
public class GameDataRegistryTests
{
    [Fact]
    public void Create_throws_when_command_type_does_not_match_owning_module_type()
    {
        var moduleCategories = new[]
        {
            new ModuleCategoryDefinition(
                "module.engine.basic", "Engine", SlotSize: 1,
                CommandTypeIds: ImmutableArray.Create("engine.accelerate"))
        };

        var moduleTypes = new[]
        {
            new ModuleTypeDefinition(
                "module.engine.basic", "Engine", SlotSize: 1, MassKg: 5000,
                StructurePointsMax: 100, PowerConsumptionW: 0,
                CommandTypeIds: ImmutableArray.Create("engine.accelerate"))
        };

        var commandDefinitions = new[]
        {
            // Deliberately mismatched: this command claims ownership by the scanner module
            // type, but is referenced by module.engine.basic's commandTypeIds.
            new CommandDefinition("engine.accelerate", "Accelerate", Type: "module.scanner.mk1")
        };

        var ex = Assert.Throws<ContentException>(() =>
            GameDataRegistry.Create(moduleCategories, moduleTypes, itemTypes: [], commandDefinitions));

        Assert.Contains("module.scanner.mk1", ex.Message, StringComparison.Ordinal);
        Assert.Contains("module.engine.basic", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_succeeds_when_command_type_matches_owning_module_type()
    {
        var moduleCategories = new[]
        {
            new ModuleCategoryDefinition(
                "module.engine.basic", "Engine", SlotSize: 1,
                CommandTypeIds: ImmutableArray.Create("engine.accelerate"))
        };

        var moduleTypes = new[]
        {
            new ModuleTypeDefinition(
                "module.engine.basic", "Engine", SlotSize: 1, MassKg: 5000,
                StructurePointsMax: 100, PowerConsumptionW: 0,
                CommandTypeIds: ImmutableArray.Create("engine.accelerate"))
        };

        var commandDefinitions = new[]
        {
            new CommandDefinition("engine.accelerate", "Accelerate", Type: "module.engine.basic")
        };

        var registry = GameDataRegistry.Create(moduleCategories, moduleTypes, itemTypes: [], commandDefinitions);

        Assert.True(registry.CommandDefinitions.Contains("engine.accelerate"));
    }
}
