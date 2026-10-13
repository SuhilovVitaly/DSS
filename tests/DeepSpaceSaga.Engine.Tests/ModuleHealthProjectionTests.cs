using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ModuleHealthProjectionTests
{
    [Fact]
    public void Known_ships_project_authoritative_module_values_and_unknown_ship_is_masked()
    {
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId != Target ? o : o with
        {
            IsKnown = true,
            Modules = o.Modules!.Select((m, i) => i == 0 ? m with
            {
                Armor = 0,
                StructurePoints = 12,
                DisabledUntilMotionTimeMs = 5000
            } : m).ToArray()
        });
        using var knownEngine = Create(scenario);
        var known = At(knownEngine, 0);
        foreach (string id in new[] { Player, Target })
        {
            var ship = known.Objects.Single(o => o.ObjectId == id);
            var source = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == id);
            Assert.Equal(source.Modules!.Count, ship.ModuleHealth.Length);
            Assert.Equal(source.Modules!.Select(m => m.ModuleId), ship.ModuleHealth.Select(m => m.ModuleId));
        }
        var configured = known.Objects.Single(o => o.ObjectId == Target).ModuleHealth[0];
        Assert.Equal(0, configured.ArmorCurrent);
        Assert.Equal(12, configured.StructureCurrent);
        Assert.Equal(5000, configured.DisabledUntilMotionTimeMs);
        Assert.False(configured.IsDestroyed);

        var hiddenScenario = TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with { IsKnown = false } : o);
        using var hiddenEngine = Create(hiddenScenario);
        var hidden = At(hiddenEngine, 0).Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(SpaceObjectType.UnknownSpaceObject, hidden.RenderObjectType);
        Assert.Empty(hidden.ModuleHealth);
    }
}
