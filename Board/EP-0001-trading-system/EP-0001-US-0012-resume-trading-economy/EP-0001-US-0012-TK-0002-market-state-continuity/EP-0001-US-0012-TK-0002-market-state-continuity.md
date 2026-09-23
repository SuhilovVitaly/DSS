---
epic: EP-0001-trading-system
story: EP-0001-US-0012-resume-trading-economy
ticket: EP-0001-US-0012-TK-0002-market-state-continuity
title: Непрерывность рынка и событий
stage: approved
layer: engine
depends_on: [EP-0001-US-0012-TK-0001-economy-save-schema, EP-0001-US-0002-market-replenishment, EP-0001-US-0003-dynamic-market-trading, EP-0001-US-0005-station-resource-fields, EP-0001-US-0008-route-risk-and-alternatives, EP-0001-US-0015-authoritative-market-quotes]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04, AC-07]
created: 2026-09-21T12:53:36Z
revision: 1
---

# Непрерывность рынка и событий

## Why

Даже если dependency DTO сериализуются по отдельности, рынок продолжится неверно при потерянном interval cursor, market revision allocator, event sequence или при повторном bootstrap materialized map/fields. Тикет создаёт один market-side staging/capture adapter и доказывает отсутствие скачка или повторного эффекта на границах интервала, сделки и события.

## Decisions

Пользовательских решений кроме исходного запроса «сделай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0012-resume-trading-economy\EP-0001-US-0012-resume-trading-economy.md» не было.

## Assumptions

- A-01: prerequisites уже сохраняют materialized `TradingMapStateData`, resource-field state, station stock/`MarketBudgetCredits`/pending output, station `MarketRevision`, resolved active events и их catalog fingerprint.
- A-02: US-0015 предоставляет monotonic market revision allocator и issued-quote cache. После load allocator продолжается выше всех persisted revisions, а cache очищается; старый quote id не исполняется.
- A-03: event generator использует `masterSeed` и persisted sequence/cursor; load не делает новый RNG draw до следующей legitimate boundary.
- A-04: market knowledge здесь означает только persisted prerequisite fields/timestamps. Future remote knowledge US-0016 не добавляется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.MarketContinuation.cs | Новый file; текущий capture/load централизован в `SimulationEngine.cs:196–359,760–833`, общего market staging adapter нет | Stage/capture/validate market map, fields, station economy, revisions, events, knowledge and cursors без shadow state |
| src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs | Текущий ordered `(previous,target]` loop и cursor в `:8–51`; dependency US-0002 добавляет hour boundary, US-0007 event ordering | Подключить staged market cursor/next boundary так, чтобы loaded boundary не выполнялась повторно; сохранить motion mapping |
| tests/DeepSpaceSaga.Engine.Tests/MarketSaveLoadContinuityTests.cs | Новый file; базовые cursor round-trips находятся в `EconomyTimeContinuityTests.cs:21–93`, station state — `StationEconomyBatch2Tests.cs:18–30,199–216` | Boundary matrix, transaction/event/revision/map/knowledge continuity и stale quote regressions |

## Public API after the change

No public API change. Internal partial seam:

```csharp
private sealed record StagedMarketContinuation(
    long LastProcessedMarketGameTimeMs,
    long NextMarketRevision,
    long NextMarketEventSequence);

private StagedMarketContinuation StageMarketContinuation(
    ScenarioFile source,
    IReadOnlyList<SpaceObjectRuntime> stagedObjects);

private TradingEconomyContinuationData CaptureMarketContinuation(
    TradingEconomyContinuationData manifest);

private void CommitMarketContinuation(StagedMarketContinuation staged);
```

`Stage` выполняет только validation/conversion, не меняет engine. Map/fields/stocks/events остаются в dependency-owned records/runtime objects; adapter отвечает за cross-cutting cursors, consistency и ephemeral quote invalidation.

## Implementation steps

1. Preflight merged dependencies: подтвердить materialized map/resource field fields, station stock/budget/pending, market revision, active event resolved state, event/config fingerprints и quote allocator/cache. Если exact fields нельзя сопоставить один-к-одному, вернуть тикет в review; не добавлять дубликаты.
2. Реализовать `StageMarketContinuation`: проверить station ids against materialized map, resource-field owner/composition refs, stock bounds/target/max, budget bounds, pending output, event stable ids/start/end/applied markers, route modifiers, market revisions и all cross references. Collections normalize in stable ordinal/domain order.
3. Для current save требовать equality manifest cursor ↔ owning subsystem cursor. `NextMarketRevision` должен быть строго больше каждого persisted station/receipt revision; `NextMarketEventSequence` больше sequence active/expired durable events. Overflow отклонять.
4. Capture записывает фактический processed market cursor и next allocators; не вызывает price calculator, event roll, map generator или knowledge refresh. `masterSeed`, materialized map/fields и station/event state берутся из dependency capture path.
5. Commit вызывается TK-0004 только после полного staged world validation. Установить cursors/allocators и очистить issued quote cache. Durable executed receipts сохраняются; команда со старым non-receipted QuoteId получает stale/unknown quote без mutation.
6. Economy loop использует loaded `LastProcessedMarketGameTimeMs`; boundary exactly at saved time уже обработана. Следующая граница обрабатывается один раз с прежним ordering и тем же mapping calendar→motion.
7. Tests строят checkpoints `hour-1`, `hour`, `hour+1`, до/после quote execution и event start/end. Сравнить normalized state immediately after load и после следующей одинаковой command/time sequence.

## Out of scope

- Voyage/fuel/ledger — TK-0003.
- Root schema migration/fingerprint composition — TK-0001/TK-0004.
- Quote formula, event generation rules, map generation, stock flow или remote knowledge feature.
- Client cache persistence и UI changes.
- Любые изменения dependency content JSON.

## Invariants

- Экономическая граница принадлежит `(previous,target]` и повторный timestamp не повторяет эффект: `SimulationEngine.EconomyTime.cs:21–49`.
- Resource access и command/event ordering stable, не dictionary/thread order: `Documentation/01-Requirements/EngineRequirements.md:3990–4006,4063–4068`.
- `masterSeed` сохраняется и продолжает deterministic RNG streams: `EngineRequirements.md:314–348`.
- Ephemeral quote не является durable financial receipt; US-0003 AC-04 требует durable duplicate result, а US-0015 выдаёт свежую quote после revision change.
- Market state остаётся authoritative Engine state; Client не рассчитывает цену или event effect: `Documentation/00-Process/CLAUDE.md:22–56`.

## Tests

Matching test project: `tests/DeepSpaceSaga.Engine.Tests`.

Named tests:

- `Save_load_at_market_boundary_does_not_apply_interval_twice`
- `Save_load_preserves_map_fields_stock_budget_and_prices`
- `Event_start_and_expiry_match_continuous_run_after_load`
- `Market_revision_and_allocators_continue_monotonically`
- `Issued_quote_is_invalidated_but_executed_receipt_replays_after_load`
- `Invalid_cross_reference_is_rejected_before_market_commit`

Commands:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketSaveLoadContinuityTests"
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

Тикет ограничен одним новым partial, существующим scheduler и одним test file. Он задаёт staging contract, validation/cursor rules, quote-cache policy, exact checkpoints и return-to-review condition для несовпавших prerequisite signatures; дополнительных продуктовых решений не требуется.
