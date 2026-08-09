# ТЗ по устранению расхождений реализации с требованиями

Документ фиксирует технические задания для расхождений, найденных при сравнении текущего кода DSS с `deep_space_saga_engine_requirements.md`.

Расхождение `1` принято в пользу текущей версии кода и отражено в основном документе требований:

- секция 21: текущая baseline-модель использует `1 Hz` authoritative snapshot loop без обязательного внутреннего `100 ms` тика;

По расхождению `2` основной документ требований принимает текущий фиксированный `DefaultScenario` с двумя временными астероидами, но сохраняет обязательное требование к `masterSeed`: при `New Game` создаётся новый случайный `masterSeed`, чтобы разные новые игры давали разную процедурную генерацию; `masterSeed` должен сохраняться и загружаться вместе с игрой. Код должен быть исправлен отдельным ТЗ ниже.

## ТЗ-02A: masterSeed для New Game и save/load continuation

Цель: добавить в runtime/session/save модель обязательный `masterSeed`, который создаётся при `New Game`, сохраняется вместе с игрой и загружается при продолжении.

Область изменений:

- Engine session/runtime state for New Game initialization;
- future/current General Save State DTOs, если save/load уже выделен в коде к моменту реализации;
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`;
- `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs` if session metadata crosses the connection boundary;
- Engine and integration tests.

Требования:

- MUST generate a new random unsigned 64-bit `masterSeed` for every `New Game`.
- MUST keep `masterSeed` immutable during the session.
- MUST persist `masterSeed` in General Save State / save JSON.
- MUST load and reuse saved `masterSeed` when continuing/restoring a game.
- MUST derive deterministic RNG stream seeds from `masterSeed` and stream name.
- MUST not regenerate `masterSeed` during ordinary scenario load, snapshot publication, pause/resume, or speed changes.
- SHOULD produce a warning and persist the generated value when loading a legacy save without `masterSeed`, matching the main requirements document.

Acceptance criteria:

1. Two independent `New Game` sessions get different `masterSeed` values.
2. Save followed by load preserves the exact same `masterSeed`.
3. Deterministic RNG stream seed derivation is stable after save/load.
4. Existing fixed `DefaultScenario` loading still works without procedural asteroid generation.
5. Tests cover new game generation, save/load preservation, and legacy missing-seed behavior.

## ТЗ-03: загрузка всех тематических JSON из Settings.json

Цель: привести `EngineContentLoader` к правилу, что все обязательные тематические JSON-файлы, объявленные в `Settings.json`, загружаются и строго валидируются до старта сессии.

Область изменений:

- `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs`;
- `src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs`;
- content DTO/definition files for factory types and recipes, if needed;
- `tests/DeepSpaceSaga.Engine.Tests`.

Требования:

- MUST загрузить `factoryTypes` и `recipes`, если эти пути присутствуют в `Settings.json`.
- MUST считать отсутствующий/нечитаемый объявленный thematic JSON startup error.
- MUST использовать `JsonUnmappedMemberHandling.Disallow` для новых thematic JSON.
- MUST валидировать, что root collections существуют, даже если они пустые.
- MUST строить immutable in-memory configuration до `LoadScenario`.
- MUST не добавлять зависимость Engine от Client, Skia, Silk.NET или UI.

Acceptance criteria:

1. Если `Settings.json` ссылается на несуществующий `factory-types.json`, startup падает с `ContentException`.
2. Если `recipes.json` содержит лишнее поле, startup падает с `ContentException`.
3. Пустые `factoryTypes: []` и `recipes: []` успешно загружаются.
4. Existing module/item/command registry behavior remains unchanged.
5. `dotnet test tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj` passes.

## ТЗ-04: CommandResult и ShipEvent в authoritative snapshot

Цель: сделать outcome обработанных команд видимым клиенту через следующий authoritative snapshot.

Область изменений:

- `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs`;
- new contract DTOs for command results and ship events;
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`;
- `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs`;
- tests in Contracts, Engine and LocalClient.

