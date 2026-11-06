using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ModulePanelProjectionTests
{
    [Theory]
    [InlineData(6)]
    [InlineData(3)]
    public void World_projection_uses_actual_module_count(int count)
    {
        using var engine = TorpedoImpactTests.Create(TorpedoImpactTests.Scenario(o => o.ObjectId != TorpedoImpactTests.Player ? o : o with
        {
            Modules = o.Modules!.Where(m => count == 6 || m.ModuleId is not
                ("MOD-PLAYER-SCANNER-01" or "MOD-PLAYER-GENERATOR-01" or "MOD-PLAYER-LIVING-QUARTERS-01")).ToArray()
        }));
        var snapshot = TorpedoImpactTests.At(engine, 0);
        Assert.Equal(count, snapshot.InstalledModules.Length);
    }

}
