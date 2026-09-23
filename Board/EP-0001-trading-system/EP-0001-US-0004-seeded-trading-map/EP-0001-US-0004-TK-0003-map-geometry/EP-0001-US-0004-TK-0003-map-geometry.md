---
epic: EP-0001-trading-system
story: EP-0001-US-0004-seeded-trading-map
ticket: EP-0001-US-0004-TK-0003-map-geometry
title: Геометрия и классы расстояний
stage: draft
layer: engine
depends_on: [EP-0001-US-0004-TK-0002-economic-graph]
files_touched: 2
serves: [AC-03, AC-04, AC-05]
created: 2026-09-22T19:26:16Z
revision: 2
---

# Геометрия и классы расстояний

## Why

Выбранный seeded graph должен получить воспроизводимые координаты и полную
metadata для каждого экономического ребра. End state: один чистый geometry
planner возвращает пять станций, безопасное размещение относительно исходного
мира и edges с корректными distance/travel/risk полями, не меняя физику Approach.

## Decisions

2026-09-22T19:26:16Z — пользователь попросил создать недостающие тикеты; продолжается
утверждённый план US-0004. Новых продуктовых решений пользователь не добавлял.

## Assumptions

- Геометрия применяется к `TradingGraphPlan` из TK-0002 и не выбирает новый
  topology template.
- `ReferenceSpeedMps`, пороги времени, `MinStationDistanceKm` и `ClearanceKm`
  берутся из `TradingMapGenerationData`, а не дублируются константами.
- Координаты хранятся в тех же world units, что `SpaceObjectData`; километры
  используются только в расчёте metadata и ограничений.
- Для `TradingMap.Geometry` используется отдельный deterministic stream с
  точным replay-контрактом: `seed =
  RngStreamSeedDerivation.DeriveStreamSeed(masterSeed, "TradingMap.Geometry")`,
  затем пропустить ровно 1000 вызовов `NextDouble()`, выполнить один draw,
  `quarterTurns = floor(value * 4)`, диапазон `0..3`, и сохранить
  `TradingMapRngData("TradingMap.Geometry", seed, 10010)`. Других draw в этом
  stream нет; `TradingMap.Topology` не потребляется и не изменяется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/TradingMapGeometryGenerator.cs | Новый файл; schema DTO находятся в `TradingMapData.cs:42–70`, graph contract задан в TK-0002 | Pure geometry planner, edge metadata calculation, deterministic geometry RNG и placement validation |
| tests/DeepSpaceSaga.Engine.Tests/TradingMapGeometryTests.cs | Новый файл; existing scenario fixtures use `ScenarioEngineTests.cs:9–68`, motion units and Approach behavior are covered by matching Engine tests | In-memory five-station graph fixtures, deterministic placement, clearance and distance-class regressions |

## Public API after the change

Нового Contracts API нет. В `DeepSpaceSaga.Engine.Scenario`:

```csharp
internal sealed record TradingMapGeometryPlan(
    TradingMapStateData State,
    IReadOnlyList<SpaceObjectData> Stations,
    TradingMapRngData GeometryStream);

internal static class TradingMapGeometryGenerator
{
    internal static TradingMapGeometryPlan Generate(
        TradingGraphPlan graph,
        IReadOnlyList<SpaceObjectData> existingObjects,
        ulong masterSeed);
}
```

`State.Edges` сохраняет endpoint/risk набор выбранного template и добавляет
`DistanceKm`, `TravelEstimateGameTimeMs`, `DistanceClass` и
`FuelMultiplierPermille`. Geometry edges неориентированы: `FromStationObjectId`
и `ToStationObjectId` всегда записываются в ordinal-порядке canonical endpoint
pair. Направленными остаются только `State.CargoFlows`; поэтому «безопасное
короткое направление от start» означает safe Short edge, инцидентное
`StartStationObjectId`, независимо от стороны `From`/`To`.
`risk.safe` — точный safe profile ID и обязан иметь
`FuelMultiplierPermille == 1000`. `State.CargoFlows` и topology stream
передаются без пересчёта.

## Implementation steps

1. Канонизировать пять station rules, template links и offsets по ordinal
   `ObjectId`, выбрать только `QuarterTurns` через точный
   `TradingMap.Geometry` replay-контракт, а `SPC-0002` оставить в исходной
   позиции с исходным именем, size, profile, inventory и остальными полями
   `SpaceObjectData`. Offsets — относительные координаты template в world
   units (`1 world unit = 100 m`), а не абсолютные координаты и не километры.
   Для station offset `o_i` и start offset `o_start` вычислять:
   `position_i = startPosition + Rotate(o_i - o_start, QuarterTurns)`.
   `startPosition` берётся из исходного `SPC-0002`; start station не
   пересоздаётся по offset. Rotation вокруг start имеет точную таблицу:
   `q=0: (x,y)`, `q=1: (-y,x)`, `q=2: (-x,-y)`, `q=3: (y,-x)`.
2. Проверить конечность координат и расстояния между всеми новыми станциями и
   исходными station/asteroid objects; соблюсти `MinStationDistanceKm` и
   `ClearanceKm`, разрешая только штатное соседство player ship со стартовой
   станцией. При невозможном размещении бросить `ScenarioException` с template,
   object и причиной, не возвращая частичное состояние.
