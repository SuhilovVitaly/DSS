using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

public sealed record LootConsumableData(string ItemTypeId, long OriginalQuantity, long Remaining, bool Destroyed);
public sealed record LootJettisonData(string CommandId, string ItemTypeId, long Quantity);
public sealed record LootScrapData(string ItemTypeId, long Quantity, int ChanceBasisPoints, string State = LootAttemptStates.NotAttempted);
public sealed record LootModuleData(string InstanceId, string Category, string CatalogId, string ItemTypeId,
    bool Destroyed, int ChanceBasisPoints, string State = LootAttemptStates.NotAttempted);
public sealed record LootSourceData(string ObjectId, string Kind, string LayoutId, double X, double Y, double Direction,
    LootPilotSnapshot? Pilot, ImmutableArray<LootConsumableData> Consumables, LootScrapData? Scrap,
    ImmutableArray<LootModuleData> Modules);
public sealed record BattleLootData(string BattleId, string Phase, string CatalogFingerprint, ulong Seed, long DrawCount,
    TacticalBattleSnapshot Map, ImmutableArray<LootSourceData> Sources)
{
    public ImmutableArray<LootJettisonData> Jettison { get; init; } = [];
    internal static void Validate(BattleLootData? data)
    {
        if (data is null) return;
        void Require(bool valid) { if (!valid) throw new ScenarioException("Invalid saved battle loot ledger."); }
        Require(!string.IsNullOrWhiteSpace(data.BattleId) && data.Phase is LootPhases.Open or LootPhases.Closed &&
            data.CatalogFingerprint is { Length: 64 } && data.CatalogFingerprint.All(Uri.IsHexDigit) &&
            data.DrawCount >= 0 && data.Map is not null && data.Map.BattleId == data.BattleId &&
            !data.Sources.IsDefault && data.Sources.All(s => s is not null));
        Require(data.Sources.Select(s => s.ObjectId).Distinct(StringComparer.Ordinal).Count() == data.Sources.Length);
        Require(!data.Jettison.IsDefault && data.Jettison.All(j => j is not null && !string.IsNullOrWhiteSpace(j.CommandId) && !string.IsNullOrWhiteSpace(j.ItemTypeId) && j.Quantity > 0) &&
            data.Jettison.Select(j => j.CommandId).Distinct(StringComparer.Ordinal).Count() == data.Jettison.Length);
        long expectedDraws = 0;
        bool Attempt(string state) => state is LootAttemptStates.NotAttempted or LootAttemptStates.Failed or LootAttemptStates.Collected;
        foreach (var s in data.Sources)
        {
            Require(!string.IsNullOrWhiteSpace(s.ObjectId) && !string.IsNullOrWhiteSpace(s.LayoutId) &&
                s.Kind is "DisabledDrone" or "DestroyedDrone" or "Carrier" or "SelfDestructCarrier" && double.IsFinite(s.X) && double.IsFinite(s.Y) && double.IsFinite(s.Direction) &&
                !s.Consumables.IsDefault && !s.Modules.IsDefault && s.Consumables.All(c => c is not null) && s.Modules.All(m => m is not null));
            Require(s.Consumables.Select(c => c.ItemTypeId).Distinct(StringComparer.Ordinal).Count() == s.Consumables.Length &&
                s.Modules.Select(m => m.InstanceId).Distinct(StringComparer.Ordinal).Count() == s.Modules.Length);
            Require(s.Kind == "DisabledDrone" ? s.Scrap is null : s.Pilot is null && s.Consumables.IsEmpty && s.Modules.IsEmpty && s.Scrap is not null);
            foreach (var c in s.Consumables)
            {
                Require(!string.IsNullOrWhiteSpace(c.ItemTypeId) && c.OriginalQuantity > 0 && c.Remaining >= 0 &&
                    c.Remaining <= c.OriginalQuantity && (!c.Destroyed || c.Remaining == 0));
                expectedDraws++;
            }
            foreach (var m in s.Modules)
            {
                Require(!string.IsNullOrWhiteSpace(m.InstanceId) && !string.IsNullOrWhiteSpace(m.ItemTypeId) &&
                    m.ChanceBasisPoints is >= 0 and <= 10000 && Attempt(m.State) && (!m.Destroyed || m.State == LootAttemptStates.NotAttempted));
                expectedDraws += 1 + (m.State == LootAttemptStates.NotAttempted ? 0 : 1);
            }
            if (s.Scrap is { } scrap)
            {
                Require(!string.IsNullOrWhiteSpace(scrap.ItemTypeId) && scrap.Quantity > 0 && scrap.ChanceBasisPoints is >= 0 and <= 10000 && Attempt(scrap.State));
                if (scrap.State != LootAttemptStates.NotAttempted) expectedDraws++;
            }
        }
        Require(data.DrawCount == expectedDraws);
    }
}
