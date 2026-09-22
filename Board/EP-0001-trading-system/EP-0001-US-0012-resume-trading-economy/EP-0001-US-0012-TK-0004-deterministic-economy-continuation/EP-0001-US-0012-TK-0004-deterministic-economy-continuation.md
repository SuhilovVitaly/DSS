---
epic: EP-0001-trading-system
story: EP-0001-US-0012-resume-trading-economy
ticket: EP-0001-US-0012-TK-0004-deterministic-economy-continuation
title: Эквивалентность непрерывной и загруженной игры
stage: approved
layer: engine
depends_on: [EP-0001-US-0012-TK-0002-market-state-continuity, EP-0001-US-0012-TK-0003-voyage-ledger-continuity]
files_touched: 3
serves: [AC-02, AC-03, AC-04, AC-07, AC-08]
created: 2026-09-21T12:53:36Z
revision: 1
---

# Эквивалентность непрерывной и загруженной игры

## Why

Market-side и voyage-side fragments должны загружаться одной atomic transaction и затем давать тот же дальнейший authoritative результат, что непрерывная игра. Тикет подключает оба staged adapters к единственному `SimulationEngine.LoadScenario/CaptureSaveState` path, проверяет combined configuration fingerprint и закрепляет split-run equivalence по ключевым торговым checkpoints.

## Decisions

Пользовательских решений кроме исходного запроса «сделай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0012-resume-trading-economy\EP-0001-US-0012-resume-trading-economy.md» не было.

## Assumptions

- A-01: TK-0002/TK-0003 возвращают полностью validated staged objects без engine mutation и имеют capture/commit seams из своих Public API sections.
- A-02: combined fingerprint строится только из immutable registry/config identities, доступных merged `GameDataRegistry` и materialized map rules; один canonical builder используется для save и load comparison.
- A-03: normalized comparison исключает `ScenarioMetadata.Name/ScenarioId`, transient issued quotes, client screen state и snapshot delivery sequence; все authoritative balances, ids, cursors and receipts входят.
- A-04: loaded `CurrentSpeed`, GameTimeMs и MotionTimeMs сохраняются по существующим правилам; история не меняет calendar/motion separation.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | Load строит runtime до lock и коммитит в `:196–359`; save формируется единым `GameStateData` в `:760–833` | Stage оба continuation adapters/fingerprint до lock, commit внутри существующей atomic секции; capture manifest в единственном save path |
| src/DeepSpaceSaga.Engine/SimulationEngine.TradingContinuation.cs | Новый file; combined fingerprint/coordinator отсутствует | Canonical fingerprint builder, root stage/capture/commit orchestration и normalized test projection helper internal-only |
| tests/DeepSpaceSaga.Engine.Tests/TradingEconomyContinuityTests.cs | Новый file; базовые split-run patterns в `SaveLoadContinuityTests.cs:22–163` и `EconomyTimeContinuityTests.cs:21–93` | Table-driven continuous-vs-reloaded scenarios across interval/trade/event/voyage/fuel/ledger/replay boundaries |

## Public API after the change

No public API change. Internal root seam:

```csharp
private sealed record StagedTradingContinuation(
    StagedMarketContinuation Market,
    StagedVoyageContinuation Voyage);

private StagedTradingContinuation StageTradingContinuation(
    ScenarioFile source,
    IReadOnlyList<SpaceObjectRuntime> stagedObjects);

private string BuildTradingConfigurationFingerprint(TradingMapStateData? map);
private TradingEconomyContinuationData CaptureTradingContinuation();
private void CommitTradingContinuation(StagedTradingContinuation staged);
```

`BuildTradingConfigurationFingerprint` canonicalizes semantic version/fingerprint strings with explicit field labels and ordinal UTF-8, then returns uppercase SHA-256. It never hashes mutable state or raw JSON file order.

## Implementation steps

