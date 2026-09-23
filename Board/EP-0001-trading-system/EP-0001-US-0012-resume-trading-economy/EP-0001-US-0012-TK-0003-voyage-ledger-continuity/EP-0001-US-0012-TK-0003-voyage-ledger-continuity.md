---
epic: EP-0001-trading-system
story: EP-0001-US-0012-resume-trading-economy
ticket: EP-0001-US-0012-TK-0003-voyage-ledger-continuity
title: Непрерывность рейса, топлива и ledger
stage: approved
layer: engine
depends_on: [EP-0001-US-0012-TK-0001-economy-save-schema, EP-0001-US-0009-voyage-fuel-cost, EP-0001-US-0010-cargo-cost-and-trade-receipts, EP-0001-US-0011-net-voyage-profit, EP-0001-US-0014-voyage-lifecycle]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04, AC-08]
created: 2026-09-21T12:53:36Z
revision: 1
---

# Непрерывность рейса, топлива и ledger

## Why

Active voyage, изъятый из баков fuel reservation и открытый ledger должны восстанавливаться как одна транзакционная единица. Если хотя бы один fragment потерян или terminal callback повторён после load, игрок получает второй расход/возврат топлива, второй доход/fee либо фиктивную прибыль. Тикет добавляет voyage-side staging/capture и durable terminal idempotency.

## Decisions

Пользовательских решений кроме исходного запроса «сделай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0012-resume-trading-economy\EP-0001-US-0012-resume-trading-economy.md» не было.

## Assumptions

- A-01: US-0014 предоставляет persisted `ActiveVoyageData` со stable `VoyageId`, origin/destination, state/progress, captured route terms и exactly-once terminal seam.
- A-02: US-0009 хранит reservation parts, fuel basis и last settlement; US-0010 хранит cargo basis и realized result; US-0011 хранит active/closed voyage ledgers и stable entry IDs.
- A-03: один active voyage соответствует не более чем одному active ledger; его reservation parts суммируются в persisted reserved kg/basis. Closed voyage не может одновременно быть active.
- A-04: terminal idempotency не зависит только от bounded command journal. Durable terminal receipt IDs/ledger entry IDs переживают eviction обычного command receipt и load.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.VoyageContinuation.cs | Новый file; current checkout не содержит active voyage/fuel reservation/ledger (`SimulationEngine.cs:52–91,196–359`) | Stage/capture/validate voyage, reservation, settlement, ledger и durable terminal receipt identity как согласованный aggregate |
| src/DeepSpaceSaga.Engine/SimulationEngine.CommandJournal.cs | Текущий command journal bounded4096, восстанавливает receipts/pending (`:8–53`) | Подключить persisted durable terminal ids к duplicate/no-op checks без изменения общего retention4096 |
| tests/DeepSpaceSaga.Engine.Tests/VoyageSaveLoadContinuityTests.cs | Новый file; текущий save continuity patterns в `SaveLoadContinuityTests.cs:22–163`, planned fuel contract в US-0009 TK-0004 | Checkpoints before/after reserve, progress, arrival/interruption, fee/trade ledger entry, replay and invalid aggregate |

## Public API after the change

No public API change. Internal partial seam:

```csharp
private sealed record StagedVoyageContinuation(
    ActiveVoyageData? ActiveVoyage,
    VoyageLedgerData? ActiveLedger,
    ImmutableArray<VoyageLedgerData> ClosedLedgers,
    ImmutableHashSet<string> DurableTerminalReceiptIds);

private StagedVoyageContinuation StageVoyageContinuation(
    ScenarioFile source,
    IReadOnlyList<SpaceObjectRuntime> stagedObjects);

private void CaptureVoyageContinuation(
    ref GameStateData state,
    ref TradingEconomyContinuationData manifest);

private void CommitVoyageContinuation(StagedVoyageContinuation staged);
```