3. Для каждого canonical, неориентированного edge вычислить евклидово
   `DistanceKm` в километрах (`distanceWorldUnits * 0.1`), затем
   `ceil(distanceKm / (ReferenceSpeedMps / 1000) * 1000 * 300)` в игровых ms.
   Классифицировать по вычисленному integer estimate с включёнными верхними
   границами: `Short` если `estimate <= ShortMaxGameTimeMs`, `Medium` если
   `ShortMaxGameTimeMs < estimate <= MediumMaxGameTimeMs`, `Long` если
   `MediumMaxGameTimeMs < estimate <= MaxTravelGameTimeMs`; превышение max
   отвергать. Перенести risk multiplier/profile без изменения endpoint pair.
   Проверить наличие Short, Medium и Long, safe Short edge, инцидентного
   start, и Long edge с scientific/military endpoint.
4. Сохранить `QuarterTurns`, выбранный template, geometry stream и все edge
   metadata в `TradingMapStateData`; не изменять motion speed, obstacle
   avoidance или Approach implementation.

## Out of scope

New Game/save bootstrap, materialization into `SimulationEngine`, scenario JSON,
UI edge rendering, route planning changes, runtime fuel deduction, risk
resolution, asteroid generation and market price/stock changes.

## Invariants

- `GameStateData` map fields и edge schema заданы в
  `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:54–77` и
  `TradingMapData.cs:42–70`.
- Existing world is built before replacement in
  `src/DeepSpaceSaga.Engine/SimulationEngine.cs:233–240`.
- World-unit conversion and Approach constraints remain those documented in
  `Documentation/01-Requirements/EngineRequirements.md:5337–5341`.
- Same master seed, canonical config and existing-object set produce identical
  stations, edges, geometry stream seed and counter, независимо от порядка
  `Stations`, `Templates`, `Links`, `Offsets` и `existingObjects`.
- Golden RNG corpus: for `masterSeed=123`, stream seed for
  `TradingMap.Geometry` is `17895960153916692429`, `QuarterTurns == 1`, and
  counter is exactly `10010`.
- `FromStationObjectId`/`ToStationObjectId` order is not a cargo direction;
  safe-start validation checks edge incidence. Cargo-flow direction is owned by
  TK-0002 and is passed through unchanged.

## Tests

`TradingMapGeometryTests`:

- `Geometry_is_deterministic_for_same_seed_and_reordered_inputs` (AC-05).
- `Stations_respect_minimum_separation_and_asteroid_clearance` (AC-04).
- `Edges_have_distance_time_class_fuel_and_risk_metadata` (AC-03).
- `Classes_cover_short_medium_long_and_science_is_long` (AC-03).
- `Invalid_or_impossible_geometry_is_rejected_without_partial_result` (AC-04).
- `Geometry_stream_is_independent_from_topology_and_unrelated_rng` (AC-05).
- `Geometry_rng_replay_has_golden_seed_quarter_turns_and_counter` (AC-05):
  manually replay the 1000 skipped draws and one draw, asserting the exact
  seed/quarter-turn/counter corpus above.
- `Offsets_are_rotated_from_start_offset_in_world_units` (AC-04/05): assert
  all four rotation table cases, start-anchor preservation, and the `0.1`
  world-unit-to-km conversion.
- `Distance_classes_use_inclusive_boundaries_and_configured_reference_speed`
  (AC-03): cover exact Short/Medium boundaries and a non-default
  `ReferenceSpeedMps` without hardcoded time values.
- `Reordered_stations_templates_links_offsets_and_objects_are_canonical`
  (AC-05): compare canonical station coordinates, endpoint order and metadata
  after reordering every accepted input collection.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~TradingMapGeometryTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj whitespace --verify-no-changes --no-restore --include Scenario/TradingMapGeometryGenerator.cs
dotnet format D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj whitespace --verify-no-changes --no-restore --include TradingMapGeometryTests.cs
```

Full-solution `dotnet format --verify-no-changes` is informational while the
repository has pre-existing unrelated EOL/whitespace violations. Such baseline
findings must be reported separately and do not waive the two scoped gates above.

## Definition of Done

- Реализация ограничена двумя файлами из `Code context`.
- AC-03, AC-04 и AC-05 покрыты named tests с фиксированными seed/config.
- Engine tests, matching build и обе scoped format checks проходят либо
  конкретное исходное падение записано отдельно; full-solution formatter debt
  фиксируется отдельно и не блокирует этот ticket.
- Geometry не меняет Approach, motion, ship, исходные астероиды или topology.
- Результат проверяем по `TradingMapStateData`, station coordinates, edge
  metadata, RNG state и отрицательным тестам.

## Self-containment check

DTO, входной graph contract, формулы, пороги, точный RNG replay, offset
transform, edge direction semantics, ограничения размещения и named tests
заданы полностью. Реализация не требует поиска новых API или изменения файлов
TK-0001/TK-0002.

## Review revision 2

2026-09-22 — review result: Request changes. Зафиксированы точный geometry RNG
replay и golden corpus, формула относительных world-unit offsets и rotation
table, неориентированная семантика edge endpoints, inclusive distance-class
boundaries, дополнительные order/boundary tests и scoped formatter gates.
Ticket возвращён в `draft` до повторного approval.
