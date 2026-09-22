---
epic: EP-0001-trading-system
story: EP-0001-US-0016-market-knowledge
ticket: EP-0001-US-0016-TK-0002-authoritative-market-observations
title: Authoritative observations, stale и Save/Load
stage: approved
layer: engine
depends_on: [EP-0001-US-0016-TK-0001-market-knowledge-contract]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-05]
created: 2026-09-21T15:11:56Z
revision: 1
---

# Authoritative observations, stale и Save/Load

## Why

Удалённое знание должно быть последним наблюдением игрока, а не live-копией текущего рынка. Engine уже владеет docking, market profiles, inventory и save projection, но не хранит player market knowledge. Тикет добавляет authoritative lifecycle: initial observation известных станций, refresh при docking, stale по revision/availability, безопасную snapshot projection и атомарное Save/Load.

Внешние prerequisites: реализованные US-0004 (пять известных station ids) и US-0015 (station-level monotonic `MarketRevision`, инкрементируемая всеми изменениями рынка). Они указаны в story dependencies; frontmatter сохраняет только внутреннюю ticket dependency, потому что номера тикетов US-0015 ещё не созданы.

## Decisions

- 2026-09-21T15:11:56Z — пользователь: «создай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0016-market-knowledge\EP-0001-US-0016-market-knowledge.md».
- Других пользовательских решений для этого тикета нет.

## Assumptions

- A-01: US-0015 предоставляет `ulong SpaceObjectRuntime.MarketRevision` и сохраняет/восстанавливает её для Station. Если approved API отличается, вернуть тикет в review; не добавлять вторую revision.
- A-02: US-0002 предоставляет bounded runtime rows с `TargetStock`/authoritative band semantics. Observation включает только economy-managed rows; Fuel/legacy rows без target исключаются.
- A-03: На New Game первое observation создаётся для каждой `Station` с `IsKnown == true` после полной материализации US-0004. Неизвестные станции не добавляются.
- A-04: `IsAvailable` при observation равно `!station.IsDestroyed && !AccessDenied`; route risk/closure не включаются.
- A-05: Snapshot build может refresh только фактически docked station, затем проецирует все observations в `StationObjectId` order. Remote observations никогда автоматически не refresh по current revision.
- A-06: Stale вычисляется, но не сохраняется: `current MarketRevision != ObservedMarketRevision` или current availability отличается от observed. Role/profile change считается incompatible save/content change и не мигрируется молча.
- A-07: Save хранит observed role/availability/time/revision/bands. Timestamp использует calendar `GameTimeMs`; load не заменяет его текущим временем.
- A-08: Номер SaveFormatVersion выбирается после dependencies и увеличивается ровно один раз, только если их итоговая версия ещё не включает `marketKnowledge`; не использовать заранее предполагаемый номер.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.MarketKnowledge.cs` | Файл отсутствует; `SimulationEngine` уже partial (`SimulationEngine.cs:17`). | Создать всё хранение, validation, observation capture, stale projection и save mapping в одном partial-файле. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Load materializes stations/profile ids (`:196–340`), snapshot calls local trade projection (`:512–533`), save captures objects/GameStateData (`:760–828`), runtime station имеет `MarketProfileId` (`:3290–3299`). | Добавить узкие вызовы initialize/load, refresh/project, capture save и использовать US-0015 current revision; не менять quote/trade formula. |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs` | `CurrentSaveFormatVersion = 9` и `GameStateData` не имеет knowledge (`:6–26, 54–80`). | Добавить optional save DTOs/field и согласованный version bump после dependencies; строгие значения проверяет partial Engine code. |
| `tests/DeepSpaceSaga.Engine.Tests/MarketKnowledgeTests.cs` | Файл отсутствует; matching test project — `tests/DeepSpaceSaga.Engine.Tests`. | Добавить fixture-driven Engine tests для initial/docked/stale/privacy/save/load/invalid data. |

## Public API after the change

Нового публичного Engine API нет. `BuildSnapshot` заполняет Contracts field из TK-0001.

Additive save schema в `GameStateData`:

```csharp
[property: JsonPropertyName("marketKnowledge")]
IReadOnlyList<StationMarketKnowledgeData>? MarketKnowledge = null

public sealed record StationMarketKnowledgeData(
    [property: JsonPropertyName("stationObjectId")] string StationObjectId,
    [property: JsonPropertyName("stationRole")] string StationRole,
    [property: JsonPropertyName("isAvailable")] bool IsAvailable,
    [property: JsonPropertyName("observedAtGameTimeMs")] long ObservedAtGameTimeMs,
    [property: JsonPropertyName("observedMarketRevision")] ulong ObservedMarketRevision,
    [property: JsonPropertyName("stockBands")] IReadOnlyList<StationMarketStockBandData> StockBands);

public sealed record StationMarketStockBandData(
    [property: JsonPropertyName("itemTypeId")] string ItemTypeId,
    [property: JsonPropertyName("stockState")] StationMarketStockState StockState);
```