Требования:

- MUST добавить immutable JSON-serializable `CommandResult` DTO with `commandId`, `objectId`, `moduleId`, `commandType`, `status`, `reasonCode`, `effectiveGameTimeMs`, `deferAttemptCount`.
- MUST добавить `CommandResults` collection в `AuthoritativeSnapshot`.
- SHOULD добавить `ShipEvents` collection или минимальный placeholder DTO, если это нужно для единообразного pipeline.
- MUST публиковать results всех команд, обработанных с момента предыдущего snapshot.
- MUST различать at least `Executed`, `Cancelled`, `Rejected`, `Deferred`, `Failed`.
- MUST preserve `Contracts` dependency-free.

Acceptance criteria:

1. Успешная команда двигателя появляется в следующем snapshot как `Executed`.
2. Невалидная команда появляется как `Rejected` with machine-readable reason.
3. Deferred command appears as `Deferred` with current attempt count.
4. Snapshot remains immutable.
5. Contract serialization round-trip test passes.

## ТЗ-05: authoritative command inbox, validation и Deferred attempts

Цель: заменить fire-and-forget queue на command lifecycle, соответствующий rules `Accepted/Pending/Deferred/Rejected/Failed`.

Область изменений:

- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`;
- optional internal command inbox/runtime records in Engine;
- `tests/DeepSpaceSaga.Engine.Tests/EngineCommandTests.cs`.

Требования:

- MUST выполнить acceptance validation before adding command to pending queue.
- MUST reject illegitimate commands immediately and not replace existing pending/deferred command.
- MUST keep at most one pending/deferred command per `(objectId, moduleId)` unless a later requirement explicitly changes this.
- MUST retry deferred commands on authoritative turns up to 3 attempts.
- MUST fail deferred command with `DeferredLimitExceeded` on the third unsuccessful attempt.
- MUST write corresponding `CommandResult` entries through the snapshot result pipeline from ТЗ-04.

Acceptance criteria:

1. Rejected command does not cancel existing valid pending command for the same module.
2. New valid command supersedes previous pending/deferred command for the same module and logs cancellation.
3. Deferred command retries exactly across authoritative turns and fails after attempt 3.
4. Tests cover invalid object, invalid module, invalid command type, busy module, supersession, and deferred limit.

## ТЗ-06: полный набор Engine commands первой итерации

Цель: привести command definitions, contracts, UI and Engine handling к минимальному набору Engine commands первой итерации.

Область изменений:

- `src/DeepSpaceSaga.Contracts/ShipEngineCommandTypes.cs`;
- `src/DeepSpaceSaga.Contracts/PlayerCommand.cs`;
- `src/DeepSpaceSaga.Client/Data/command-definitions.json`;
- `src/DeepSpaceSaga.Client/Data/module-types.json`;
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`;
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs`;
- Engine and Client tests.

Требования:

- MUST support `Accelerate`, `Brake`, `MaintainSpeed`, `TurnLeftStep`, `TurnRightStep`, `TurnLeftUntilCancel`, `TurnRightUntilCancel`, `MaintainCourse`, `MatchTargetSpeed`, `MatchTargetCourse`.
- MUST remove or migrate `engine.cancel-all`; it is not the canonical first-iteration command.
- MUST add explicit target parameter support for `MatchTargetSpeed` and `MatchTargetCourse` in `PlayerCommand` or an equivalent dependency-free DTO shape.
- MUST validate `targetObjectId` authoritatively in Engine.
- MUST keep UI selection separate from authoritative target: target must be sent explicitly.
- MUST preserve one `ActiveCycle` per Engine module.

Acceptance criteria:

1. JSON command definitions contain the canonical command set.
2. Engine rejects unknown legacy `engine.cancel-all`.
3. `MaintainSpeed` cancels acceleration/braking behavior.
4. `MaintainCourse` cancels until-cancel turn behavior.
5. `MatchTargetSpeed` changes scalar speed only.
6. `MatchTargetCourse` changes direction only.

## ТЗ-07: стартовый корабль из 3 платформ и 12 occupied cells

Цель: привести `DefaultScenario` and module type data к стартовому кораблю из секции 50.

Область изменений:

- `src/DeepSpaceSaga.Client/Data/module-types.json`;
- `src/DeepSpaceSaga.Client/Data/item-types.json` if needed;
- `src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json`;
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs`;
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`;
- scenario/content validation tests.

Требования:

- MUST define module types for Bridge/Navigation Computer, Engine, Generator, Battery, Container, Drilling Unit, Scanner, Habitation Module, Combat Laser.
- MUST configure the player ship as 3 connected platforms of 4 cells each.
- MUST fully occupy all 12 mounting cells.
- MUST keep Container as `SlotSize = 4` on platform 2.
- MUST keep active modules able to own `ActiveCycle`; passive modules must not.
- MUST validate no overlapping cells and exact `SlotSize` occupancy.

Acceptance criteria:

1. `DefaultScenario` player ship has exactly 9 installed modules.
2. Platform occupancy totals 12 cells and has no free mounting cell.
3. Scenario loader rejects overlap, negative cell, and wrong slot count.
4. Initial snapshot still identifies `PlayerShipObjectId`.
5. Engine tests pass for scenario load and module placement validation.

## ТЗ-08: fuel state for Engine module

Цель: реализовать fuel как отдельное runtime-состояние двигателя, не как cargo stack.

Область изменений:

- `src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs`;
- module type JSON schema/DTO in `EngineContentLoader`;
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs`;
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`;
- `src/DeepSpaceSaga.Client/Data/module-types.json`;
- `src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json`;
- Engine tests.

Требования:

- MUST add `FuelCapacityKg` to Engine module type definition.
- MUST add `FuelAmountKg` to installed Engine module runtime/scenario state.
- MUST store fuel as integer `Int64` kilograms.
- MUST initialize starter Engine with `Floor(FuelCapacityKg / 2)`.
- MUST ensure fuel is not stored in Container cargo and does not use `ItemType.UnitMassKg`.
- SHOULD defer exact fuel consumption formula until the open question at the end of section 56.10 is resolved.

Acceptance criteria:

1. Engine module type with missing/invalid `FuelCapacityKg` fails validation.
2. Installed Engine with `FuelAmountKg` outside `0..FuelCapacityKg` fails validation.
3. Starter Engine loads with half tank.
4. No cargo stack with fuel item is required for Engine commands.

## ТЗ-09: ActiveCycle duration, command factors and fixed-point normalization

Цель: remove hardcoded cycle durations and implement the factor model from section 56.3.

Область изменений:

- `src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs`;
- `src/DeepSpaceSaga.Engine/Content/CommandDefinition.cs`;
- `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs`;
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`;
- `src/DeepSpaceSaga.Client/Data/command-definitions.json`;
- `src/DeepSpaceSaga.Client/Data/module-types.json`;
- Engine content and command tests.