1. В `LoadScenario`, после base schema normalization, catalog check и full runtime object construction, вычислить expected combined fingerprint from current immutable registry + loaded materialized map rules. Сравнить ordinal с manifest до `_worldStateLock`.
2. Вызвать market и voyage `Stage...` before commit. Ни один adapter не имеет права менять `_objects`, clocks, journals, RNG/cursors, caches или ledgers. Любая exception оставляет current session byte/structurally unchanged по повторному `CaptureSaveState`.
3. В существующем lock block сохранить текущий общий commit order: identity/clock → objects → economy/market → voyage/fuel/ledger → command and terminal journals. До выхода из lock Engine должен быть полностью runnable; half-restored state не наблюдается.
4. В `CaptureSaveState` под тем же world lock получить dependency-owned DTO и manifest cursors/fingerprint. Не вызывать generator, event roll, quote calculator, settlement или ledger posting.
5. Добавить normalized authoritative projection helper только `internal`/test-visible: сортировка stable-id collections, metadata normalization, no transient fields. Production persistence продолжает использовать обычный ScenarioData JSON.
6. Построить table-driven split-run tests. Для каждого checkpoint run A идёт непрерывно; run B выполняет `CaptureSaveState → Serialize → LoadFromJson(true) → new engine.LoadScenario(isSave:true)`. Затем оба получают одинаковые commands/time advances.
7. Checkpoints: before/at/after market hour; before/after successful and partial trade; before/after event activation/expiry; after Undock reservation; mid-voyage; before/after arrival/interruption; after port/event/passenger/COGS entries; duplicate command and terminal callback.
8. На каждом checkpoint сравнить immediate normalized state and next results: price curves/revisions, route availability, command receipts, stock/budgets, player/station money, events, active voyage/progress, tank+reservation, settlement, active/closed ledger totals and entries.
9. Negative tests tamper catalog/profile/event/map fingerprint, manifest cursor, reservation/ledger binding. Assert exact diagnostic contains subsystem identity and `Save was not modified`; preloaded sentinel world remains unchanged.

## Out of scope

- Новый public contract, UI, content, balance simulation или persistence subsystem.
- Byte-equality transient metadata/render snapshots.
- Исправление dependency mechanics, если split-run выявил их самостоятельный bug: такой failure возвращается владельцу dependency, не расширяет этот ticket.
- LocalClient disk path — TK-0005.

## Invariants

- Runtime object construction and semantic checks precede mutation: `SimulationEngine.cs:196–233,319–359`.
- Save capture is one authoritative `GameStateData`, not parallel subsystem files: `SimulationEngine.cs:760–833`.
- Same time, ids, RNG and runtime state after load must reproduce future authoritative results: `Documentation/01-Requirements/EngineRequirements.md:4070–4074`.
- Stable ordering must not depend on dictionary, reflection or thread scheduling: `EngineRequirements.md:3990–4006`.
- Motion time and calendar time remain separate; load does not accelerate physical movement: `SimulationEngine.EconomyTime.cs:12–20`; story invariant.

## Tests

Matching test project: `tests/DeepSpaceSaga.Engine.Tests`.

Named tests:

- `Continuous_and_reloaded_market_sequences_are_equivalent`
- `Continuous_and_reloaded_event_sequences_are_equivalent`
- `Continuous_and_reloaded_voyage_finances_are_equivalent`
- `Immediate_loaded_state_has_no_price_stock_money_or_progress_jump`
- `Duplicate_command_and_terminal_effects_remain_exactly_once_after_load`
- `Incompatible_fingerprint_rejects_without_modifying_loaded_world`

Commands:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~TradingEconomyContinuityTests"
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~SaveLoadContinuityTests|FullyQualifiedName~EconomyTimeContinuityTests|FullyQualifiedName~CatalogCompatibilityTests"
dotnet build D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --no-restore
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

Тикет ограничен root load/save file, одним coordinator partial и одним table-driven test file. Order, fingerprint input, comparison normalization, checkpoint matrix, negative cases и ownership failures заданы явно; implementer не должен выбирать новую механику или искать дополнительный state store.