Exact dependency record namespaces/names may differ, но required fields фиксированы: stable voyage/ledger/entry ids; origin/destination; state/progress/arrival; reservation tank/module parts with kg+basis; last settlement; active/closed ledger entries and totals. Несовпадение возвращает ticket в review.

## Implementation steps

1. Preflight merged US-0014/0009/0010/0011 APIs. Сопоставить один persisted field каждому required fact; не создавать второй voyage, reservation или ledger DTO.
2. Stage полностью до commit: validate voyage station refs/map edge, state/progress 0..1000, captured route terms, reservation module refs, kg/basis conservation, settlement terminal state, ledger voyage binding, entry stable IDs, chronological/stable ordering и checked totals.
3. Проверить atomic combinations: no active voyage → no reservation and no active voyage ledger; active voyage → exactly one matching active ledger; arrived/interrupted → no active reservation, exactly one settlement and closed ledger; closed IDs distinct from active.
4. Capture under world lock serializes tank state plus reservation parts exactly once. Conservation: `tank kg+basis + reserved kg+basis` равны authoritative pre-reservation totals с учётом persisted settlement; cargo COGS и route fuel cost остаются разными lines.
5. Сохранить active/closed ledgers и durable terminal receipt IDs in stable order. Ledger totals выводятся из persisted entries only для validation; load не добавляет balancing entry и не повторяет port/event/passenger/trade posting.
6. Commit вызывается TK-0004 после полного world validation. Restore active voyage/ledger/terminal ids before any tick or command can run.
7. Duplicate `CommandId` продолжает возвращать original `CommandResult`; duplicate terminal callback по VoyageId/terminal receipt делает no-op и возвращает persisted settlement даже после ordinary receipt eviction.
8. Tests на checkpoints: before reserve; immediately after reserve; mid-progress; just before/after arrival; after interrupted refund; before/after partial sale and port fee. На каждом проверить immediate equality и exactly-once next action.

## Out of scope

- Реализация Undock/lifecycle, fuel formula, cost basis, ledger calculation или Finance UI.
- Market intervals/events/quotes — TK-0002.
- Root atomic load and split-run matrix — TK-0004.
- Увеличение `CommandReceiptLimit`, unlimited audit log или изменение retention policy.
- Новые Contracts DTO и content data вне prerequisite APIs.

## Invariants

- Save обязан хранить pending/runtime/counter state, чтобы load не создавал повторные IDs/effects: `Documentation/01-Requirements/EngineRequirements.md:236–246,3610,4732–4734`.
- Current command receipts и pending commands уже захватываются/восстанавливаются: `SimulationEngine.cs:823–828,353–358`; `SimulationEngine.CommandJournal.cs:32–53`.
- Fuel — module state, cargo — отдельный stack; current schema хранит их раздельно: `ScenarioData.cs:291–302,416–424`.
- Финансовый итог рейса не должен дважды учитывать fuel/fee: `Board/EP-0001-trading-system/Documentation.md:97,102–103`.
- Invalid staged save не меняет ранее загруженный мир; current load уже строит runtime before lock: `SimulationEngine.cs:232–317`.

## Tests

Matching test project: `tests/DeepSpaceSaga.Engine.Tests`.

Named tests:

- `Save_load_after_reservation_preserves_tank_and_escrow_conservation`
- `Mid_voyage_save_load_preserves_progress_route_terms_and_active_ledger`
- `Arrival_after_load_settles_fuel_and_ledger_exactly_once`
- `Interrupted_voyage_after_load_refunds_unused_fuel_once`
- `Duplicate_terminal_callback_replays_settlement_after_command_receipt_eviction`
- `Invalid_voyage_reservation_ledger_combination_is_rejected_before_commit`

Commands:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~VoyageSaveLoadContinuityTests"
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

Тикет задаёт три файла, exact aggregate invariants, staging/capture/commit seam, terminal dedupe policy, conservation equations и checkpoint matrix. Реализация не ищет дополнительные persistence locations: если prerequisite не предоставляет перечисленные facts, это зафиксированный dependency mismatch, а не разрешение расширить scope.
