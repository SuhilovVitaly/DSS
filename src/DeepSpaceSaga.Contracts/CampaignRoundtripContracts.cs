using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>Read-only campaign summary of the existing authoritative settlement receipt.</summary>
public sealed record CampaignRoundtripSnapshot(SortieSettlementSnapshot Settlement)
{
    [JsonIgnore] public string LastBattleId => Settlement.BattleId;
    [JsonIgnore] public int SettlementRevision => Settlement.CarrierRevision;
    [JsonIgnore] public bool CarrierDestroyed => Settlement.ShipDestroyed;
    [JsonIgnore] public ImmutableArray<string> LostDroneIds => Settlement.LostDroneIds;
    [JsonIgnore] public int ConsumedTorpedoes => Settlement.ConsumedTorpedoes;
}
