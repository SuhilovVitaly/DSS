using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
namespace DeepSpaceSaga.Client.Tests;

public class TorpedoCommandContentTests
{
    [Fact]
    public void Self_destruct_command_available_without_map_selection()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(AppContext.BaseDirectory, "Settings.json"), out _, out _);
        var module = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.torpedo.launcher.basic"));
        Assert.Equal(new[] { CombatCommandTypes.Fire, CombatCommandTypes.SelfDestruct }, module.CommandTypeIds);
        var command = registry.CommandDefinitions.GetDefinition(registry.CommandDefinitions.GetIndex(CombatCommandTypes.SelfDestruct));
        Assert.Equal("none", command.Target);
        Assert.Equal("module.torpedo.launcher", command.Type);
        Assert.Equal(0, command.ActivationEnergyCellsCost);
    }
}
