using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private DemoCampaignData _demoCampaign = new();
    private DemoCampaignDefinitions _demoDefinitions = new(null, null);

    internal void ConfigureDemoCampaign(DemoCampaignDefinitions definitions)
    {
        definitions.ValidateReferences(_registry);
        _demoDefinitions = definitions;
    }

    public DemoCampaignResponse ExecuteDemoCampaign(DemoCampaignCommand command)
    {
        lock (_worldStateLock)
        {
            if (command is null || string.IsNullOrWhiteSpace(command.CommandId))
                return new(command?.CommandId ?? "", false, DemoCampaignReasons.InvalidIdentity, _demoCampaign.Revision);

            // Serialization identifies payloads; dispatch remains explicit partial handlers, never reflection.
            var bytes = Encoding.UTF8.GetBytes(command.GetType().FullName + "\n" + JsonSerializer.Serialize(command, command.GetType()));
            string hash = Convert.ToHexString(SHA256.HashData(bytes));
            var previous = _demoCampaign.Receipts.FirstOrDefault(r => r.Response.CommandId == command.CommandId);
            if (previous is not null)
                return previous.PayloadHash == hash ? previous.Response
                    : new(command.CommandId, false, DemoCampaignReasons.IdentityConflict, _demoCampaign.Revision);

            string? reason = null;
            var candidate = _demoCampaign;
            bool handled = false;
            if (command.ExpectedRevision != _demoCampaign.Revision) reason = DemoCampaignReasons.StaleRevision;
            else if (_tacticalBattleSession is not null) reason = DemoCampaignReasons.BattleActive;
            else if (_objects.Any(o => o.InitialMotion.ObjectId == PlayerShipObjectId && o.IsDestroyed))
                reason = DemoCampaignReasons.CarrierDestroyed;
            else
                PrepareDemoCampaignCommand(command, ref candidate, ref handled, ref reason);
            PrepareStationJobsCommand(command, ref candidate, ref handled, ref reason); // EP-0034 US-0006, explicit dispatch
            var boarding = PreparePassengerBoarding(command, ref candidate, handled, ref reason); // EP-0034 US-0002

            reason ??= handled ? null : DemoCampaignReasons.Unsupported;
            bool applied = handled && reason is null;
            long revision = applied ? checked(_demoCampaign.Revision + 1) : _demoCampaign.Revision;
            long sequence = checked(_demoCampaign.CommitSequence + 1);
            var response = new DemoCampaignResponse(command.CommandId, applied, reason, revision);
            candidate = (applied ? candidate : _demoCampaign) with
            {
                Revision = revision,
                CommitSequence = sequence,
                Receipts = (_demoCampaign.Receipts.Length >= 1024 ? _demoCampaign.Receipts.RemoveRange(0, _demoCampaign.Receipts.Length - 1023) : _demoCampaign.Receipts).Add(new(hash, sequence, response))
            };
            ValidateDemoCampaignSections(candidate);
            _demoCampaign = candidate;
            if (applied && boarding is not null) BoardPassengers(boarding); // EP-0034 US-0002: same commit
            return response;
        }
    }

    private void TickDemoCampaign(long gameTimeMs)
    {
        PublishStationJobs(gameTimeMs); // EP-0034 US-0006: idempotent per calendar bucket, also while paused
        DeliverPassengerContracts(gameTimeMs); // EP-0034 US-0002: idempotent, a Completed contract never pays again
        if (gameTimeMs <= _demoCampaign.LastTickGameTimeMs) return;
        var candidate = _demoCampaign;
        TickDemoCampaignSections(gameTimeMs, ref candidate);
        ValidateDemoCampaignSections(candidate);
        _demoCampaign = candidate with { LastTickGameTimeMs = gameTimeMs };
    }

    private DemoCampaignSnapshot BuildDemoCampaignProjection() => new(
        _demoCampaign.Revision, _demoCampaign.CommitSequence, BuildDemoCompanyProjection(),
        BuildDemoTransportProjection(), _demoCampaign.Encounters, _demoCampaign.Rewards,
        _demoCampaign.Progression, _demoCampaign.Story, BuildDemoStartProjection());

    private DemoCampaignData CaptureDemoCampaignState() => _demoCampaign;

    // Hooks must prepare immutable candidate state only; rejection cannot mutate the live world.
    partial void PrepareDemoCampaignCommand(DemoCampaignCommand command, ref DemoCampaignData candidate,
        ref bool handled, ref string? reason);
    partial void TickDemoCampaignSections(long gameTimeMs, ref DemoCampaignData candidate);
    partial void ValidateDemoCampaignSections(DemoCampaignData candidate);
}
