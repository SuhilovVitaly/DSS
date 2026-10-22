using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
namespace DeepSpaceSaga.Client.Tests;

public class TorpedoCommandContentTests
{
    [Fact]
    public void Detonate_command_requires_owned_projectile_identity()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(AppContext.BaseDirectory, "Settings.json"), out _, out _);
        var module = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.torpedo.launcher.basic"));
        Assert.Equal(new[] { CombatCommandTypes.Launch, CombatCommandTypes.Detonate }, module.CommandTypeIds);
        var command = registry.CommandDefinitions.GetDefinition(registry.CommandDefinitions.GetIndex(CombatCommandTypes.Detonate));
        Assert.Equal("object", command.Target);
        Assert.Equal("module.torpedo.launcher", command.Type);
        Assert.Equal(0, command.ActivationEnergyCellsCost);
    }
}
