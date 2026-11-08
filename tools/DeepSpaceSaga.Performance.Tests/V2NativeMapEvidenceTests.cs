using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Performance.Tests;

public sealed class V2NativeMapEvidenceTests
{
    [Fact]
    public void RouteRequiresObservedDockLoadAndReturn()
    {
        string directory = V2MapAcceptanceReportTests.NewDirectory();
        try
        {
            var start = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
                [new("ship", 10, 20, 0, 0)], "ship",
                SolarSystemMap: new(2, 42, 1000000, [], [], []),
                ClusterMap: new(2, "c", [], [], [], Geography: new(4, 1, 1, 3)));
            var dock = start with { Objects = [start.Objects[0] with { IsDocked = true, DockedStationObjectId = "station" }] };
            void Write(string phase, AuthoritativeSnapshot s) => File.WriteAllText(Path.Combine(directory, phase + ".json"), JsonSerializer.Serialize(s));
            Write("start", start); Write("approach", start with { Objects = [start.Objects[0] with { NavigationTargetObjectId = "station" }] });
            Write("docked", dock); Write("loaded", dock); Write("returned", dock);
            Assert.True(V2NativeMapAcceptance.RouteComplete(directory, out string error), error);
            Write("loaded", start);
            Assert.False(V2NativeMapAcceptance.RouteComplete(directory, out _));
            Write("loaded", dock with { Objects = [dock.Objects[0] with { X = 999 }] });
            Assert.False(V2NativeMapAcceptance.RouteComplete(directory, out _));
            Write("loaded", dock); File.Delete(Path.Combine(directory, "returned.json"));
            Assert.False(V2NativeMapAcceptance.RouteComplete(directory, out _));
        }
        finally { Directory.Delete(directory, true); }
    }
    [Fact]
    public void ScriptedPassDoesNotImplyHumanAcceptance()
    {
        Assert.False(V2NativeMapAcceptance.AcceptanceComplete(true, "NOT RUN", "NOT RUN", true, true));
        Assert.False(V2NativeMapAcceptance.AcceptanceComplete(false, "passed", "passed", true, true));
        Assert.False(V2NativeMapAcceptance.AcceptanceComplete(true, "passed", "passed", false, true));
        Assert.False(V2NativeMapAcceptance.AcceptanceComplete(true, "passed", "passed", true, false));
    }
    [Fact]
    public void CleanupNeverRemovesForeignSlots()
    {
        string parent = V2MapAcceptanceReportTests.NewDirectory();
        try
        {
            string foreign = Path.Combine(parent, "quicksave.json"); File.WriteAllText(foreign, "foreign");
            string owned = V2NativeMapAcceptance.CreateOwnedSlots(parent);
            File.WriteAllText(Path.Combine(owned, "quicksave.json"), "owned");
            V2NativeMapAcceptance.CleanupOwnedSlots(parent, owned);
            Assert.Equal("foreign", File.ReadAllText(foreign));
            Assert.Throws<InvalidOperationException>(() => V2NativeMapAcceptance.CleanupOwnedSlots(parent, parent));
            Assert.Equal("foreign", File.ReadAllText(foreign));
        }
        finally { Directory.Delete(parent, true); }
    }
}
