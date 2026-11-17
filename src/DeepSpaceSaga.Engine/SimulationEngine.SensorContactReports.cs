using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private const string SensorReportDialogueId = "dialogue.sensor-pirate-contact";

    private void OpenPendingSensorReport(long time)
    {
        if (_demoCampaign.PendingSensorReport is not { } batch || CombatOrLootOpen) return;
        if (_dialogue.Active is not null)
        {
            _dialogue.ResumeSpeed = SimulationSpeed.Speed0;
            return;
        }
        string participant = batch.Reports[0].OperatorCrewId ?? "sensor-script";
        string? error = StartDialogue(SensorReportDialogueId, null, participant, batch.BatchId, time);
        if (error is not null) throw new ScenarioException("Cannot open sensor report: " + error);
        _dialogue.Active = _dialogue.Active! with
        {
            Parameters = ImmutableDictionary<string, string>.Empty.Add("sensorBatchId", batch.BatchId)
        };
        _dialogue.ResumeSpeed = SimulationSpeed.Speed0;
    }

    private void AcknowledgeSensorReport()
    {
        if (_demoCampaign.PendingSensorReport is null) return;
        _demoCampaign = _demoCampaign with
        {
            PendingSensorReport = null,
            Revision = checked(_demoCampaign.Revision + 1),
            CommitSequence = checked(_demoCampaign.CommitSequence + 1)
        };
    }

    private static void ValidateSensorDialogueSave(DemoCampaignData campaign, ScenarioFile scenario)
    {
        if (scenario.GameState.DialogueState?.ActiveDialogue is not { DialogueDefinitionId: SensorReportDialogueId } active) return;
        if (campaign.PendingSensorReport is not { } batch || active.Parameters is null ||
            !active.Parameters.TryGetValue("sensorBatchId", out var id) || id != batch.BatchId || active.StationObjectId is not null ||
            active.ParticipantId != (batch.Reports[0].OperatorCrewId ?? "sensor-script"))
            throw new ScenarioException("Saved sensor dialogue does not match its pending batch.");
        var crew = scenario.GameState.SpaceObjects.Single(s => s.ObjectId == scenario.GameState.PlayerShipObjectId).Crew?
            .SingleOrDefault(c => c.CrewId == active.ParticipantId);
        if (active.ParticipantId != "sensor-script" && crew is null || active.SpeakerDisplayName != (crew?.DisplayName ?? "sensor-script") ||
            active.SpeakerPortraitImage != crew?.PortraitAssetId)
            throw new ScenarioException("Saved sensor dialogue speaker does not match the crew.");
    }

    private DemoCampaignData AddSensorReport(DemoCampaignData campaign, string groupId, ObjectMotionSnapshot player, long time)
    {
        var world = campaign.PirateWorld!;
        int index = Enumerable.Range(0, world.Groups.Length).Single(i => world.Groups[i].GroupId == groupId);
        var group = world.Groups[index];
        if (group.ReportConsumed) return campaign;
        var sensorOperator = BuildSensorOperatorProjection();
        string? operatorId = sensorOperator?.AvailabilityReason is null ? sensorOperator?.CrewId : null;
        bool irina = operatorId == "CHR-PLAYER-DEFENSE";
        int error = (irina ? 1 : 2) * group.Members.Length;
        int total = campaign.PirateCompositions.Where(c => c.GroupId == groupId).Sum(c => c.Drones.Length);
        int estimate = total + PirateRandom(MasterSeed, "estimate:" + groupId).Next(-error, error + 1);
        double x = group.X - player.X, y = group.Y - player.Y;
        double angle = group.Direction * Math.PI / 180, playerAngle = player.Direction * Math.PI / 180;
        double vx = (Math.Sin(angle) * group.SpeedKmS - Math.Sin(playerAngle) * player.SpeedKmS) * 10;
        double vy = (-Math.Cos(angle) * group.SpeedKmS + Math.Cos(playerAngle) * player.SpeedKmS) * 10;
        double? eta = SensorEtaRealSeconds(x, y, vx, vy);
        var report = new SensorContactReport("report:" + groupId, groupId, operatorId, time,
            (Math.Atan2(x, -y) * 180 / Math.PI + 360) % 360, Distance(group.X, group.Y, player.X, player.Y),
            eta, eta is null ? "no_intercept" : null, estimate, group.Members.Select(m => m.SignatureTypeId).ToImmutableArray());
        var pending = campaign.PendingSensorReport;
        return campaign with
        {
            SensorReports = campaign.SensorReports.Add(report),
            PendingSensorReport = pending is null ? new("sensor-batch:" + time + ":" + groupId, [report])
                : pending with { Reports = pending.Reports.Add(report) },
            PirateWorld = world with { Groups = world.Groups.SetItem(index, group with { ReportConsumed = true, ApproximateDroneCount = estimate }) }
        };
    }

    internal static double? SensorEtaRealSeconds(double x, double y, double vx, double vy)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(vx) || !double.IsFinite(vy)) return null;
        double scale = Math.Max(200, Math.Max(Math.Max(Math.Abs(x), Math.Abs(y)), Math.Max(Math.Abs(vx), Math.Abs(vy))));
        x /= scale; y /= scale; vx /= scale; vy /= scale;
        double radius = 200 / scale, c = x * x + y * y - radius * radius;
        if (c <= 0) return 0;
        double a = vx * vx + vy * vy, b = x * vx + y * vy;
        if (a == 0 || b >= 0) return null;
        double discriminant = b * b - a * c;
        if (discriminant < -1e-14 * Math.Max(b * b, a * c)) return null;
        double seconds = 4 * c / (-b + Math.Sqrt(Math.Max(0, discriminant)));
        return double.IsFinite(seconds) ? seconds : null;
    }
}