Требования:

- MUST add `BaseCycleTimeMs` to active module type definitions.
- MUST add command `TimeFactor`, `ComplexityFactor`, and `ConsumptionFactor` as readable decimal JSON.
- MUST normalize factors to fixed-point integer representation where `1000 = 1.0`.
- MUST compute `EffectiveCycleTimeMs = Ceil(BaseCycleTimeMs * TimeFactor)`.
- MUST create an `ActiveCycle` even when `EffectiveCycleTimeMs = 0`.
- MUST complete zero-duration cycles no earlier than the next authoritative tick/turn, not in the same processing pass.

Acceptance criteria:

1. `timeFactor: 1.2` normalizes to `1200`; `0.75` normalizes to `750`.
2. Floating-point is not used as authoritative runtime representation for factors.
3. Zero-duration command creates an `ActiveCycle` visible to runtime state before completion.
4. Existing Engine command behavior is covered by tests after replacing hardcoded durations.

## ТЗ-10: player-visible object labels and knowledge-safe label text

Цель: привести карту labels к секции 42 and player knowledge constraints.

Область изменений:

- `src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs` or dedicated render DTO metadata;
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`;
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs`;
- label tests in Client and Engine snapshot tests.

Требования:

- MUST provide enough dependency-free metadata for Client to choose player-visible label text without exposing forbidden factual fields.
- MUST show unknown object label as localized `Неизвестный объект`.
- MUST show known asteroid label as its `objectId`.
- MUST show station label as station name plus station ID.
- MUST show `PlayerShip` and `NpcShip` by name only, without `objectId`.
- MUST not show hidden `ObjectType`, mass, speed, direction, composition, or other factual attributes for unknown objects.

