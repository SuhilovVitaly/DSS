---
epic: EP-0001-trading-system
story: EP-0001-US-0009-voyage-fuel-cost
ticket: EP-0001-US-0009-TK-0004-voyage-fuel-settlement
title: Authoritative reservation, refund и расход рейса
stage: approved
layer: engine
depends_on: [EP-0001-US-0009-TK-0001-voyage-fuel-contract, EP-0001-US-0009-TK-0002-fuel-accounting-foundation, EP-0001-US-0009-TK-0003-engine-efficiency-content]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06]
created: 2026-09-21T12:38:06Z
revision: 1
---

# Authoritative reservation, refund и расход рейса

## Why

Подключить топливный accounting к authoritative lifecycle рейса. End state: Undock резервирует рассчитанные kg+basis атомарно, arrival расходует весь резерв, interruption возвращает неиспользованную часть по progress, snapshot/save/command retry дают один согласованный результат.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`.

## Decisions

Пользователь не добавлял решений кроме выбранной story. Формула, escrow, rounding, tank order, terminal semantics и last-settlement handoff зафиксированы в AC/A-01…A-08 story.

## Assumptions

Тикет исполняется только после US-0014 и US-0006. US-0014 владеет state machine/progress и persisted `ActiveVoyageData`; US-0006 доказывает physical A→B→A. Active voyage уже фиксирует `DistanceKm` и effective `FuelMultiplierPermille`. Если файлы/signatures dependency отличаются от таблицы, ticket возвращается в review до изменений.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs` | Сейчас voyage block отсутствует; `GameStateData` optional state заканчивается catalog compatibility (`:52–78`) | В dependency `ActiveVoyageData` добавить reservation parts; добавить optional persisted last settlement |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Voyages.cs` | Планируемый owner US-0014; сейчас Undock отсутствует (`NavigationComputerCommandTypes.cs:6–14`, `StationScreen.cs:83–96`) | Два hook: reserve до lifecycle commit, settle до terminal removal; никаких дублированных states |
| `src/DeepSpaceSaga.Engine/SimulationEngine.VoyageFuel.cs` | Новый partial; fuel voyage arithmetic отсутствует | Checked calculation, staged reservation, projection, exactly-once arrival/interruption settlement |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Snapshot/save/load и module projection централизованы (`:196–359,510–534,720–859`) | Хранить/восстанавливать last settlement и передать fuel fields в Contracts projection |
| `tests/DeepSpaceSaga.Engine.Tests/VoyageFuelLifecycleTests.cs` | Новый файл; lifecycle/fuel integration coverage отсутствует | Formula, rejection atomicity, success/interruption, retry, save/load, no-engine-burn tests |

## Public API after the change

Используется Contracts API TK-0001; других public types нет. Engine-internal persisted shape:

```csharp
public sealed record VoyageFuelReservationPartData(
    string ModuleId,
    long ReservedFuelKg,
    long ReservedFuelCostBasisCredits);

// trailing field on dependency ActiveVoyageData
IReadOnlyList<VoyageFuelReservationPartData>? FuelReservationParts = null;

// trailing GameStateData field
VoyageFuelSettlementSnapshot? LastVoyageFuelSettlement = null;
```

Required partial seam:

```csharp
private string? TryReserveVoyageFuel(long distanceKm, int fuelMultiplierPermille,
    out ImmutableArray<VoyageFuelReservationPartData> parts);
private VoyageFuelSettlementSnapshot? SettleVoyageFuel(
    ActiveVoyageData voyage, bool arrived, int progressPermille);
