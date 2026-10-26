using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client.Tests;

public sealed class AppPathsTests : IDisposable
{
    private readonly string _root;
    public AppPathsTests()
    {
        string repository = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(repository, "DeepSpaceSaga.sln")))
            repository = Directory.GetParent(repository)?.FullName ?? throw new DirectoryNotFoundException("Repository not found");
        string evidence = Environment.GetEnvironmentVariable("DSS_DIAGNOSTICS_DIRECTORY") ?? Path.GetFullPath(Path.Combine(repository, "../artifacts/validation/ep-0015-ubuntu-support/app-path-tests"));
        _root = Path.Combine(evidence, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }
    public void Dispose() => Directory.Delete(_root, recursive: true);
    private string At(string name) => Path.GetFullPath(Path.Combine(_root, name));
    private AppPaths Resolve(string platform, Dictionary<string, string>? environment = null) =>
        AppPaths.Resolve(At("content"), platform, At("home"), key => environment?.GetValueOrDefault(key));

    [Fact]
    public void UnixUsesAbsoluteXdgRoots()
    {
        var paths = Resolve("unix", new() { ["XDG_DATA_HOME"] = At("data"), ["XDG_CONFIG_HOME"] = At("config") });
        Assert.Equal(At("data/DeepSpaceSaga"), paths.UserData);
        Assert.Equal(At("config/DeepSpaceSaga"), paths.UserConfig);
        Assert.Equal(At("content/Scenarios"), paths.Scenarios);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative/ignored")]
    public void UnixIgnoresUnsetEmptyAndRelativeXdgValues(string? value)
    {
        var paths = Resolve("unix", new() { ["XDG_DATA_HOME"] = value!, ["XDG_CONFIG_HOME"] = value! });
        Assert.Equal(At("home/.local/share/DeepSpaceSaga"), paths.UserData);
        Assert.Equal(At("home/.config/DeepSpaceSaga"), paths.UserConfig);
    }
    [Fact]
    public void WindowsUsesLocalAppDataForBothRoots()
    {
        var paths = Resolve("windows", new() { ["LOCALAPPDATA"] = At("local") });
        Assert.Equal(At("local/DeepSpaceSaga"), paths.UserData);
        Assert.Equal(paths.UserData, paths.UserConfig);
    }
    [Fact]
    public void PortableMarkerKeepsConfigurationAndDataBesideExecutable()
    {
        Directory.CreateDirectory(At("content"));
        File.WriteAllText(At("content/portable.txt"), "");
        var paths = Resolve("unix");
        Assert.True(paths.Portable);
        Assert.Equal(paths.Content, paths.UserData);
        Assert.Equal(paths.Content, paths.UserConfig);
        using (AppPaths.Use(paths)) Assert.Same(paths, AppPaths.Current);
    }
    private AppPaths Template()
    {
        var paths = Resolve("unix");
        Directory.CreateDirectory(paths.Content);
        File.WriteAllText(Path.Combine(paths.Content, "Settings.json"), "{\"gameSettings\":{\"language\":\"English\"},\"defaultScenario\":\"Scenarios/test/scenario.json\"}");
        Directory.CreateDirectory(Path.Combine(paths.Content, "Saves"));
        File.WriteAllText(Path.Combine(paths.Content, "Saves/legacy.json"), "legacy bytes");
        return paths;
    }
    [Fact]
    public void FirstLaunchCopiesTemplateAndLegacySavesOnceWithoutDeletingOriginals()
    {
        var paths = Template();
        var messages = new List<string>();
        byte[] original = File.ReadAllBytes(Path.Combine(paths.Content, "Settings.json"));
        paths.Initialize(messages.Add);
        Assert.Equal("legacy bytes", File.ReadAllText(Path.Combine(paths.Saves, "legacy.json")));
        Assert.Equal("legacy bytes", File.ReadAllText(Path.Combine(paths.Content, "Saves/legacy.json")));
        Assert.Equal(paths.Content, JsonNode.Parse(File.ReadAllText(paths.Settings))!["contentRoot"]!.GetValue<string>());
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(paths.Content, "Settings.json")));
        Assert.Single(messages);
        File.Delete(Path.Combine(paths.Saves, "legacy.json"));
        paths.Initialize(messages.Add);
        Assert.Empty(Directory.EnumerateFiles(paths.Saves));
        Assert.Single(messages);
    }
    [Fact]
    public void ExistingSettingsAndSavesArePreservedWhileInstallationRootUpdates()
    {
        var paths = Template();
        Directory.CreateDirectory(paths.UserConfig); Directory.CreateDirectory(paths.Saves);
        File.WriteAllText(paths.Settings, "{\"contentRoot\":\"old-installation\",\"gameSettings\":{\"language\":\"Russian\",\"uiScale\":1.2}}");
        File.WriteAllText(Path.Combine(paths.Saves, "legacy.json"), "current bytes");
        paths.Initialize();
        var settings = JsonNode.Parse(File.ReadAllText(paths.Settings))!;
        Assert.Equal("Russian", settings["gameSettings"]!["language"]!.GetValue<string>());
        Assert.Equal(1.2, settings["gameSettings"]!["uiScale"]!.GetValue<double>());
        Assert.Equal(paths.Content, settings["contentRoot"]!.GetValue<string>());
        Assert.Equal("current bytes", File.ReadAllText(Path.Combine(paths.Saves, "legacy.json")));
        Assert.Empty(Directory.EnumerateFiles(paths.UserConfig, "*.tmp-*"));
    }
    [Fact]
    public async Task ExternalConfigurationCreatesRealSessionAndWritesSaveOnlyToUserData()
    {
        string repository = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(repository, "DeepSpaceSaga.sln"))) repository = Directory.GetParent(repository)!.FullName;
        string content = Path.Combine(repository, "src/DeepSpaceSaga.Client");
        var paths = new AppPaths(content, At("data"), At("config"), false);
        paths.Initialize();
        await using var connection = LocalGameSessionConnection.CreateFromSettingsFile(paths.Settings, paths.Saves);
        await connection.SaveAsync("ubuntu-path-check");
        Assert.True(File.Exists(Path.Combine(paths.Saves, "ubuntu-path-check.json")));
        Assert.False(File.Exists(Path.Combine(content, "Saves/ubuntu-path-check.json")));
        await using var restored = LocalGameSessionConnection.CreateFromSaveFile(paths.Settings,
            Path.Combine(paths.Saves, "ubuntu-path-check.json"), paths.Saves);
        Assert.Single(Directory.EnumerateFiles(paths.Saves, "ubuntu-path-check.json"));
    }
}
