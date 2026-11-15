using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0010Tk0001Tests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"dss-ep0010-tk0001-{Guid.NewGuid():N}");
    public Ep0029Us0010Tk0001Tests() => Directory.CreateDirectory(Path.Combine(_dir, "Data", "UI"));
    public void Dispose() { try { Directory.Delete(_dir, recursive: true); } catch { } }

    private PilotPortraitCatalog LoadCatalog(bool yunFemale)
    {
        var json = new JsonObject
        {
            ["version"] = 1,
            ["pilots"] = new JsonObject
            {
                ["yun"] = new JsonObject { ["portrait"] = "W4/Portraits/nonexistent", ["female"] = yunFemale },
                ["vera"] = new JsonObject { ["portrait"] = "W4/Portraits/nonexistent", ["female"] = true },
            },
        };
        File.WriteAllText(Path.Combine(_dir, "Data", "UI", "tactical-battle-pilots.json"), json.ToJsonString());
        return PilotPortraitCatalog.Load(_dir);
    }

    private static TacticalBuildSnapshot Build(string pilotId, string pilotName, CrewGender? gender) =>
        new("frame", pilotId, pilotName, "gun", "Gun", 1, 2, 0, 0, 10, 0, null, 0,
            "armor", "Armor", null, null, 0, 0, 0, 1, 1, 1, 1, 1)
        { PilotGender = gender };

    private static TacticalUnitSnapshot Unit(TacticalBuildSnapshot build) => new(
        Id: "u", Side: "A", Kind: TacticalUnitKind.Drone, FrameId: "frame",
        Pose: new TacticalPoseSnapshot(0, 0, 0), Speed: 10, Hull: 10, MaxHull: 10, Armor: 5, MaxArmor: 5,
        State: TacticalUnitState.Alive, RoleId: null, Jammed: false,
        Engines: ImmutableArray<TacticalEngineSnapshot>.Empty, Limits: null, PreviousTurn: 0, Build: build,
        Accuracy: 0, Maneuver: 0);

    [Fact]
    public void OwnedPortraitOverridesLegacyAlias()
    {
        using var catalog = LoadCatalog(yunFemale: false); // legacy alias says male
        var unit = Unit(Build("yun", "Audrey", CrewGender.Female)); // owned crew says female

        Assert.True(PilotCardModel.IsFemale(unit, catalog));
        Assert.Equal("Audrey", PilotCardModel.Name(unit));
    }

    [Fact]
    public void MissingPortraitKeepsIdentity()
    {
        using var catalog = LoadCatalog(yunFemale: false);
        // No PilotGender on the build: falls back to the legacy alias, and the name is preserved.
        var unit = Unit(Build("vera", "Vera", null));

        Assert.True(PilotCardModel.IsFemale(unit, catalog)); // legacy "vera" is female
        Assert.Equal("Vera", PilotCardModel.Name(unit));
    }

    [Fact]
    public void SessionSwitchDropsOldPortraitBindings()
    {
        using var catalog = LoadCatalog(yunFemale: true);
        // Two units share the same legacy alias but belong to different crews: each reads its own gender.
        var audrey = Unit(Build("yun", "Audrey", CrewGender.Female));
        var oleg = Unit(Build("yun", "Oleg", CrewGender.Male));

        Assert.True(PilotCardModel.IsFemale(audrey, catalog));
        Assert.False(PilotCardModel.IsFemale(oleg, catalog)); // no inherited cache from the previous crew
    }
}
