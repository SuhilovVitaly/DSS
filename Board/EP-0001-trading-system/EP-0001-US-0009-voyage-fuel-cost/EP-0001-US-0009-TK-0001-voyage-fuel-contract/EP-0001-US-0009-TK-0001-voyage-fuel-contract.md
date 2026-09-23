---
epic: EP-0001-trading-system
story: EP-0001-US-0009-voyage-fuel-cost
ticket: EP-0001-US-0009-TK-0001-voyage-fuel-contract
title: Контракт reservation и settlement топлива
stage: approved
layer: contracts
depends_on: []
files_touched: 5
serves: [AC-02, AC-03, AC-04, AC-06]
created: 2026-09-21T12:38:06Z
revision: 1
---

# Контракт reservation и settlement топлива

## Why

Передать клиенту и следующим stories authoritative данные о зарезервированном/использованном топливе без Engine reference и клиентского пересчёта. End state: active voyage расширен backward-compatible fuel fields, последний terminal settlement доступен в snapshot, оба DTO стабильно сериализуются.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`.

## Decisions

Пользователь указал canonical story path; технических решений сверх story/epic не добавлял. Обязательные поля lifecycle dependency и aggregate fuel DTO зафиксированы assumptions A-01/A-07.

## Assumptions

Тикет исполняется после contracts-части US-0014, которая создаёт `ActiveVoyageSnapshot.cs` с `VoyageId`, origin/destination, state и progress. Nullable fuel fields сохраняют возможность промежуточного lifecycle без US-0009 и consumer US-0006. Contracts публикует значения, но не вычисляет их.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/ActiveVoyageSnapshot.cs` | Планируемый dependency US-0014; сейчас voyage API отсутствует, consumer shape записан в `EP-0001-US-0006-repeatable-trading-voyage.md:46–54` | Добавить trailing nullable reserved/projected fields, не менять lifecycle fields |
| `src/DeepSpaceSaga.Contracts/VoyageFuelSettlementSnapshot.cs` | Новый файл; route fuel receipt отсутствует | Immutable aggregate settlement DTO с exact fields ниже |
| `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs` | Positional record заканчивается `SimulationTimeMs` (`:11–53`); optional projections используют trailing defaults | Добавить последним nullable `LastVoyageFuelSettlement` |
| `src/DeepSpaceSaga.Contracts/CommandResult.cs` | Stable reason codes заканчиваются trade validation (`:77–142`) | Добавить два voyage fuel reason code без изменения `CommandResult` shape |
| `tests/DeepSpaceSaga.Contracts.Tests/VoyageFuelContractTests.cs` | Новый файл; JSON/default patterns есть в `StationTradeSnapshotTests.cs` | Round-trip, legacy/default и arithmetic-free contract tests |

## Public API after the change

Trailing fields dependency record:

```csharp
public sealed record ActiveVoyageSnapshot(
    // lifecycle fields supplied by US-0014,
    long? ReservedFuelKg = null,
    long? ProjectedConsumedFuelKg = null,
    long? ProjectedRouteFuelCostCredits = null);
```

Новые DTO/поле:

```csharp
public sealed record VoyageFuelSettlementSnapshot(
    string VoyageId,
    long ReservedFuelKg,
    long ConsumedFuelKg,
    long ReturnedFuelKg,
    long RouteFuelCostCredits);

// Last trailing AuthoritativeSnapshot parameter:
VoyageFuelSettlementSnapshot? LastVoyageFuelSettlement = null

public const string InsufficientVoyageFuel = "insufficient_voyage_fuel";
public const string FuelEfficiencyUnavailable = "fuel_efficiency_unavailable";
```

Все количества/стоимость неотрицательны по Engine invariant; Contracts не бросает validation exceptions. Для settlement Engine гарантирует `ConsumedFuelKg + ReturnedFuelKg == ReservedFuelKg`.

## Implementation steps

1. В dependency `ActiveVoyageSnapshot` добавить три trailing nullable поля точно с указанными именами. `null` означает, что US-0009 не подключена; не трактовать как нулевой/free рейс.
2. Создать `VoyageFuelSettlementSnapshot` без Engine types, методов расчёта или UI text.
3. Добавить `LastVoyageFuelSettlement` последним параметром `AuthoritativeSnapshot`, не переставляя `RouteArrivalGameTimeMs`/`SimulationTimeMs` и существующие fields.
4. Добавить две exact snake_case константы в `CommandReasonCodes`; тексты UI и новые status values не вводить.
5. JSON tests проверяют полный active/settlement round-trip, legacy snapshot без новых полей, explicit null, крупные `Int64` значения и exact reason constants.

## Out of scope

Расчёт формулы, validation, reason codes, tank mutation, persistence Engine state, lifecycle, ledger/history, локализация и UI.

## Invariants

- Contracts не зависит от Engine/Client: `Documentation/00-Process/CLAUDE.md:44–54`.
- Public boundary immutable и JSON-serializable: `AuthoritativeSnapshot.cs:6–11`.
- Existing constructors/legacy JSON сохраняются trailing defaults; nullable не выдаёт отсутствующую реализацию за нулевой расход.
- Только пять файлов таблицы; production layer contracts, matching `DeepSpaceSaga.Contracts.Tests`.

## Tests

`VoyageFuelContractTests`:

- `Active_voyage_round_trips_reserved_projected_fuel_fields` (AC-02/03/06).
- `Fuel_settlement_round_trips_exact_int64_values` (AC-02/04).
- `Legacy_snapshot_defaults_voyage_fuel_fields_to_null` (AC-04/06).
- `Missing_active_fuel_fields_remain_null_not_zero` (AC-06).
- `Voyage_fuel_reason_codes_are_stable_snake_case_values` (AC-04).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter FullyQualifiedName~VoyageFuelContractTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Contracts\DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Implementation выполнена только в пяти разрешённых файлах.
- AC-02/03/04/06 покрыты named serialization/default tests; build/format проходят либо baseline failure записано отдельно.
- Signatures и trailing compatibility соблюдены; Contracts не содержит fuel arithmetic.
- Нет скрытых assumptions, блокирующих вопросов или работы вне `Code context`.

## Self-containment check

Точные имена, типы, null semantics, ordering и tests заданы. Единственный внешний gate — существование lifecycle record US-0014; при другом имени файла/типа ticket возвращается в review.
