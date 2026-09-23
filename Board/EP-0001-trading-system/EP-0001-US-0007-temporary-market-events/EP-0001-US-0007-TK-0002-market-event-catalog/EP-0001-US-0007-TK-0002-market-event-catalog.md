---
epic: EP-0001-trading-system
story: EP-0001-US-0007-temporary-market-events
ticket: EP-0001-US-0007-TK-0002-market-event-catalog
title: Схема каталога и save-состояния событий
stage: approved
layer: engine
depends_on: [EP-0001-US-0002-TK-0002-market-economy-schema, EP-0001-US-0007-TK-0001-market-event-contract]
files_touched: 5
serves: [AC-01, AC-04, AC-06]
created: 2026-09-21T11:08:58Z
revision: 1
---

# Схема каталога и save-состояния событий

## Why

Lifecycle нельзя строить на свободной scenario-JSON структуре: нужны strict definitions, cross-item/profile validation, canonical fingerprint и полная resolved save shape. Ticket создаёт data contract и compatibility guard, но не активирует события.

Matching test project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`.

## Decisions

D-01, 2026-09-21T11:08:58Z: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0007-temporary-market-events\EP-0001-US-0007-temporary-market-events.md». Других пользовательских решений не было.

## Assumptions

- Catalog schemaVersion=1; runtime Settings load требует exact set из восьми canonical IDs.
- SaveFormat становится 10 поверх planned US-0002 version9. Optional fields сохраняют чтение legacy сценариев/сохранений в пределах уже поддерживаемой политики.
- Catalog effects item-specific; engine не выводит затронутые товары из display text или category heuristics.
- Activation chance и duration заданы целыми hourly/fixed-point значениями; никакого wall-clock/float.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Content/StationMarketEventDefinition.cs | Новый файл | Internal immutable definition/effect/route records и canonical ID set |
| src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs | :11–63 registry properties/compatibility; :73–110 Create и item/profile validation | Добавить StationMarketEvents registry, semantic validation и event fingerprint |
| src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs | :8–13 strict JSON; :73–129 Settings/type-data assembly; :132–180 profile loader pattern | `typeData.stationMarketEvents`, strict schema loader/DTO mapping, exact-eight shipping gate |
| src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs | :7–24 SaveFormat8 в текущем checkout; US-0002 планирует9; :52–73 GameStateData; :245–289 legacy event shape | Save10, catalog fingerprint, resolved generated-event fields без удаления legacy fields |
| tests/DeepSpaceSaga.Engine.Tests/StationMarketEventCatalogTests.cs | Новый test file | Strict schema, validation, fingerprint и DTO roundtrip tests |

## Public API after the change

Internal content API in `StationMarketEventDefinition.cs`:

```csharp
internal sealed record StationMarketEventDefinition(
    string TypeId,
    string DisplayNameKey,
    string DescriptionKey,
    string EffectSummaryKey,
    int Priority,
    int ChancePermillePerHour,
    int MinDurationHours,
    int MaxDurationHours,
    ImmutableArray<string> EligibleMarketProfileIds,
    ImmutableArray<StationMarketEventItemEffectDefinition> ItemEffects,
    StationMarketEventRouteEffectDefinition? RouteEffect = null);

internal sealed record StationMarketEventItemEffectDefinition(
    string ItemTypeId,
    int ProductionMultiplierPermille,
    int DemandMultiplierPermille,
    int PriceMultiplierPermille,
    long ActivationStockDelta = 0);

internal sealed record StationMarketEventRouteEffectDefinition(
    string Availability,
    int MaxAffectedIncidentEdges,
    int TravelTimeMultiplierPermille,
    int FuelMultiplierPermille,
    string? RiskProfileId = null);

internal static class StationMarketEventIds
{
    // event.reactor-accident, event.decompression, event.hydroponics-failure,
    // event.pirate-blockade, event.cargo-convoy, event.quarantine,
    // event.repair-boom, event.scientific-contract
    public static ImmutableArray<string> All { get; }
}
```

`GameDataRegistry` получает `TypeRegistry<StationMarketEventDefinition> StationMarketEvents` и uppercase SHA-256 `StationMarketEventCatalogFingerprint` от canonical ordinal JSON payload всех semantic fields. Create получает trailing optional `IEnumerable<StationMarketEventDefinition>? stationMarketEvents=null` и internal `bool requireCompleteMarketEventSet=false`; Settings loader всегда передаёт true.

`EngineSettingsFile.TypeData` получает optional string `StationMarketEvents`. Declared path обязателен и nonempty; absent path означает legacy/no generated events.

Scenario/save extensions:

```csharp
GameStateData(...,
    [JsonPropertyName("marketEventCatalogFingerprint")]
    string? MarketEventCatalogFingerprint = null)

StationEventData(...,
    [JsonPropertyName("definitionId")] string? DefinitionId = null,
    [JsonPropertyName("displayNameKey")] string? DisplayNameKey = null,
    [JsonPropertyName("descriptionKey")] string? DescriptionKey = null,
    [JsonPropertyName("effectSummaryKey")] string? EffectSummaryKey = null,
    [JsonPropertyName("itemEffects")]
    IReadOnlyList<StationMarketEventItemEffectData>? ItemEffects = null,
    [JsonPropertyName("routeEffect")]
    StationMarketEventRouteEffectData? RouteEffect = null,
    [JsonPropertyName("activationStockDeltaApplied")]
    bool ActivationStockDeltaApplied = false)
