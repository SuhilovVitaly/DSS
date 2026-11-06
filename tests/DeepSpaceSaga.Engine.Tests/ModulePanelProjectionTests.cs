using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ModulePanelProjectionTests
{
    [Theory]
    [InlineData(6)]
    [InlineData(3)]
    public void World_projection_uses_actual_module_count(int count)
    {
        using var engine = PointCounterFixture.Create(configure: o => o.ObjectId != PointCounterFixture.Player ? o : o with
        {
            Modules = o.Modules!.Where(m => count == 6 || m.ModuleId is not
                ("MOD-PLAYER-SCANNER-01" or "MOD-PLAYER-GENERATOR-01" or "MOD-PLAYER-LIVING-QUARTERS-01")).ToArray()
        });
        var snapshot = PointCounterFixture.At(engine);
        Assert.Equal(count, snapshot.InstalledModules.Length);
    }

}
