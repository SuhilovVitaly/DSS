using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine;

/// <summary>Immutable candidate overlay. The command owner publishes it only after complete validation.</summary>
internal sealed class DebugTuningState
{
    private static readonly string[] Engines = ["module.engine.tetrarch", "module.engine.strizh"];
    private static readonly string[] Launchers = ["module.torpedo.launcher.basic", "module.torpedo.launcher.light", "module.countermeasure.launcher.basic"];

    internal GameDataRegistry Registry { get; }
    internal DebugTuningSnapshot Snapshot { get; }

    internal DebugTuningState(GameDataRegistry catalog)
    {
        Registry = catalog;
        var defaults = ReadValues(catalog);
        Snapshot = new(0, defaults, defaults);
    }

    private DebugTuningState(GameDataRegistry registry, DebugTuningSnapshot snapshot)
    {
        Registry = registry;
        Snapshot = snapshot;
    }

    internal bool TryApply(DebugTuningPatch? patch, out DebugTuningState next, out string? reason)
    {
        next = this;
        reason = null;
        if (patch is null || patch.Values.IsDefault)
        {
            reason = DebugTuningReasonCodes.UnknownParameter;
            return false;
        }
        // Validate every entry before constructing any replacement registry or revision.
        foreach (var entry in patch.Values)
        {
            if (entry is null || !Snapshot.Defaults.Any(v => SameKey(v, entry)))
            {
                reason = DebugTuningReasonCodes.UnknownParameter;
                return false;
            }
            var (min, max, integral) = Bounds(entry.Field);
            if (!double.IsFinite(entry.Value) || entry.Value < min || entry.Value > max ||
                integral && entry.Value != Math.Truncate(entry.Value) || Snapshot.Revision == long.MaxValue)
            {
                reason = DebugTuningReasonCodes.ValueOutOfRange;
                return false;
            }
        }
        var definitions = ImmutableArray.CreateBuilder<ModuleTypeDefinition>(Registry.ModuleTypes.Count);
        for (int i = 0; i < Registry.ModuleTypes.Count; i++) definitions.Add(Registry.ModuleTypes.GetDefinition(i));
        foreach (var entry in patch.Values)
        {
            int index = Registry.ModuleTypes.GetIndex(entry.ModuleTypeId);
            var definition = definitions[index];
            if (entry.ProfileId is { } profile)
            {
                int p = 0;
                while (definition.Profiles[p].ProfileId != profile) p++;
                var current = definition.Profiles[p];
                definitions[index] = definition with
                {
                    Profiles = definition.Profiles.SetItem(p, entry.Field == "speedKmS"
                        ? current with { SpeedKmS = entry.Value } : current with { BlastRadiusKm = entry.Value })
                };
            }
            else definitions[index] = entry.Field switch
            {
                "maxSpeedKmS" => definition with { MaxSpeedMps = (int)Math.Round(entry.Value * 1000, MidpointRounding.AwayFromZero) },
                "accelerationMps2" => definition with { LinearInertiaMps2 = (int)entry.Value },
                "turnRateDegPerSec" => definition with { AngularInertiaDegPerSec = (int)entry.Value },
                "reloadMs" => definition with { ReloadMs = (long)entry.Value },
                _ => throw new InvalidOperationException("Validated tuning field was not mapped.")
            };
        }
        var registry = Registry.WithModuleTypes(definitions.MoveToImmutable());
        next = new(registry, new(Snapshot.Revision + 1, ReadValues(registry), Snapshot.Defaults));
        return true;
    }

    private static bool SameKey(DebugTuningValue a, DebugTuningValue b) =>
        a.ModuleTypeId == b.ModuleTypeId && a.Field == b.Field && a.ProfileId == b.ProfileId;

    private static (double Min, double Max, bool Integral) Bounds(string field) => field switch
    {
        "maxSpeedKmS" => (.1, 50, false),
        "accelerationMps2" => (10, 20000, true),
        "turnRateDegPerSec" => (1, 360, true),
        "reloadMs" => (100, 600000, true),
        "speedKmS" => (.1, 100, false),
        "blastRadiusKm" => (.05, 50, false),
        _ => throw new InvalidOperationException("Unknown tuning field.")
    };

    private static ImmutableArray<DebugTuningValue> ReadValues(GameDataRegistry registry)
    {
        var result = ImmutableArray.CreateBuilder<DebugTuningValue>();
        foreach (string id in Engines)
        {
            if (!registry.ModuleTypes.Contains(id)) continue;
            var type = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(id));
            if (type.MaxSpeedMps is { } speed) result.Add(new(id, "maxSpeedKmS", speed / 1000.0));
            if (type.LinearInertiaMps2 is { } acceleration) result.Add(new(id, "accelerationMps2", acceleration));
            if (type.AngularInertiaDegPerSec is { } turn) result.Add(new(id, "turnRateDegPerSec", turn));
        }
        foreach (string id in Launchers)
        {
            if (!registry.ModuleTypes.Contains(id)) continue;
            var type = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(id));
            if (type.ReloadMs is { } reload) result.Add(new(id, "reloadMs", reload));
            if (type.Profiles.IsDefault) continue;
            string[] profiles = id == Launchers[2] ? ["wide", "fast"] : ["slow", "medium", "fast"];
            foreach (string profile in profiles)
            {
                var p = type.Profiles.FirstOrDefault(p => p.ProfileId == profile);
                if (p is null) continue;
                result.Add(new(id, "speedKmS", p.SpeedKmS, profile));
                result.Add(new(id, "blastRadiusKm", p.BlastRadiusKm, profile));
            }
        }
        return result.ToImmutable();
    }
}
