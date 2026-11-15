using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests;

public sealed class ModuleStructureCatalogTests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static GameDataRegistry Load(bool output) => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(output ? AppContext.BaseDirectory : ClientRoot, "Settings.json"), out _, out _);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void All_modules_structure_150(bool output)
    {
        var registry = Load(output);
        var modules = Enumerable.Range(0, registry.ModuleTypes.Count)
            .Select(registry.ModuleTypes.GetDefinition).ToArray();
        Assert.NotEmpty(modules);
        Assert.All(modules, module => Assert.True(module.StructurePointsMax > 0));
        // Drone bays carry their own structure points (S 150, M 250); every other module stays 150.
        Assert.All(modules.Where(m => m.CategoryTypeId != "module.drone-bay"), module => Assert.Equal(150, module.StructurePointsMax));
        Assert.Contains(modules, m => m.TypeId == "module.bridge.navigation.computer.basic");
        Assert.Contains(modules, m => m.TypeId == "living.quarters.mk1.three-cabins");
        Assert.Contains(modules, m => m.TypeId == "module.engine.strizh");
        Assert.Contains(modules, m => m.TypeId == "module.drone-bay.small" && m.StructurePointsMax == 150);
        Assert.Contains(modules, m => m.TypeId == "module.drone-bay.medium" && m.StructurePointsMax == 250);
        Assert.DoesNotContain(modules, m => m.TypeId == "module.countermeasure.launcher.single");
    }
}
