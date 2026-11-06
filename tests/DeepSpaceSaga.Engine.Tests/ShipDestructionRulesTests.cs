using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ShipDestructionRulesTests
{
    private static readonly string[] CriticalRoles = ["bridge", "engine"];

    [Fact]
    public void Two_of_nine_survives_three_kills()
    {
        var modules = Enumerable.Range(0, 9)
            .Select(i => new ShipModuleLife("module-" + i, null, i < 2)).ToArray();
        Assert.Null(ShipDestructionMath.Cause(modules, "module-1", CriticalRoles, 0.3333m));
        modules[2] = modules[2] with { Destroyed = true };
        Assert.Equal("threshold:3/9", ShipDestructionMath.Cause(modules, "module-2", CriticalRoles, 0.3333m));
    }

    [Theory]
    [InlineData("bridge")]
    [InlineData("engine")]
    public void Critical_module_kills(string role)
    {
        ShipModuleLife[] modules = [new("critical", role, true), new("other", null, false), new("third", null, false),
            new("fourth", null, false)];
        Assert.Equal("critical:critical", ShipDestructionMath.Cause(modules, "critical", CriticalRoles, 0.3333m));
    }

    [Fact]
    public void Mukha_one_module_kills()
    {
        ShipModuleLife[] modules = [new("launcher", null, true), new("engine", "engine", false)];
        Assert.Equal("threshold:1/2", ShipDestructionMath.Cause(modules, "launcher", CriticalRoles, 0.3333m));
    }
}
