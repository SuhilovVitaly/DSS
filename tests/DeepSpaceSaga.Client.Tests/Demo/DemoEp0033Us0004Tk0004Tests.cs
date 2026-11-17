using DeepSpaceSaga.Client.UI.Screens.Hire;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033Us0004Tk0004Tests
{
    [Fact]
    public async Task HiringHasPendingBarrierAndDoesNotRepeatOnReopen()
    {
        var wait = new TaskCompletionSource<DemoCampaignResponse>();
        int calls = 0;
        var screen = new HireScreen(service: (c, _) => { calls++; return new(wait.Task); });
        screen.Hire("offer"); screen.Hire("offer");
        Assert.Equal(1, calls);
        wait.SetResult(new("c", true, null, 1, Recruitment: new([], [], [], 2)));
        await screen.PendingRecruitment!;
        screen.OnDeactivated(); screen.OnActivated();
        Assert.Equal(1, calls);
        Assert.NotEmpty(screen.RecruitmentStatus);
    }
    [Fact]
    public async Task AssignmentCarriesRealIdentitiesAndLocalizesRejection()
    {
        DemoCampaignCommand? sent = null;
        var screen = new HireScreen(service: (c, _) => { sent = c; return ValueTask.FromResult(new DemoCampaignResponse(c.CommandId, false, "service_invalid_target", 1)); });
        screen.Assign("pilot", "drone"); await screen.PendingRecruitment!;
        Assert.Equal("pilot", Assert.IsType<AssignPilotCommand>(sent).CrewId);
        Assert.DoesNotContain("service_invalid_target", screen.RecruitmentStatus);
    }
}