```

`StationMarketEventItemEffectData` mirrors the four item fields; `StationMarketEventRouteEffectData` mirrors route definition. Existing `DisplayName`, `Description`, `PriceFactors`, start and duration remain present for backward compatibility. Generated event requires non-null DefinitionId/keys/finite positive DurationMs/resolved effects and `ActivationStockDeltaApplied`; legacy event has DefinitionId null and follows existing rules.

## Implementation steps

1. Создать immutable definition types и canonical ID constants exactly as listed. Не добавлять behavior/RNG в content model.
2. EngineContentLoader читает `{ "schemaVersion":1, "events":[...] }` strict JSON. Missing declared file, unknown field, null array/member, duplicate typeId/item effect/profile, wrong primitive or unsupported version → contextual ContentException.
3. Validate: exact eight IDs for Settings; priority 0..1000; chance 0..1000; 1<=min<=max<=168 hours; eligible profiles nonempty and registered; item effects nonempty, registered Cargo item, non-Fuel, one per item; each multiplier 0..4000; at least one multiplier !=1000 or delta!=0 or route effect non-null.
4. Checked activation delta допускает диапазон `-1_000_000..1_000_000`; definition with delta requires that item be present in every eligible profile economy target. Production multiplier отличается от1000 только если item является Supply хотя бы одного eligible profile; demand — если item является Demand хотя бы одного. Для конкретного profile без соответствующего flow этот component является no-op. Price может применяться к любому target item.
5. Route validation: only pirate-blockade/quarantine may have non-null route; availability Restricted/Unavailable; max edges exactly1; multipliers 500..4000; non-route definitions null. Route event must also have at least one market item effect, so US-0007 remains observable before US-0008.
6. Validate localization keys nonempty and unique by semantic role/type. Content tests in TK-0003 prove keys exist in RU/EN; Engine does not open locale files.
7. Build canonical fingerprint from sorted definitions/profiles/item effects and all numeric/string fields, not JSON declaration order. Reordering definitions/effects/profiles leaves hash; any semantic value changes it.
8. Extend ScenarioData additively. Bump version expectation from prerequisite9 to10; do not change catalogVersion, `CatalogCompatibility` item hash or market profile fingerprint.
9. Serialize generated event resolved effects plus definition ID/keys. Legacy record construction remains source-compatible via trailing optional parameters. DTO layer itself does not activate, expire or validate current time.
10. Tests build minimal item/profile registries using exact references. `requireCompleteMarketEventSet=false` is used only for focused unit fixtures; Settings loader test proves the complete gate cannot be bypassed.

## Out of scope

Scheduler, runtime resolution/projection, stock/price mutation, market revision, Save load enforcement, UI, localization files, route edge selection и tuning evidence. Не менять US-0002 profile schema or quote API.

## Invariants

- Unknown content is rejected before world startup: EngineContentLoader.cs:8–13,76–85.
- Money/factors avoid float/double and use fixed-point: EngineRequirements.md:5263–5265.
- Price event needs stable id, player-facing text, factors, duration and deterministic ordering: EngineRequirements.md:5288–5299.
- Existing legacy event fields remain readable: ScenarioData.cs:245–289; StationEconomyBatch2Tests.cs:387–570.

## Tests

В новом `StationMarketEventCatalogTests.cs`:

- `Shipping_catalog_requires_exact_eight_canonical_event_ids` (AC-01).
- `Event_catalog_rejects_unknown_profile_item_duplicate_and_invalid_ranges` (AC-01): отдельные cases для priority/chance/duration/multipliers/delta/Fuel/source-demand mismatch.
- `Only_blockade_and_quarantine_accept_one_edge_route_effect` (AC-04).
- `Event_fingerprint_is_order_independent_and_covers_every_effect` (AC-06): fixed expected hash не вычислять вторым вызовом production method.
- `Generated_event_save_shape_roundtrips_resolved_effects_and_applied_marker` (AC-06).
- `Legacy_price_event_shape_remains_readable_without_definition` (AC-06).
- `Save_version_ten_adds_event_fingerprint_without_changing_catalog_identity` (AC-06).
- `Declared_event_file_is_mandatory_and_strict` (AC-01): missing, unknown field, version, incomplete set.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~StationMarketEventCatalogTests|FullyQualifiedName~StationEconomyBatch2Tests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены в пяти разрешённых файлах.
- AC-01/04/06 schema contribution покрыт named tests.
- Engine tests/build/format проходят либо baseline failure записан точно.
- API, compatibility, invariants и out-of-scope соблюдены.
- Нет скрытого scheduler/UI/route implementation.
- Result проверяется strict loader, fingerprint fixtures и DTO roundtrip.

## Self-containment check

Ticket содержит exact types, ranges, canonical IDs, save extensions, version/fingerprint policy и exhaustive validation. Implementer не должен выбирать schema или искать требования. End state: валидный eight-event catalog загружается однозначно, malformed content отклоняется, generated event имеет устойчивую save форму.
