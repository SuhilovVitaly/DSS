using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ModuleDamageTests
{
    [Fact]
    public void Damage_split_150_vs_light()
    {
        var first = ModuleDamageMath.Apply(150, 20, 100, 60, 150);
        Assert.Equal(0, first.ArmorAfter);
        Assert.Equal(60, first.StructureAfter);
        var second = ModuleDamageMath.Apply(150, 20, 100, first.ArmorAfter, first.StructureAfter);
        Assert.Equal(0, second.StructureAfter);
    }

    [Fact]
    public void Unarmored_dies_one_hit()
    {
        var result = ModuleDamageMath.Apply(150, 20, 100, 0, 150);
        Assert.Equal(0, result.ArmorAfter);
        Assert.Equal(0, result.StructureAfter);
        Assert.Equal(150, result.StructureLost);
    }

}