private (long ConsumedKg, long CostCredits) ProjectVoyageFuel(ActiveVoyageData voyage);
```

`null` reason from reserve means success; failure uses `CommandReasonCodes.InsufficientVoyageFuel`, `FuelEfficiencyUnavailable` or existing `value_overflow` convention. Lifecycle не меняется при failure.

## Implementation steps

1. Validation выполняется до mutation: active player ship/destination/edge supplied dependency; `distanceKm>0`, multiplier>0, progress in 0..1000; все propulsion efficiencies positive and equal for MVP. Missing/conflicting efficiency → `fuel_efficiency_unavailable`.
2. Вычислить `reservedFuelKg = CeilingDivide(checked(distanceKm × multiplier), checked(efficiency × 1000))`; result должен быть positive. Не использовать `double/float/Math.Ceiling`.
3. Собрать fuel modules в installed order, проверить aggregate available. При нехватке вернуть `insufficient_voyage_fuel` без изменения docking, active voyage, amount, basis, receipt или command result side effects beyond one Rejected result.
4. Построить staged reservation parts: взять kg по module order, выделить basis helper TK-0002, затем одним commit уменьшить amount+basis и сохранить parts в создаваемом voyage. CommandId replay переиспользует durable receipt и не вызывает reserve снова.
5. Snapshot active voyage суммирует reserved; projected consumed = 0 при progress 0, иначе `min(reserved, CeilingDivide(reserved×progress,1000))`; projected cost выделяется тем же basis helper без mutation.
6. Arrival terminal path вызывает settlement с consumed=reserved/returned=0. Interruption использует projected consumed, для каждого reservation part возвращает unused kg и basis в тот же ModuleId после capacity/identity validation. Все module updates, receipt и lifecycle terminal commit атомарны под world lock.
7. `RouteFuelCostCredits` равен сумме consumed basis; returned basis возвращается. Проверить `consumed+returned=reserved`, отсутствие negative/overflow и conservation `tank basis + consumed cost` до/после settlement.
8. Exactly-once guard использует stable VoyageId: повторный terminal callback после persisted `LastVoyageFuelSettlement` no-op и возвращает тот же receipt. Новый voyage не очищает receipt; следующий terminal settlement заменяет его.
9. Save mid-voyage хранит already-reduced tanks и exact parts; load не резервирует повторно. Save after settlement хранит tanks и last receipt без active reservation. Projection заполняет TK-0001 fields из одной модели.
10. Integration tests используют public `ReceiveCommand`/snapshot/save seams dependency, без прямой мутации runtime. Отдельно проверить, что физическая speed/Approach и обычные engine commands не затронуты.

## Out of scope

Создание lifecycle/Undock/UI, route evaluator, transfer между баками, sell-back, fuel leak/damage, cargo COGS, multi-voyage ledger, Finance/TradeJournal rendering, net profit и balance tuning.

## Invariants

- Engine — единственный owner механики: `EngineRequirements.md:362–419`; Client получает готовые values.
- Fuel kg/capacity conventions: `EngineRequirements.md:4928–4954`; никаких cargo stacks.
- Undock сохраняет physical vector: `EngineRequirements.md:119–129`; Approach speed не меняется: `:5335–5343`.
- Command dedup/receipts: `SimulationEngine.cs:130–143`; `SimulationEngine.CommandJournal.cs:8–54`.
- Save/snapshot строятся под world lock: `SimulationEngine.cs:196–359,398–535,720–833`.
- Пять files, production layer engine, matching `DeepSpaceSaga.Engine.Tests`.

## Tests

`VoyageFuelLifecycleTests`:

- `Undock_reserves_ceiling_distance_multiplier_over_efficiency_with_integer_math` (AC-01/02).
- `Insufficient_fuel_rejects_before_docking_voyage_tank_or_basis_changes` (AC-01/04).
- `Missing_or_conflicting_engine_efficiency_rejects_without_partial_reservation` (AC-01).
- `Arrival_consumes_full_reservation_and_publishes_exact_settlement_once` (AC-02/04/05/06).
- `Interruption_at_zero_half_and_full_progress_returns_expected_fuel_and_basis` (AC-03/05).
- `Reservation_spanning_modules_refunds_each_source_without_exceeding_capacity` (AC-03).
- `Repeated_command_and_repeated_terminal_callback_do_not_double_reserve_or_settle` (AC-04).
- `Save_load_mid_voyage_preserves_reservation_without_second_tank_debit` (AC-04/05).
- `Save_load_after_settlement_preserves_tank_basis_and_last_receipt` (AC-04/06).
- `Voyage_fuel_does_not_change_speed_approach_or_individual_engine_command_fuel` (AC-06).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter FullyQualifiedName~VoyageFuelLifecycleTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Steps выполнены только в пяти files; lifecycle/Contracts/content owners не переписаны.
- AC-01…AC-06 покрыты named tests; test/build/format проходят либо конкретный baseline failure записан отдельно.
- Reservation/settlement atomic, deterministic, integer-only и exactly-once; kg+basis conservation доказана.
- Mid/post voyage Save/Load не списывает fuel повторно; public projection совпадает с persisted state.
- Physical motion и отдельные engine commands не меняют fuel; out-of-scope соблюдён.
- Нет скрытых assumptions, блокирующих вопросов или работы вне `Code context`.

## Self-containment check

Формулы, rounding, reason codes, module ordering, persisted parts, lifecycle hooks, atomicity, public projection и named regressions заданы. Implementer не ищет бизнес-решения; несовпадение dependency API требует review, а не расширения ticket.