`IsStale` не входит в save DTO: это производная current-vs-observed величина.

## Implementation steps

1. В новом partial-файле хранить observations по ordinal station id; предоставить private helpers `InitializeOrLoadMarketKnowledge`, `ObserveStationMarket`, `BuildStationMarketKnowledgeProjection`, `CaptureMarketKnowledge` и validation без публичного API.
2. На New Game после materialization создать observation для каждой known Station: resolve profile `DisplayName`, availability, current revision, current calendar time и deterministic bands по `ItemTypeId`. Не включать unknown station или row без bounded target.
3. На load валидировать unique station ids, существование/known Station, профиль/role, timestamp `0..GameTimeMs`, unique known item ids и observed revision `<= current revision`. Ошибка бросает `ScenarioException` до замены `_objects`/текущего мира.
4. Перед snapshot projection определить фактически docked station. Если она известна, заменить только её observation текущими значениями и `GameTimeMs`; repeated snapshot на той же revision/availability/bands не меняет timestamp, чтобы сам snapshot tick не считался новым observation.
5. Для каждого stored observation вычислить `IsStale` из current revision/availability, не копируя current bands в старую запись. Отсортировать stations и bands ordinally; передать массив в `AuthoritativeSnapshot`.
6. CaptureSaveState записывает observations из stored values, не из current market. Согласовать один SaveFormat bump с уже реализованными dependencies и сохранить backward-compatible `null` handling только для New Game/поддерживаемых legacy saves по их migration policy.
7. Тестами доказать пять initial observations, отсутствие exact fields, stable timestamps, stale после каждого класса revision change из US-0015, docking refresh, undock privacy, save round-trip и atomic rejection invalid references.

## Out of scope

- Создание/инкремент `MarketRevision`, quote curve/QuoteId и сделки US-0015/US-0003.
- Генерация карты US-0004 или market flow US-0002.
- Scanner/intelligence command, платная разведка, route availability/risk.
- Client presentation и изменение TradeScreen.
- Сохранение history более чем одного observation на station.

## Invariants

- Engine — единственный источник market state; Client читает snapshots (`EngineRequirements.md:362–430`).
- Точный market projection остаётся dock-only: `SimulationEngine.cs:560–597`; Undock очищает docking state `:1883–1903`.
- Snapshot publication выполняется под world lock и возвращает immutable data (`SimulationEngine.cs:445–535`; `EngineRequirements.md:855–864`).
- Save строится из materialized runtime state, а load сначала валидирует кандидата (`SimulationEngine.cs:196–340, 760–830`).
- Calendar/economic timestamp не заменяется `MotionTimeMs`; clocks разделены (`SimulationEngine.cs:514, 533`; `ScenarioData.cs:54–80`).
- Remote DTO не получает exact quantities/prices/budget/quotes по API TK-0001.

## Tests

Named tests в `MarketKnowledgeTests`:

- `Known_five_station_map_gets_deterministic_initial_observations`
- `Unknown_station_gets_no_market_observation`
- `Unchanged_remote_market_keeps_observed_time_and_revision`
- `Production_event_and_trade_revision_changes_mark_remote_observation_stale`
- `Docking_refreshes_only_local_observation_and_exposes_exact_trade_projection`
- `Undock_hides_exact_trade_projection_but_keeps_coarse_observation`
- `Save_load_preserves_observed_values_and_recomputes_stale`
- `Invalid_duplicate_unknown_or_future_market_knowledge_rejects_load_atomically`

Команды:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketKnowledgeTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены в разрешённых файлах.
- Каждый пункт acceptance criteria, указанный в `serves`, покрыт изменением и named tests.
- Именованные тесты тикета проходят; указаны точные команды проверки.
- Build/lint соответствующего layer проходят либо конкретное исходное падение записано отдельно и не скрыто.
- Публичные API, invariants и out-of-scope ограничения соблюдены.
- Нет незаписанных assumptions, незакрытых блокирующих вопросов или скрытой работы вне `Code context`.
- Результат можно проверить по команде, тесту, diff evidence или наблюдаемому поведению.

## Self-containment check

Тикет задаёт acquisition, refresh, stale, availability, ordering, validation, persistence schema и точные тесты. Implementer работает только в двух существующих Engine/schema files, одном новом partial и одном matching test file; quote/map/UI решения не требуются.
