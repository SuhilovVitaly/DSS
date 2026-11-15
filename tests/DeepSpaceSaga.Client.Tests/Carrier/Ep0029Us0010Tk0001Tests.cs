using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0010Tk0001Tests : IDisposable
{
    // The real shipped W4 pilot portrait shared with the crew record (starter-carrier.json).
    private const string PilotAssetId = "Images/Persons/W4/Portraits/CHR-20260915-102448-E2757F.png";
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"dss-ep0010-tk0001-{Guid.NewGuid():N}");
    public Ep0029Us0010Tk0001Tests() => Directory.CreateDirectory(Path.Combine(_dir, "Data", "UI"));
    public void Dispose() { try { Directory.Delete(_dir, recursive: true); } catch { } }

    private void WriteCatalog(params (string Id, string Portrait, bool Female)[] pilots)
    {
        var json = new JsonObject { ["version"] = 1, ["pilots"] = new JsonObject() };
        foreach (var (id, portrait, female) in pilots)
            json["pilots"]![id] = new JsonObject { ["portrait"] = portrait, ["female"] = female };
        File.WriteAllText(Path.Combine(_dir, "Data", "UI", "tactical-battle-pilots.json"), json.ToJsonString());
    }

    private void CopyClientAsset(string assetId, string? targetRelative = null)
    {
        string target = Path.Combine(_dir, (targetRelative ?? assetId).Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(Path.Combine(ClientRoot, assetId.Replace('/', Path.DirectorySeparatorChar)), target, overwrite: true);
    }

    private void WriteTinyPortrait(string relative)
    {
        string path = Path.Combine(_dir, relative.Replace('/', Path.DirectorySeparatorChar)) + ".png";
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var bitmap = new SKBitmap(2, 2);
        bitmap.Erase(SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(path);
        png.SaveTo(file);
    }

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

    private static TacticalBuildSnapshot Build(string pilotId, string pilotName, CrewGender? gender,
        string? portraitAssetId = null) =>
        new("frame", pilotId, pilotName, "gun", "Gun", 1, 2, 0, 0, 10, 0, null, 0,
            "armor", "Armor", null, null, 0, 0, 0, 1, 1, 1, 1, 1)
        { PilotGender = gender, PilotPortraitAssetId = portraitAssetId };

    private static TacticalUnitSnapshot Unit(TacticalBuildSnapshot build) => new(
        Id: "u", Side: "A", Kind: TacticalUnitKind.Drone, FrameId: "frame",
        Pose: new TacticalPoseSnapshot(0, 0, 0), Speed: 10, Hull: 10, MaxHull: 10, Armor: 5, MaxArmor: 5,
        State: TacticalUnitState.Alive, RoleId: null, Jammed: false,
        Engines: ImmutableArray<TacticalEngineSnapshot>.Empty, Limits: null, PreviousTurn: 0, Build: build,
        Accuracy: 0, Maneuver: 0);

    private static TacticalBattleFrame Frame(TacticalUnitSnapshot unit) => new(
        new TacticalBattleSnapshot(
            BattleId: "battle", Revision: 1, Phase: TacticalBattlePhase.Fire, Turn: 1, PlayerSide: "A", Seed: 1,
            ScenarioId: "scenario", ScenarioName: "Scenario", Tactics: [], Units: [unit], Suggestions: [],
            Paths: [], Targets: [], Incoming: [], Shots: [], BeforeSalvoUnits: [], FireSkipped: false,
            Rules: new TacticalForecastRules(new TacticalHitRulesSnapshot(50, 5, 95, 0, 0, 0, 0), 0.5, 90, 1),
            Losses: [], Outcome: null, Log: []),
        new TacticalBattleView(1920, 1080, 0, 0, 10), new SKRect(1680, 0, 1920, 720), 1, false, "");

    [Fact]
    public void SharedCrewPortraitAssetWinsOverLegacyAlias()
    {
        // The legacy alias points at a valid but different 2x2 image; the shared crew portrait is the
        // real 1024x1536 W4 asset, so dimensions prove which one the catalog decoded.
        WriteCatalog(("yun", "W4/Portraits/legacy", false));
        WriteTinyPortrait("Images/Persons/W4/Portraits/legacy");
        CopyClientAsset(PilotAssetId);
        using var catalog = PilotPortraitCatalog.Load(_dir);

        using var expected = SKBitmap.Decode(Path.Combine(ClientRoot, PilotAssetId.Replace('/', Path.DirectorySeparatorChar)));
        Assert.NotNull(expected);
        Assert.True(catalog.TryGet("yun", PilotAssetId, out var image));
        Assert.Equal(expected.Width, image.Width);   // the real W4 asset, not the 2x2 alias image
        Assert.Equal(expected.Height, image.Height);
        Assert.Equal(new SKRect(170, 70, 854, 754), catalog.SourceRect("yun", PilotAssetId));

        var unit = Unit(Build("yun", "Audrey", CrewGender.Female, PilotAssetId));
        var card = Assert.Single(PilotCardModel.Project(Frame(unit)));
        Assert.Equal(PilotAssetId, card.PortraitAssetId);
        Assert.True(PilotCardModel.IsFemale(unit, catalog));
        Assert.Equal("Audrey", PilotCardModel.Name(unit));
    }

    [Fact]
    public void LegacyAliasStillWorksWhenAssetMissing()
    {
        // The shared crew asset id does not resolve; the legacy pilot alias must still supply the portrait.
        WriteCatalog(("yun", "W4/Portraits/legacy", false));
        CopyClientAsset(PilotAssetId, "Images/Persons/W4/Portraits/legacy.png");
        using var catalog = PilotPortraitCatalog.Load(_dir);

        const string missingAsset = "Images/Persons/W4/Portraits/absent.png";
        Assert.True(catalog.TryGet("yun", missingAsset, out var image));
        Assert.True(image.Width > 2);
        Assert.Equal(new SKRect(170, 70, 854, 754), catalog.SourceRect("yun", missingAsset));
        Assert.True(catalog.TryGet("yun", out var legacy));
        Assert.Equal(legacy.Width, image.Width);

        // Identity is unchanged when the asset is missing.
        var unit = Unit(Build("yun", "Audrey", CrewGender.Female, missingAsset));
        var card = Assert.Single(PilotCardModel.Project(Frame(unit)));
        Assert.Equal(missingAsset, card.PortraitAssetId); // the card reports the requested asset id
        Assert.Equal("Audrey", PilotCardModel.Name(unit));
    }

    [Fact]
    public void AssetPathTraversalIsRejectedAndFallsBack()
    {
        WriteCatalog(("yun", "W4/Portraits/legacy", false));
        WriteTinyPortrait("Images/Persons/W4/Portraits/legacy");
        using var catalog = PilotPortraitCatalog.Load(_dir);

        Assert.False(catalog.TryGet("missing-pilot", Path.Combine(_dir, PilotAssetId), out _)); // rooted path rejected
        Assert.False(catalog.TryGet("missing-pilot", "../outside.png", out _));
        Assert.False(catalog.TryGet("missing-pilot", "Images/Persons/W4/Portraits/not-a-png.txt", out _));
        Assert.True(catalog.TryGet("yun", "../outside.png", out _)); // rejected asset falls back to the valid alias
        Assert.Equal(new SKRect(170, 70, 854, 754), catalog.SourceRect("yun", "../outside.png"));
    }

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
