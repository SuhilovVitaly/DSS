---
epic: EP-0001-trading-system
story: EP-0001-US-0004-seeded-trading-map
ticket: EP-0001-US-0004-TK-0004-map-bootstrap-save
title: New Game и восстановление сети
stage: approved
layer: engine
depends_on: [EP-0001-US-0004-TK-0003-map-geometry, EP-0001-US-0001-TK-0002-profile-market-bootstrap]
files_touched: 3
serves: [AC-01, AC-04, AC-05, AC-06]
created: 2026-09-22T19:26:16Z
revision: 2
---

# New Game и восстановление сети

## Why

Подключить materialized map к существующему engine lifecycle. End state: New
Game строит карту до commit мира, а Save/Load принимает уже сохранённые
stations, edges и RNG без повторной генерации, дублей или сдвига координат.

## Decisions

2026-09-22T19:26:16Z — пользователь попросил создать недостающие тикеты; продолжается
утверждённый план US-0004. Новых продуктовых решений пользователь не добавлял.

2026-09-23 — пользователь разрешил расширить scope TK-0004 на
симметричную проверку направленного cargo flow против неориентированного edge в
`TradingMapData.cs`, необходимую для Save/Load materialized map.

## Assumptions

- `tradingMapGeneration` разрешён только в New Game при `gameTimeMs == 0`;
  `tradingMap` является единственным map block в Save.
- Генератор получает новый `masterSeed`, если New Game его не содержит, и
  сохраняет его в materialized state; Save всегда повторно использует seed.
- Existing scenarios without map fields остаются legacy path без retrofit.
- Профильный market bootstrap вызывается один раз на материализованные станции;
  prices, voyage lifecycle и dynamic market simulation остаются соседними
  историями.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | `LoadScenario` validates and resolves seed at `:197–231`, builds runtime objects at `:233–245`, saves materialized objects through `CaptureSaveState` at `:780–825` | Stage New Game map generation and saved-map restoration before world commit; include map state in snapshot/save capture without regenerating |
| tests/DeepSpaceSaga.Engine.Tests/TradingMapBootstrapTests.cs | Новый файл; existing load/save seams are `ScenarioEngineTests.cs:136–170` and `SimulationEngine.CaptureSaveStateForTests` | New Game, three initial states, save/load equality, invalid-map atomicity and legacy regressions |
| src/DeepSpaceSaga.Engine/Scenario/TradingMapData.cs | `TradingMapDataValidation` validates cargo-flow endpoint pairs at `:177` | Accept either direction of a directed cargo flow for an undirected materialized edge; preserve flow direction and all other validation |

## Public API after the change

Нового Contracts API нет. Внутренний engine lifecycle получает один helper:

```csharp
private GameStateData MaterializeOrRestoreTradingMap(
    ScenarioFile scenario,
    GameStateData normalizedState,
    bool isSave,
    ulong masterSeed);
```

Он вызывает `TradingMapGraphGenerator`/`TradingMapGeometryGenerator` и
`TradingMapDataValidation` из предыдущих тикетов. `CaptureSaveState` выдаёт
`GameStateData.TradingMap` и не выдаёт `TradingMapGeneration`.

## Implementation steps

1. После `ValidateAndNormalize` и разрешения seed определить mode: New Game с
   request block, materialized scenario, Save с result block или legacy. Не
   трактовать Save как новый request даже при наличии актуального config.
2. Для New Game canonicalize rules, построить graph и geometry, добавить ровно
   четыре новые Station к исходным ship/asteroids/start station, затем вызвать
   существующий profile-aware market bootstrap. Все проверки выполняются на
   staged `GameStateData`; исключение не меняет текущий `_objects`.
3. Для Save валидировать и восстановить сохранённые stations/map/RNG exactly as
   stored; не вызывать graph/geometry RNG, не добавлять станции и не менять
   coordinates. Устаревший config не должен переписывать saved Rules.
4. Передать materialized map в runtime/save projection и сохранить только
   result block вместе с seed, текущими object/module states и compatibility.
   Сохранить Default_500 legacy behavior и существующие market initialization
   invariants.
5. Проверить commit boundary: invalid references, duplicate station IDs,
   impossible geometry или malformed RNG отклоняются до замены мира и содержат
   field/object context в `ScenarioException`.

## Out of scope

Изменение `ScenarioData.cs`, graph/geometry algorithms,
scenario JSON content, Client UI, Approach/Motion, fuel/risk settlement,
dynamic prices, market events and global save-version migration.

## Invariants

- `EngineContentLoader` уже различает scenario/save bootstrap:
  `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:29–50`.
- `LoadScenario` currently validates catalog compatibility and stages runtime
  before commit: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:197–240`.
- Save capture is lock-protected and serializes materialized world:
  `src/DeepSpaceSaga.Engine/SimulationEngine.cs:780–825`.
- Map request/result exclusivity and legacy absence are validated by
  `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:76–107` and TK-0001.

## Tests

`TradingMapBootstrapTests`:

- `New_game_materializes_exactly_five_stations_and_preserves_ship_state` (AC-01).
- `Default_docked_and_undocked_share_same_seeded_map` (AC-01/05).
- `Save_load_restores_map_coordinates_edges_and_rng_without_generation` (AC-06).
- `Invalid_map_does_not_replace_existing_world` (AC-06).
- `Legacy_scenario_without_map_fields_keeps_existing_object_count` (AC-06).
- `Approach_to_each_materialized_station_keeps_ship_speed` (AC-04).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~TradingMapBootstrapTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Изменены только три файла из `Code context`.
- AC-01, AC-04, AC-05 и AC-06 покрыты named tests на New Game, Save/Load,
  atomic rejection и legacy path.
- Tests, Engine build и format проходят либо исходное падение явно записано.
- Save не генерирует карту повторно, не дублирует stations и не переписывает
  текущий мир до успешной валидации.
- Не изменены map DTO, graph/geometry algorithms, Client и соседние истории.

## Self-containment check

Режимы bootstrap, порядок staging/commit, seed/RNG правила, save projection,
error boundary и точные тесты полностью определены; implementer не должен
искать дополнительный lifecycle/API или менять dependency files, кроме явно
перечисленной validation correction в `TradingMapData.cs`.
