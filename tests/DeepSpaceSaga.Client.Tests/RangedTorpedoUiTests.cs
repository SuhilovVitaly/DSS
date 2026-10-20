using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Client.Tests.TorpedoCommandPanelTests;

namespace DeepSpaceSaga.Client.Tests;

public class RangedTorpedoUiTests
{
    [Theory]
    [InlineData(200d, 1999.99, true)]
    [InlineData(200d, 2000, true)]
    [InlineData(200d, 2000.01, false)]
    [InlineData(50d, 500, true)]
    [InlineData(50d, 500.01, false)]
    [InlineData(null, 60, false)]
    public async Task Out_of_range_fire_is_disabled(double? range, double distance, bool enabled)
    {
        await using var f = new Fixture();
        SelectTarget(f);
        f.Update(s => s with
        {
            Objects = s.Objects.SetItem(1, s.Objects[1] with { Y = s.Objects[0].Y + distance }),
            InstalledModules = [s.InstalledModules[0] with { LauncherCombat = s.InstalledModules[0].LauncherCombat! with { RangeKm = range } }]
        });
        Render(f.Screen);
        Assert.Equal(enabled, Button(f.Screen).Enabled);
        if (!enabled)
        {
            Assert.Contains(range is null ? Localization.Get("Combat.RangeUnknown") : Localization.Get("Combat.OutOfRange"), Status(f));
            ClickFire(f);
            Assert.Empty(f.Connection.Commands);
        }
    }

    [Fact]
    public async Task Selection_change_does_not_retarget_pending_fire()
    {
        await using var f = new Fixture();
        SelectTarget(f);
        ClickFire(f);
        var sent = Assert.Single(f.Connection.Commands);
        f.Screen.OnMouseDown(740, 420);
        Render(f.Screen);
        Assert.Equal("hover", f.Screen.SelectedObjectId);
        Assert.Equal("target", sent.TargetObjectId);
        Assert.False(Button(f.Screen).Enabled);
        Assert.Equal("Sending...", Status(f));
    }

    [Fact]
    public async Task Boundary_rejection_clears_only_matching_pending()
    {
        await using var f = new Fixture();
        SelectTarget(f);
        ClickFire(f);
        var first = Assert.Single(f.Connection.Commands);
        void Reject(PlayerCommand command) => f.Update(s => s with
        {
            CommandResults = [new(command.CommandId, command.ObjectId, command.ModuleId, command.CommandType,
                CommandResultStatus.Rejected, 0, "target_out_of_range")]
        });
        Reject(first with { CommandId = "unrelated" });
        Render(f.Screen);
        Assert.False(Button(f.Screen).Enabled);
        Assert.Equal("Sending...", Status(f));
        Reject(first);
        f.Update(s => s with { CommandResults = [] }); // Coalesced snapshots still retain the matching receipt.
        Render(f.Screen);
        Assert.True(Button(f.Screen).Enabled);
        Assert.Contains(Localization.Get("Combat.LaunchRejectedOutOfRange"), Status(f));
        ClickFire(f);
        var second = f.Connection.Commands[1];
        Assert.NotEqual(first.CommandId, second.CommandId);
        Reject(first);
        Render(f.Screen);
        Assert.False(Button(f.Screen).Enabled);
        Reject(second);
        Render(f.Screen);
        Assert.True(Button(f.Screen).Enabled);
        Assert.True(f.Handle.Buffer.Latest!.Snapshot.InstalledModules[0].LauncherCombat!.ActiveTorpedoIds.IsDefaultOrEmpty);
    }

    private static string? Status(Fixture f) => f.Screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher").StatusBarText;
}
