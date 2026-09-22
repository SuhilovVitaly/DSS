---
epic: EP-0001-trading-system
story: EP-0001-US-0011-net-voyage-profit
ticket: EP-0001-US-0011-TK-0002-voyage-ledger-lifecycle
title: Жизненный цикл и расходы рейсного ledger
stage: approved
layer: engine
depends_on: [EP-0001-US-0011-TK-0001-voyage-finance-contract, EP-0001-US-0006-repeatable-trading-voyage, EP-0001-US-0009-TK-0004-voyage-fuel-settlement, EP-0001-US-0010-TK-0002-persisted-cargo-cost-basis, EP-0001-US-0014-voyage-lifecycle]
files_touched: 5
serves: [AC-01, AC-02, AC-04]
created: 2026-09-21T12:56:24Z
revision: 1
---

# Жизненный цикл и расходы рейсного ledger

## Why

Рейсному отчёту нужен authoritative lifecycle: открыть запись после принятого Undock, зафиксировать увозимый cargo, один раз принять fuel settlement и портовые начисления, завершить transport phase и заморозить предыдущую запись перед следующим рейсом. Это устраняет двойной fuel/fee и не выводит прибыль из общего изменения Credits.

## Decisions

Пользователь не задавал дополнительных технических решений. Применяются story A-01 (transport close + realization phase), A-04 (assessed fee уменьшает прибыль), A-05 (только consumed fuel settlement) и A-08 (retention 50).

## Assumptions

US-0014 создаёт `SimulationEngine.Voyages.cs` и stable `ActiveVoyageData`; US-0009 settlement выполняется до удаления active voyage; US-0010 добавляет known/unknown total basis к cargo stack. TK-0002 начинается только после проверки этих signatures. Первый dock без входящего voyage не создаёт финансовый отчёт.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.VoyageLedger.cs` | Новый partial; поиск `Voyage/Ledger/NetProfit` в current `src` не нашёл модели | Runtime ledger state, posting IDs, opening cargo capture, state transitions и bounded retention |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Voyages.cs` | Планируемый owner US-0014; сейчас Undock/voyage API отсутствует (`NavigationComputerCommandTypes.cs:3–14`, `StationScreen.cs:83–96`) | Begin/finalize hooks вокруг accepted Undock и terminal arrival/interruption; принять уже рассчитанный fuel settlement |
| `src/DeepSpaceSaga.Engine/SimulationEngine.PortFees.cs` | `RenewPortFees` знает assessed fee, paid и новый debt (`:29–48`) | После успешного atomic fee commit записать один posting с stable due-time key |
| `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs` | Choice commit заменяет objects/Credits (`:165–174`); docking fee result теряет финансовую категорию | До/после commit вычислить initial docking assessed/paid/debt delta и post в прибывший ledger |
| `tests/DeepSpaceSaga.Engine.Tests/VoyageLedgerLifecycleTests.cs` | Новый файл; текущие отдельные seams — `PortFeeScheduleTests.cs:21–81`, dependency voyage/fuel tests | Start/terminal/fuel/fee/debt/retry/retention and no-ledger-first-dock regressions |

## Public API after the change

Нового public API нет; используется contract TK-0001. В `SimulationEngine.VoyageLedger.cs` единственный internal owner предоставляет:

```csharp
private void BeginVoyageLedger(ActiveVoyageData voyage, long gameTimeMs);
private void CompleteVoyageTransport(string voyageId, string? destinationStationObjectId,
    long gameTimeMs, bool interrupted, VoyageFuelSettlementSnapshot? fuelSettlement);
private void RecordVoyagePortFee(string postingId, long assessedCredits,
    long paidCredits, long debtAddedCredits);
private void RecordVoyageAmount(string postingId, VoyageAmountKind kind, long credits);
private void FinalizeAwaitingVoyageBeforeUndock();
```

`VoyageAmountKind` — private/internal enum `EventCost`, `PassengerPayout`, `PassengerPenalty`; текущий тикет не создаёт producers. Posting принимается только при positive amount и уникальном non-empty `postingId`; duplicate — no-op, invalid/overflow — отказ до mutation.

