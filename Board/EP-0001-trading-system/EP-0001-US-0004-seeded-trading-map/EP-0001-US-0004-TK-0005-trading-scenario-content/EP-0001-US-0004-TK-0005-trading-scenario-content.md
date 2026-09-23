---
epic: EP-0001-trading-system
story: EP-0001-US-0004-seeded-trading-map
ticket: EP-0001-US-0004-TK-0005-trading-scenario-content
title: Три стартовых варианта одной сети
stage: approved
layer: content-data
depends_on: [EP-0001-US-0004-TK-0004-map-bootstrap-save, EP-0001-US-0001-TK-0003-market-catalog-content]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07]
created: 2026-09-22T19:26:16Z
revision: 1
---

# Три стартовых варианта одной сети

## Why

Дать New Game три штатных сценария с одинаковым map-generation contract и
разными начальными состояниями корабля. End state: Default, Docked и Undocked
проходят один engine bootstrap, дают одну сеть при одном seed, а Default_500
остаётся прежним stress scenario.

## Decisions

2026-09-22T19:26:16Z — пользователь попросил создать недостающие тикеты; продолжается
утверждённый план US-0004. Новых продуктовых решений пользователь не добавлял.

## Assumptions

- Map rules находятся в optional `gameState.tradingMapGeneration` каждого из
  трёх scenario JSON; они byte-for-byte эквивалентны после canonical ordering.
- Master seed для cross-scenario comparison задаётся тестовым harness, потому
  что обычный New Game без seed обязан получить новый random masterSeed.
- Default/ Docked/Undocked сохраняют свои существующие ship position, speed,
  docking и loadout поля; добавляются только map rules.
- Пять station market profiles ссылаются на catalog/profile IDs US-0001; цены
  и удалённые authoritative quotes здесь не публикуются.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json | Четыре исходных объекта; `SPC-0002` station at `:117–140`, two asteroids at `:143–166` | Add valid schemaVersion=1 `tradingMapGeneration` rules, five station roles, templates, risks and configurable thresholds |
| src/DeepSpaceSaga.Client/Scenarios/Docked/scenario.json | Existing `SPC-0001` is docked to `SPC-0002` at `:18–29`; station begins at `:121` | Add the same canonical map-generation rules without changing docked ship/loadout state |
| src/DeepSpaceSaga.Client/Scenarios/Undocked/scenario.json | Existing `SPC-0001` is undocked at `:18–29`; station begins at `:120` | Add the same canonical map-generation rules without changing undocked ship/loadout state |
| tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs | Real scenario bootstrap and first authoritative snapshot are covered at `:383–472` | Parameterized three-scenario content/engine smoke, same-seed equivalence, five markers and Default_500 regression |

## Public API after the change

Нового API нет. Content adds only JSON values accepted by
`TradingMapGenerationData` from TK-0001:

```json
{
  "tradingMapGeneration": {
    "schemaVersion": 1,
    "startStationObjectId": "SPC-0002",
    "referenceSpeedMps": 700,
    "shortMaxGameTimeMs": 21600000,
    "mediumMaxGameTimeMs": 64800000,
    "maxTravelGameTimeMs": 129600000,
    "minStationDistanceKm": 5,
    "clearanceKm": 2,
    "stations": [],
    "templates": [],
    "riskProfiles": []
  }
}
```

The arrays are populated with the five roles, three valid templates and the
named risk profiles required by TK-0002/TK-0003; no remote prices are included.

## Implementation steps

1. Add identical canonical rules to Default, Docked and Undocked: roles
   `market.transit`, `market.mining`, `market.industrial`, `market.hydroponic`,
   `market.scientific-military`; names/sizes; three templates; valid risks;
   thresholds and geometry clearances.
2. Keep original JSON fields and ordering semantics for player ship, SPC-0002,
   initial asteroids, docking and loadout. Do not add pre-materialized stations
   or edge blocks to scenario files; New Game generation owns them.
3. Add Client integration coverage that loads all three real scenarios, injects
   one equal master seed into equivalent test copies, and compares normalized
   `TradingMap` topology/roles/coordinates/edge metadata/cargo flows/RNG state.
   Assert five Station objects in the first authoritative snapshot and existing
   marker visibility at the current suitable zoom/marker policy.
4. Assert Docked/Undocked state differences remain intact, Default_500 still
   loads its existing 502-object stress fixture, and save output contains
   materialized map classification/flows but no `tradingMapGeneration` request.

## Out of scope

Default_500 content rewrite, production engine/client code, a new graph screen or
edge drawing, price tables, dynamic economy, voyage/fuel/risk settlement,
asteroid-field generation and remote-market API.

## Invariants

- Scenario loading is strict for unknown JSON fields:
  `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:27–30`.
- The shipped scenario tree and existing scenario smoke surface are under
  `src/DeepSpaceSaga.Client/Scenarios` and
  `tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs:383–472`.
- `SPC-0002` remains the Large starting station and the ship remains `SPC-0001`;
  existing scenario tests assert this in
  `tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs:136–170`.
- All three files use the same canonical map rules; differing ship state is the
  only intentional scenario difference for this story.

## Tests

`LocalSessionIntegrationTests`:

- `Three_start_scenarios_have_equivalent_seeded_map_and_preserve_ship_state` (AC-01/05).
- `Each_seeded_scenario_publishes_five_station_markers_with_edge_and_flow_save` (AC-01/02/03/07).
- `Scenario_content_rejects_missing_role_template_or_risk_reference` (AC-02/03/06).
- `Default_500_remains_the_existing_502_object_stress_scenario` (AC-07).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~LocalSessionIntegrationTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Изменены только три scenario JSON и один matching Client test file.
- Все AC этой истории покрыты named integration/content tests; exact remote
  prices не появляются в artifacts.
- Client test/build и format проходят либо конкретное исходное падение записано.
- Existing ship/docking/loadout/Default_500 behavior не изменён; map rules
  canonical and references resolve through US-0001 content.
- Save evidence проверяет materialized map, flows and classification, а не
  только наличие input config.

## Self-containment check

Полный content shape, canonical IDs, role list, scenario paths, test harness,
stress regression и ограничения перечислены. Implementer не должен менять
Engine files или искать дополнительные scenario/config owners.
