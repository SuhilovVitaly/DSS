using System.Reflection;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatReportIdentityTests
{
    private static BattleReportReporterSnapshot Capture(SimulationEngine engine, SpaceObjectRuntime owner,
        string? moduleId, BattleReportReporterRole role) =>
        (BattleReportReporterSnapshot)typeof(SimulationEngine).GetMethod("CaptureReportIdentity", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(engine, [owner, moduleId, role])!;

    private static SpaceObjectRuntime Owner() => new(new(Player, 0, 0, 0, 0), SpaceObjectType.PlayerShip, 0,
        [new("scanner", 0, [], "On", "Ready", 60, null, [], OperatorCrewId: "crew")],
        Crew: [new("crew", "Captured Name")], CaptainDisplayName: "Captain", CaptainPortraitImage: "captain.png");

    [Fact]
    public void AssignedOperatorIsCaptured()
    {
        using var engine = Create();
        var owner = Owner();
        var reporter = Capture(engine, owner, "scanner", BattleReportReporterRole.TorpedoOperator);
        owner = owner with { Crew = [new("crew", "Replacement")] };
        Assert.Equal("Captured Name", reporter.DisplayName);
        Assert.Equal("crew", reporter.CrewId);
        Assert.NotEqual(owner.Crew[0].DisplayName, reporter.DisplayName);
    }

    [Fact]
    public void MissingScannerIdentityUsesService()
    {
        using var engine = Create();
        var reporter = Capture(engine, Owner() with { Crew = ImmutableArray<CrewMemberRuntime>.Empty }, "scanner", BattleReportReporterRole.ScannerOperator);
        Assert.Equal(BattleReportReporterRole.ScannerOperator, reporter.Role);
        Assert.Null(reporter.CrewId);
        Assert.Null(reporter.DisplayName);
    }

    [Fact]
    public void MissingCrewPortraitStaysNull()
    {
        using var engine = Create();
        Assert.Null(Capture(engine, Owner(), "scanner", BattleReportReporterRole.CountermeasureOperator).PortraitImage);
    }

    [Fact]
    public void CaptainPortraitIsCaptured()
    {
        using var engine = Create();
        var reporter = Capture(engine, Owner(), null, BattleReportReporterRole.Captain);
        Assert.Equal("Captain", reporter.DisplayName);
        Assert.Equal("captain.png", reporter.PortraitImage);
    }

    [Fact]
    public void HiddenNamesAreNotProjected()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o with { IsKnown = false, Name = "Secret" }));
        var method = typeof(SimulationEngine).GetMethod("CaptureKnownParticipant", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.Equal(((string?, string?))(null, null), method.Invoke(engine, [Target]));
        Assert.Equal((Player, "Secret"), method.Invoke(engine, [Player]));
        Assert.Equal(((string?, string?))(null, null), method.Invoke(engine, ["missing"]));
    }
}
