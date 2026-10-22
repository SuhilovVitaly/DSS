using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client.Tests;

public sealed class DebugTuningDefaultsFileTests : IDisposable
{
    private readonly string _directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/ep0013/defaults-fixtures", Guid.NewGuid().ToString("N"));
    private static string SettingsPath => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "../../../../../src/DeepSpaceSaga.Client/Settings.json"));
    private string ReferencePath => Path.Combine(_directory, "debug-tuning.defaults.json");

    [Fact]
    public void Application_export_overwrites_reference_from_catalog_and_never_reads_current_file()
    {
        Directory.CreateDirectory(_directory);
        string current = Path.Combine(_directory, "debug-tuning.json");
        File.WriteAllText(current, "invalid manually edited current file");
        Program.WriteDebugTuningDefaults(SettingsPath, _directory);
        Verify();
        File.WriteAllText(ReferencePath, "manually edited reference");
        Program.WriteDebugTuningDefaults(SettingsPath, _directory);
        Verify();
        Assert.Equal("invalid manually edited current file", File.ReadAllText(current));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp-*"));

        void Verify()
        {
            string json = File.ReadAllText(ReferencePath);
            using var document = JsonDocument.Parse(json);
            Assert.Equal("catalog", document.RootElement.GetProperty("source").GetString());
            Assert.Equal(5, document.RootElement.GetProperty("ships").GetProperty("module.engine.tetrarch").GetProperty("maxSpeedKmS").GetDouble());
            var catalog = LocalGameSessionConnection.LoadDebugTuningDefaults(SettingsPath);
            Assert.True(DebugTuningFileFormat.TryParse(json, catalog.Defaults, out var patch, out var reason), reason);
            Assert.Equal(25, patch.Values.Length);
            Assert.Equal(catalog.Defaults.ToArray(), patch.Values.ToArray());
        }
    }

    [Fact]
    public void Reference_export_uses_original_defaults_even_if_given_a_modified_snapshot()
    {
        var catalog = LocalGameSessionConnection.LoadDebugTuningDefaults(SettingsPath);
        var modified = catalog with { Revision = 10, Values = catalog.Values.SetItem(0, catalog.Values[0] with { Value = 12 }) };
        DebugTuningFileWriter.WriteDefaults(_directory, modified);
        Assert.True(DebugTuningFileFormat.TryParse(File.ReadAllText(ReferencePath), catalog.Defaults, out var patch, out _));
        Assert.Equal(5, patch.Values[0].Value);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
