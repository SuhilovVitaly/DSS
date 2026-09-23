---
epic: EP-0001-trading-system
story: EP-0001-US-0007-temporary-market-events
ticket: EP-0001-US-0007-TK-0001-market-event-contract
title: Authoritative проекция активных событий
stage: approved
layer: contracts
depends_on: [EP-0001-US-0002-TK-0001-market-stock-snapshot]
files_touched: 2
serves: [AC-04, AC-05, AC-06]
created: 2026-09-21T11:08:58Z
revision: 1
---

# Authoritative проекция активных событий

## Why

Клиенту и будущей US-0008 нужен сериализуемый authoritative view активных событий без зависимости от Engine content types. Контракт сообщает identity, локализационные ключи, срок и bounded route-effect descriptor, но не раскрывает hidden budget и не предлагает Client пересчитывать market effects.

Matching test project: `tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`.

## Decisions

Решений пользователя сверх точного запроса от 2026-09-21T11:08:58Z не было. D-01: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0007-temporary-market-events\EP-0001-US-0007-temporary-market-events.md».

## Assumptions

- Active events принадлежат station-level `StationTradeSnapshot`, не отдельной строке товара.
- Contracts переносят localization keys; optional legacy display strings сохраняют старые scenario-authored события.
- Route effect здесь только DTO. Он не выбирает edge и не исполняет voyage.
- Default immutable arrays должны JSON-roundtrip как empty, как существующий `Items` contract.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs | :10–13 record содержит только StationObjectId и Items; :23–40 item projection | Добавить optional ActiveEvents и новые immutable event/route records/constants в этот же файл |
| tests/DeepSpaceSaga.Contracts.Tests/StationTradeSnapshotTests.cs | Existing matching serialization/equality coverage для StationTradeSnapshot | Добавить named JSON/default/route-descriptor tests; не создавать другой test file |

## Public API after the change

```csharp
public sealed record StationTradeSnapshot(
    string StationObjectId,
    ImmutableArray<StationInventoryItemSnapshot> Items = default,
    ImmutableArray<StationMarketEventSnapshot> ActiveEvents = default);

public sealed record StationMarketEventSnapshot(
    string EventId,
    string DefinitionId,
    string DisplayNameKey,
    string DescriptionKey,
    string EffectSummaryKey,
    long StartedGameTimeMs,
    long EndsGameTimeMs,
    long RemainingGameTimeMs,
    StationMarketRouteEffectSnapshot? RouteEffect = null,
    string? LegacyDisplayName = null,
    string? LegacyDescription = null);

public sealed record StationMarketRouteEffectSnapshot(
    string Availability,
    int MaxAffectedIncidentEdges,
    int TravelTimeMultiplierPermille,
    int FuelMultiplierPermille,
    string? RiskProfileId = null);

public static class StationRouteAvailabilityEffects
{
    public const string None = "None";
    public const string Restricted = "Restricted";
    public const string Unavailable = "Unavailable";
}
```

`EndsGameTimeMs` всегда строго больше `StartedGameTimeMs` для generated shipping event. `RemainingGameTimeMs` — authoritative `max(0, EndsGameTimeMs-snapshot.GameTimeMs)`; permanent legacy event проецируется с обоими значениями `long.MaxValue`. `MaxAffectedIncidentEdges` допустим 0/1 в этой story; `None` требует 0 и multipliers1000, blockade/quarantine используют 1. DTO не содержит probability, stock delta, hidden budget или price formula.

## Implementation steps

1. Расширить record только trailing optional полем, чтобы существующие positional callers с двумя аргументами продолжили компилироваться.
2. Применить `ImmutableArrayDefaultJsonConverter<StationMarketEventSnapshot>` к ActiveEvents, аналогично Items; default/empty сериализуются и десериализуются как пустой массив без null handling в Client.
3. Объявить event/route records и string constants в том же contracts file. Все числа — `long`/`int`; graphics/content/Engine types не пересекают boundary.
4. Зафиксировать JSON property roundtrip для 0, 1 и 2 событий, unicode legacy strings, end=`long.MaxValue` и обе route availability values.
5. Test не должен утверждать бизнес-валидацию malformed DTO: Contracts — transport, семантическую проверку выполняет Engine TK-0002/TK-0004.

## Out of scope

Engine projection/generation/save, content JSON, localization lookup, UI layout, edge selection, route validation и trade quote logic. Не добавлять event effects в `StationInventoryItemSnapshot` и не публиковать Credits/MarketBudgetCredits.

## Invariants

- Contract boundary остаётся JSON-serializable и не ссылается на Engine: Documentation/00-Process/CLAUDE.md:22–56.
- Station credits скрыты; snapshot даёт только безопасные derived значения: src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:15–21.
- Active event text приходит ключами; экономические значения authoritative и не рассчитываются клиентом: TradingSystemMvpStories.md:279–290.

## Tests

В `StationTradeSnapshotTests.cs` добавить:

- `Station_trade_snapshot_without_events_roundtrips_as_empty` — старый two-argument caller, default и explicit empty.
- `Station_market_events_roundtrip_identity_keys_and_absolute_window` — два events, ordinal order сохраняется, start/end/remaining exact.
- `Route_effect_roundtrips_without_hidden_market_state` — blockade `Unavailable/1`, quarantine `Restricted/1`, multipliers/risk exact; serialized JSON не содержит credits, probability или item multipliers.
- `Permanent_legacy_event_uses_max_end_and_fallback_text` — keys могут быть empty только для legacy projection, fallback strings сохраняются.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~StationTradeSnapshotTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Contracts\DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены в двух разрешённых файлах.
- AC-04/05/06 transport contribution покрыт named tests.
- Contract tests, matching build и format проходят либо точное исходное падение записано отдельно.
- Public API, invariants и out-of-scope соблюдены.
- Нет незаписанных assumptions или скрытых Engine/UI изменений.
- Результат проверяется JSON roundtrip и public signature.

## Self-containment check

Даны exact records, compatibility/default rules, допустимые route values и тесты. Implementer не должен искать формат события или решать, какие Engine details раскрывать. End state: snapshot может безопасно переносить 0–2 active events и route-effect descriptor.
