---
epic: EP-0001-trading-system
story: EP-0001-US-0009-voyage-fuel-cost
ticket: EP-0001-US-0009-TK-0002-fuel-accounting-foundation
title: Эффективность и себестоимость топлива в баке
stage: approved
layer: engine
depends_on: []
files_touched: 5
serves: [AC-01, AC-05, AC-06]
created: 2026-09-21T12:38:06Z
revision: 1
---

# Эффективность и себестоимость топлива в баке

## Why

Добавить Engine-owned исходные данные для route fuel calculation и денежной оценки: эффективность двигателя и conserved acquisition basis каждого бака. End state: content может задать положительную эффективность, refuel увеличивает basis на точную стоимость, save/load сохраняет amount+basis, а старое топливо получает детерминированный bootstrap.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`.

## Decisions

Пользователь не добавлял отдельных решений. Integer km/kg, total cost basis и legacy bootstrap приняты как story assumptions A-03/A-05/A-08 для выполнения формулы эпика без float/double.

## Assumptions

Поле efficiency optional на этапе загрузки: legacy/custom content продолжает загружаться, но TK-0004 отклонит старт voyage без положительного значения. `FuelCostBasisCredits` — total basis текущих kg в module, не unit price и не отдельное списание Credits.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs` | Engine type содержит `FuelCapacityKg`, но не efficiency (`:5–20`) | Trailing optional `long? FuelEfficiencyKmPerKg` |
| `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs` | DTO/constructor/engine validation обрабатывают capacity (`:294–330,676–692`) | JSON field, mapping и positive/owner validation без требования для legacy |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs` | `ShipModuleData` сохраняет `FuelAmountKg`, basis отсутствует (`:291–302`) | Trailing optional `fuelCostBasisCredits` |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Runtime/load/save fuel — `:836–859,930–995,1084–1105`; refuel — `:2115–2144`; отдельные engine cycles не меняют fuel | Runtime basis, bootstrap/save/refuel update и pure allocation helper; не трогать движение |
| `tests/DeepSpaceSaga.Engine.Tests/VoyageFuelAccountingTests.cs` | Новый файл; fuel load/refuel tests сейчас разделены между Scenario/Trade tests | Loader, bootstrap, refuel, conservation, save/load regressions |

## Public API after the change

No Contracts/public session API change. Engine-internal/data schema:

```csharp
// ModuleImplementation JSON + internal definition
long? FuelEfficiencyKmPerKg

// ShipModuleData JSON + InstalledModuleRuntime
long? FuelCostBasisCredits // DTO; runtime is resolved non-negative long for fuel modules
```

Resolution rules:

```text
engine module + explicit basis >= 0  => use explicit basis
engine module + missing basis        => FuelAmountKg * item.fuel.BasePriceCredits
non-fuel module                       => runtime basis 0; explicit non-zero is invalid
```

## Implementation steps

1. Добавить optional `fuelEfficiencyKmPerKg` в module DTO/definition. Если поле задано, значение должно быть `>0` и category — `module.engine`; ноль/negative/non-engine отклоняются `ContentException`. Missing остаётся null до TK-0003.
2. Добавить optional `fuelCostBasisCredits` последним полем `ShipModuleData`. При runtime load проверить non-negative. Для fuel module без поля вычислить checked `FuelAmountKg × basePrice(item.fuel)`; отсутствие `item.fuel` при положительном fuel — явный `ScenarioException`, не нулевая basis.
3. Добавить `FuelCostBasisCredits` в `InstalledModuleRuntime`, записывать его рядом с `FuelAmountKg` в save. Для нулевого fuel basis должна быть 0; explicit positive basis при 0 kg отклонить как inconsistent.
4. В atomic refuel branch после всех validation увеличить `FuelAmountKg` на qty и `FuelCostBasisCredits` на exact command cost. Rejected/replayed command не меняет basis; Credits/stock/tank/basis коммитятся вместе.
5. Добавить internal pure helper пропорционального отделения basis: 0 kg → 0; полный объём → вся basis; иначе integer ratio с `MidpointRounding.AwayFromZero`, checked arithmetic и clamp `[0,totalBasis]`. TK-0004 переиспользует helper.
6. Tests загружают explicit/missing basis, проверяют invalid data, refuel по authoritative station price, exact save round-trip и conservation allocation. Отдельно подтвердить, что Accelerate/Brake/Turn/Approach не меняют amount/basis.

## Out of scope

Active voyage/reservation/settlement, Contracts snapshot, shipped efficiency value, fuel transfer, sell-back, cargo COGS, UI, net profit и изменение engine command mechanics.

## Invariants

- Fuel — kg в Engine module, не cargo: `EngineRequirements.md:4928–4954,5128`.
- Money path integer/fixed-point, без float/double: `EngineRequirements.md:5247–5265`.
- Runtime fuel остаётся в `[0,FuelCapacityKg]`: `SimulationEngine.cs:1084–1105`.
- Refuel validation выполняется до commit: `SimulationEngine.cs:2121–2144`; новый basis входит в тот же atomic update.
- Пять files, один production layer engine, matching `DeepSpaceSaga.Engine.Tests`.

## Tests

`VoyageFuelAccountingTests`:

- `Engine_efficiency_accepts_positive_optional_value_and_rejects_invalid_owner_or_value` (AC-01).
- `Legacy_fuel_bootstraps_basis_from_item_fuel_base_price` (AC-05).
- `Explicit_fuel_basis_round_trips_without_revaluation` (AC-05).
- `Refuel_adds_exact_authoritative_cost_to_tank_basis_atomically` (AC-05).
- `Rejected_or_replayed_refuel_does_not_add_basis_twice` (AC-05).
- `Proportional_basis_allocation_conserves_total_with_away_from_zero_rounding` (AC-05).
- `Individual_engine_commands_do_not_change_fuel_or_basis` (AC-06).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter FullyQualifiedName~VoyageFuelAccountingTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Steps выполнены только в пяти разрешённых файлах; JSON content и Contracts не изменены.
- AC-01/05/06 покрыты named tests, build/format проходят либо baseline failure записано отдельно.
- Amount+basis сохраняются/восстанавливаются без переоценки; refuel и allocation сохраняют conservation/exactly-once.
- Existing engine commands не расходуют fuel; missing efficiency остаётся compatibility state, не скрытым нулём.
- Нет незаписанных assumptions, блокирующих вопросов или скрытой работы.

## Self-containment check

Schema fields, validation, bootstrap source, rounding, atomic update, exact files и tests заданы. Implementer не проектирует voyage lifecycle и не ищет числовой baseline в других документах.
