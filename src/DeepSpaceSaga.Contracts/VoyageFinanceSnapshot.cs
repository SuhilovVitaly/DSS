using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

public static class VoyageFinanceStates
{
    public const string InTransit = "in_transit";
    public const string AwaitingRealization = "awaiting_realization";
    public const string Finalized = "finalized";
    public const string Interrupted = "interrupted";
}

/// <summary>Carried remainder at acquisition basis, never marked to market or included in net profit.</summary>
public sealed record VoyageCargoRemainderSnapshot(string ItemTypeId, long Quantity, long? CostBasisCredits);

/// <summary>
/// Engine-owned realized financial result. Net = sales - COGS - consumed route fuel - assessed port fees
/// - event costs + passenger payout - passenger penalty. Unknown COGS makes COGS/net unavailable;
/// known COGS is nonnegative, known net may be negative. Paid fees and outstanding debt are a breakdown
/// of assessed fees, not additional expenses. Transport arrival awaits sales until the next accepted
/// departure finalizes the record; interrupted records are terminal. Unsold cargo is not profit.
/// </summary>
public sealed record VoyageFinanceSnapshot(
    string VoyageId,
    string OriginStationObjectId,
    string? DestinationStationObjectId,
    long StartedGameTimeMs,
    long? CompletedGameTimeMs,
    string State,
    long GrossSalesCredits,
    long? CostOfGoodsSoldCredits,
    bool HasUnknownCostOfGoodsSold,
    long RouteFuelCostCredits,
    long PortFeesAssessedCredits,
    long PortFeesPaidCredits,
    long OutstandingPortFeeDebtCredits,
    long EventCostsCredits,
    long PassengerPayoutCredits,
    long PassengerPenaltyCredits,
    long? NetProfitCredits,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<VoyageCargoRemainderSnapshot>))]
    ImmutableArray<VoyageCargoRemainderSnapshot> UnsoldCargo = default);