## Implementation steps

1. Перед commit нового accepted Undock сначала finalized предыдущий `awaiting_realization`, затем создать ledger с тем же `VoyageId`/origin/destination/start time и snapshot cargo quantities + nullable total basis. Rejected/replayed Undock не меняет ledger.
2. На arrival/interruption найти exactly one matching in-transit ledger. Применить `LastVoyageFuelSettlement` только если `settlement.VoyageId` совпадает и posting ID ещё не принят; записать только `RouteFuelCostCredits`, не Refuel total и не kg×current price.
3. Arrival переводит запись в `awaiting_realization`, задаёт destination/completed time; interruption — в `interrupted` и сразу finalized для продаж. Duplicate terminal callback no-op. Retention удаляет oldest finalized entries сверх 50, не active/current awaiting entry.
4. В docking dialogue commit сравнить player Credits и `PortFeeDebt` до/после; assessed = paid + debtAdded. Posting key строится из `VoyageId`, station id и `FirstPortFeeGameTimeMs`. Нет matching arrived ledger — не создавать synthetic voyage.
5. В `RenewPortFees` после успешного commit post assessed fee, paid и debt delta с key `(VoyageId, stationId, due)`. Assessed всегда увеличивает expense; paid/debt — только breakdown. Погашение уже существующего debt не повторяет assessed.
6. `RecordVoyageAmount` создаёт единую будущую seam для настоящих event/passenger handlers, но этот ticket не вызывает её фиктивными данными. `credits == 0` не создаёт posting.
7. Все суммы обновлять checked stage-before-assign. Любая ошибка оставляет world, Credits, fee/debt и ledger без частичного изменения.
8. Tests используют public commands/dialogue/time advance dependency и snapshots, а не прямую установку totals; отдельный test может inspect internal state только для overflow atomicity.

## Out of scope

Sell/profit arithmetic и snapshot projection (TK-0003), UI/locale, Save/Load, реализация Undock/fuel/cargo basis, passenger contracts/event costs, debt collection policy, balance tuning.

## Invariants

- Engine — единственный owner механики; Client получает готовые values: `EngineRequirements.md:362–419`.
- US-0009 fuel settlement invariant: consumed + returned = reserved, route cost — consumed basis (`EP-0001-US-0009-TK-0004-voyage-fuel-settlement.md:70–80`).
- Port fee current mutation: assessed/paid/debt разделимы в `SimulationEngine.PortFees.cs:39–48`.
- Dialogue transaction остаётся atomic: candidate строится отдельно и commit выполняется только после success (`DialogueEffectTransaction.cs:11–20,129–132`; `SimulationEngine.Dialogue.cs:165–174`).
- Opening cargo basis не переоценивается по рынку; nullable semantics берутся из US-0010 TK-0002.
- Пять файлов, production layer engine, matching `DeepSpaceSaga.Engine.Tests`.

## Tests

Named tests:

- `Accepted_undock_opens_one_ledger_with_stable_voyage_id_and_opening_cargo` (AC-02).
- `Rejected_or_replayed_undock_does_not_open_or_finalize_extra_ledger` (AC-02).
- `Arrival_posts_route_fuel_once_and_refuel_purchase_is_not_a_second_expense` (AC-01/02).
- `Docking_fee_records_assessed_paid_and_debt_on_arrived_voyage` (AC-04).
- `Daily_port_fee_uses_due_boundary_and_duplicate_boundary_is_idempotent` (AC-02/04).
- `Interrupted_voyage_is_terminal_and_rejects_later_postings` (AC-02).
- `Next_accepted_undock_finalizes_previous_awaiting_realization` (AC-02).
- `Ledger_retention_keeps_latest_fifty_without_dropping_active_entry` (AC-04).

Commands:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~VoyageLedgerLifecycleTests"
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

Lifecycle states, exact hooks, attribution moments, posting/dedupe keys, fee breakdown, retention, failure atomicity, five allowed files и named tests заданы. Implementer должен лишь сверить dependency signatures; создавать соседние механики или искать правила не требуется.