Acceptance criteria:

1. Station from `DefaultScenario` renders a label containing both `Start Station` and `SPC-0002`.
2. Player ship label contains `Player Ship` and not `SPC-0001`.
3. Unknown temporary asteroid label is `Неизвестный объект`.
4. Known asteroid label is its ID.
5. Tests cover renderer label choice without pixel-fragile assertions.

## ТЗ-11: player knowledge projection for marker color and factual visibility

Цель: отделить authoritative object facts от player-visible render projection.

Область изменений:

- Engine runtime knowledge model;
- snapshot/render metadata DTOs in Contracts;
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`;
- `src/DeepSpaceSaga.Client/UI/SpaceMapColorResolver.cs`;
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs`;
- Engine and Client tests.

Требования:

- MUST keep factual `ObjectType`, `MassKg`, `SpeedMps`, `DirectionDegrees`, and `CompositionType` authoritative in Engine.
- MUST expose only player-visible render type/knowledge state to Client UI.
- MUST render unknown objects with fallback color before successful `GeneralScan`.
- MUST keep movement data available for prediction without displaying it as player knowledge.
- MUST mark starter stations and permanent asteroids as initially known.
- MUST not use Skia/Silk types outside Client.

Acceptance criteria:

1. Temporary asteroid before scan renders as Unknown/fallback despite factual `Asteroid`.
2. Known station renders as Station/Orange.
3. GeneralScan success immediately changes render projection to revealed type.
4. Client cannot derive label text from hidden factual fields.
5. Contracts remain dependency-free.

## ТЗ-12: tactical map marker sizes and scale visibility filter

Цель: привести marker rendering к секции 39.

Область изменений:

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs`;
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelLayout.cs`;
- `src/DeepSpaceSaga.Client/UI/SpaceMapColorResolver.cs` if render type mapping changes;
- Client tests for marker sizing/filtering.

Требования:

- MUST render marker sizes in screen pixels, unaffected by zoom.
- MUST use 10 px marker size for UnknownSpaceObject, Asteroid, Station, NpcShip, PlayerShip.
- MUST use 25 px marker size for Planet.
- MUST use 50 px marker size for Sun.
- MUST apply scale visibility filter: on `Small/Combat` show Sun, Planet, Station, PlayerShip, NpcShip, Asteroid, UnknownSpaceObject.
- MUST apply scale visibility filter: on `Medium`, `Large`, `System` show only Sun, Planet, Station, PlayerShip.
- MUST keep hidden objects authoritative in Engine; filter is Client-side rendering only.

Acceptance criteria:

1. Marker radius/diameter tests prove 10/25/50 px screen-space sizes.
2. Zoom changes world projection but not marker pixel size.
3. Medium/Large/System scale hides Asteroid, UnknownSpaceObject, and NpcShip from rendering.
4. Hidden-by-scale objects remain present in latest snapshot and prediction buffer.
5. Label geometry safe radius uses the same marker-size policy as marker drawing.
