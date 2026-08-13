# 10 небольших технических заданий для DSS

Дата подготовки: 2026-08-09

Основание: `CLAUDE.md`, `deep_space_saga_engine_requirements.md`, `Docs/TacticalMapSpecification.md`, текущий код `src/` и тесты `tests/`.

Общие ограничения для всех заданий:

- `DeepSpaceSaga.Engine` остается чистой библиотекой без SkiaSharp/Silk.NET/UI-зависимостей.
- `DeepSpaceSaga.Contracts` остается dependency-free DTO/API слоем.
- Направление зависимостей сохраняется: `Client -> Engine.LocalClient -> Engine`, `Motion` используется для общей детерминированной математики движения.
- Snapshot DTO должны быть immutable и JSON-serializable.
- Тесты добавляются рядом с затронутой границей; сборка выполняется через `dotnet build DeepSpaceSaga.sln`.

## ТЗ-01: CommandResult в authoritative snapshot

Цель: сделать результат обработки player command видимым клиенту и тестам через следующий authoritative snapshot.

Источники и текущий код:

- Требования: секция 56.5-56.6 про `CommandResult`, `ShipEvent`, deterministic ordering и diagnostic outcome.
- Сейчас: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs` содержит только `Objects`; `src/DeepSpaceSaga.Engine/SimulationEngine.cs` возвращает внутренний `CommandStartDisposition`, но не публикует его наружу.

Область изменений:

- `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs`
- новый DTO в `src/DeepSpaceSaga.Contracts`
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`
- `tests/DeepSpaceSaga.Contracts.Tests`
- `tests/DeepSpaceSaga.Engine.Tests/EngineCommandTests.cs`

Требования:

- MUST добавить immutable DTO `CommandResult`.
- MUST включить `ImmutableArray<CommandResult> CommandResults` в `AuthoritativeSnapshot`.
- MUST публиковать результаты команд, обработанных с момента предыдущего snapshot.
- MUST различать минимум `Executed`, `Rejected`, `Deferred`, `Cancelled`, `Failed`.
- MUST включать machine-readable `ReasonCode` для неуспешных исходов.

Acceptance Criteria:

1. Валидная engine command появляется в следующем snapshot как `Executed`.
2. Команда с неверным `objectId`, `moduleId` или `commandType` появляется как `Rejected`.
3. Busy-module command появляется как `Deferred`, если она реально переотложена.
4. `CommandResults` не мутируется после создания snapshot.

Проверка:

- `dotnet test tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj`
- `dotnet test tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~EngineCommandTests"`
- `dotnet build DeepSpaceSaga.sln`

## ТЗ-02: минимальный ShipEvent / watch-log DTO в snapshot

Цель: заложить канал вахтенного журнала для событий корабля без реализации полноценного UI журнала.

Источники и текущий код:

- Требования: секция 56.6 требует логировать успешные, failed и interrupted command completions.
- Сейчас: `AuthoritativeSnapshot` не содержит `ShipEvents`, а `InterfaceLog` является client-side diagnostic логом.

Область изменений:

- `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs`
- новый DTO `ShipEvent` в `src/DeepSpaceSaga.Contracts`
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`
- `tests/DeepSpaceSaga.Contracts.Tests`
- `tests/DeepSpaceSaga.Engine.Tests/EngineCommandTests.cs`

Требования:

- MUST добавить immutable JSON-serializable `ShipEvent` с `eventId`, `objectId`, `moduleId`, `eventType`, `reasonCode`, `gameTimeMs`.
- MUST добавлять событие при завершении engine command.
- MUST добавлять событие при отмене/прерывании active cycle.
- SHOULD пока не строить клиентский UI журнала.

Acceptance Criteria:

1. Завершенный `TurnRightStep` создает `ShipEvent` с game time завершения.
2. Отмена auto-repeat command создает отдельное событие с machine-readable reason.
3. `ShipEvents` сериализуются вместе со snapshot без зависимостей от Client.

Проверка:

- `dotnet test tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj`
- `dotnet test tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~EngineCommandTests"`
- `dotnet build DeepSpaceSaga.sln`

## ТЗ-03: канонические Engine commands вместо `engine.cancel-all`

Цель: привести contract constants, JSON definitions и command panel к минимальному набору Engine commands из требований.

Источники и текущий код:

- Требования: секция 56.8 фиксирует `MaintainSpeed`, `MaintainCourse`, `MatchTargetSpeed`, `MatchTargetCourse`.
- Сейчас: `ShipEngineCommandTypes` и `command-definitions.json` содержат `engine.cancel-all`, но не содержат canonical maintain/match commands.

Область изменений:

- `src/DeepSpaceSaga.Contracts/ShipEngineCommandTypes.cs`
- `src/DeepSpaceSaga.Contracts/PlayerCommand.cs`
- `src/DeepSpaceSaga.Client/Data/command-definitions.json`
- `src/DeepSpaceSaga.Client/Data/module-types.json`
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs`
- `tests/DeepSpaceSaga.Contracts.Tests`
- `tests/DeepSpaceSaga.Client.Tests/GameSessionEngineCommandPanelTests.cs`

Требования:

- MUST добавить constants для `MaintainSpeed`, `MaintainCourse`, `MatchTargetSpeed`, `MatchTargetCourse`.
- MUST убрать `CancelAll` из первой итерации UI/JSON как canonical command.
- MUST не делать UI selection implicit authoritative target для match commands.
- MAY оставить внутреннюю backward-compatible обработку legacy `engine.cancel-all` только если она не видна в canonical data/UI.

Acceptance Criteria:

1. JSON command definitions содержит весь canonical набор из секции 56.8.
2. Панель двигателя больше не показывает `Cancel All`.
3. Contract smoke tests проверяют новые command constants.
4. Existing command-panel tests обновлены под новый набор кнопок.

Проверка:

- `dotnet test tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj`
- `dotnet test tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~GameSessionEngineCommandPanelTests"`
- `dotnet build DeepSpaceSaga.sln`

## ТЗ-04: authoritative параметры target для match commands

Цель: дать `MatchTargetSpeed` и `MatchTargetCourse` явный dependency-free способ передавать `targetObjectId`.

Источники и текущий код:

- Требования: секция 56.9 требует обязательный `targetObjectId`; UI selection не является implicit target.
- Сейчас: `PlayerCommand` содержит только `CommandId`, `ClientSequence`, `ObjectId`, `ModuleId`, `CommandType`.

Область изменений:

- `src/DeepSpaceSaga.Contracts/PlayerCommand.cs`
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`
- `tests/DeepSpaceSaga.Contracts.Tests`
- `tests/DeepSpaceSaga.Engine.Tests/EngineCommandTests.cs`

Требования:

- MUST расширить `PlayerCommand` dependency-free параметрами команды, достаточными для `targetObjectId`.
- MUST authoritatively reject match command без `targetObjectId`.
- MUST reject match command с неизвестной целью.
- MUST при старте active cycle captured target speed/course, чтобы завершение не зависело от дальнейшего изменения цели.

Acceptance Criteria:

1. `MatchTargetSpeed` без target публикует `Rejected`.
2. `MatchTargetCourse` с неизвестным target публикует `Rejected`.
3. При валидном target speed/course захватываются на старте.
4. Contract serialization round-trip сохраняет параметры команды.

Проверка:

- `dotnet test tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj`
- `dotnet test tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~EngineCommandTests"`
- `dotnet build DeepSpaceSaga.sln`

## ТЗ-05: загрузка `factory-types.json` и `recipes.json` из Settings

Цель: сделать все thematic JSON, объявленные в `Settings.json`, реально загружаемыми и строго валидируемыми.

Источники и текущий код:

- Требования: секция 10 требует загружать обязательные thematic JSON до старта сессии.
- Сейчас: `Settings.json` ссылается на `factoryTypes` и `recipes`, но `EngineContentLoader` загружает только modules/items/commands.

Область изменений:

- `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs`
- `src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs`
- новые минимальные definition records для factory/recipe, если нужны
- `tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs`

Требования:

- MUST загрузить `factoryTypes`, если путь присутствует в `Settings.json`.
- MUST загрузить `recipes`, если путь присутствует в `Settings.json`.
- MUST считать missing declared file startup error.
- MUST применять `JsonUnmappedMemberHandling.Disallow`.
- MUST разрешать пустые root arrays `factoryTypes: []` и `recipes: []`.

Acceptance Criteria:

1. Текущие пустые `factory-types.json` и `recipes.json` успешно загружаются.
2. Missing `factory-types.json` дает `ContentException`.
3. Лишнее поле в `recipes.json` дает `ContentException`.
4. Существующая загрузка modules/items/commands не меняет поведения.

Проверка:

- `dotnet test tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~ScenarioEngineTests"`
- `dotnet build DeepSpaceSaga.sln`

## ТЗ-06: factor model для command definitions

Цель: вынести длительность и модификаторы active commands из hardcoded логики в content data.

Источники и текущий код:

- Требования: секция 56.3 требует `BaseCycleTimeMs` у module type и `TimeFactor`, `ComplexityFactor`, `ConsumptionFactor` у command definition.
- Сейчас: `CommandDefinition` содержит только `TypeId`/`DisplayName`, а `SimulationEngine.CreateEngineCycle` hardcodes `0` или `1000` ms.

Область изменений:

- `src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs`
- `src/DeepSpaceSaga.Engine/Content/CommandDefinition.cs`
- `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs`
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`
- `src/DeepSpaceSaga.Client/Data/module-types.json`
- `src/DeepSpaceSaga.Client/Data/command-definitions.json`
- `tests/DeepSpaceSaga.Engine.Tests`

Требования:

- MUST добавить `BaseCycleTimeMs` для active module types.
- MUST добавить decimal-like JSON factors у command definitions.
- MUST нормализовать factors во fixed-point `1000 = 1.0`.
- MUST считать отсутствующий factor равным `1000`.
- MUST вычислять `EffectiveCycleTimeMs = Ceil(BaseCycleTimeMs * TimeFactor)`.
- MUST не использовать `double`/`float` как authoritative runtime representation factors.

Acceptance Criteria:

1. `timeFactor: 1.2` загружается как `1200`.
2. `complexityFactor: 0.75` загружается как `750`.
3. `EffectiveCycleTimeMs` берется из content data, а не из `CreateEngineCycle` hardcode.
4. Zero-duration cycle завершается не раньше следующего authoritative turn.

Проверка:

- `dotnet test tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~EngineCommandTests"`
- `dotnet build DeepSpaceSaga.sln`

## ТЗ-07: fuel state для Engine module

Цель: добавить fuel как runtime-состояние двигателя, не cargo item.

Источники и текущий код:

- Требования: секция 56.10 требует `FuelCapacityKg` в engine module type и `FuelAmountKg` в installed module runtime.
- Сейчас: `ModuleTypeDefinition` не содержит fuel fields; `ShipModuleData` хранит cargo, но fuel у двигателя отсутствует.

Область изменений:

- `src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs`
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs`
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`
- `src/DeepSpaceSaga.Client/Data/module-types.json`
- `src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json`
- `tests/DeepSpaceSaga.Engine.Tests`

Требования:

- MUST добавить `FuelCapacityKg` для Engine module type.
- MUST добавить `FuelAmountKg` для installed Engine module state.
- MUST хранить оба значения как integer kg, предпочтительно `long`.
- MUST валидировать `0 <= FuelAmountKg <= FuelCapacityKg`.
- MUST не хранить fuel в Container cargo и не использовать `ItemType.UnitMassKg`.
- SHOULD не реализовывать расход fuel до решения открытого вопроса секции 56.10.

Acceptance Criteria:

1. Engine module type без валидного `FuelCapacityKg` не загружается.
2. Installed Engine с fuel вне диапазона не загружается.
3. Default scenario содержит стартовый Engine с половиной бака.
4. Save/load сохраняет `FuelAmountKg`.

Проверка:

- `dotnet test tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~SaveLoadContinuityTests|FullyQualifiedName~ScenarioEngineTests"`
- `dotnet build DeepSpaceSaga.sln`

## ТЗ-08: стартовый корабль из 9 модулей и 12 occupied cells

Цель: привести Default scenario к первому gameplay focus по корабельным платформам и installed modules.

Источники и текущий код:

- Требования: секция 56.1 и существующее follow-up ТЗ по стартовому кораблю.
- Сейчас: `Default/scenario.json` содержит только Container и Engine; `module-types.json` содержит только два module types.

Область изменений:

- `src/DeepSpaceSaga.Client/Data/module-types.json`
- `src/DeepSpaceSaga.Client/Data/item-types.json`, если потребуется
- `src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json`
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`
- `tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs`

Требования:

- MUST добавить module types для Bridge/Navigation Computer, Engine, Generator, Battery, Container, Drilling Unit, Scanner, Habitation Module, Combat Laser.
- MUST настроить player ship как 3 platform indices.
- MUST занять ровно 12 mounting cells суммарно.
- MUST сохранить Container slot size 4 на platform 2.
- MUST валидировать overlap, duplicate cells и wrong slot count.

Acceptance Criteria:

1. Real default scenario грузится с 9 installed modules у `SPC-0001`.
2. Суммарно занято 12 cells, без пересечений внутри platform.
3. Active modules имеют command type ids только там, где требования это разрешают.
4. Existing scenario load tests обновлены под новый starter ship.

Проверка:

- `dotnet test tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~ScenarioEngineTests"`
- `dotnet build DeepSpaceSaga.sln`

## ТЗ-09: player-visible render projection для labels/colors

Цель: отделить authoritative facts от данных, которые клиенту разрешено показывать игроку.

Источники и текущий код:

- Требования: секции 3, 39, 42 и `Docs/TacticalMapSpecification.md`.
- Сейчас: `ObjectMotionSnapshot.ObjectType` передает фактический тип, `DisplayName` задан только для player ship, а unknown label в `ObjectLabelRenderer` равен `Unknown Celestial Object`.

Область изменений:

- `src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs`
- `src/DeepSpaceSaga.Engine/SimulationEngine.cs`
- `src/DeepSpaceSaga.Client/UI/SpaceMapColorResolver.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs`
- `tests/DeepSpaceSaga.Engine.Tests`
- `tests/DeepSpaceSaga.Client.Tests/ObjectLabelTests.cs`
- `tests/DeepSpaceSaga.Client.Tests/SpaceMapColorResolverTests.cs`

Требования:

- MUST не отдавать клиенту hidden factual `ObjectType` как основание для player-visible labels/colors неизвестных объектов.
- MUST добавить dependency-free render metadata, например `RenderObjectType` или `KnowledgeState`.
- MUST unknown object показывать как `Неизвестный объект`.
- MUST known station label строить как name + objectId.
- MUST player/npc ship label строить по name без objectId.
- MUST known asteroid label строить по objectId.

Acceptance Criteria:

1. Temporary asteroid с `IsKnown=false` получает fallback color и label `Неизвестный объект`.
2. Station `SPC-0002` с `IsKnown=true` показывает `Start Station` и `SPC-0002`.
3. Player ship показывает `Player Ship` без `SPC-0001`.
4. Client tests не зависят от pixel-fragile screenshots.

Проверка:

- `dotnet test tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj`
- `dotnet test tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~ObjectLabelTests|FullyQualifiedName~SpaceMapColorResolverTests"`
- `dotnet build DeepSpaceSaga.sln`

## ТЗ-10: screen-space marker sizes и scale visibility filter

Цель: привести tactical map markers к требованиям по размеру и видимости на масштабах.

Источники и текущий код:

- Требования: секция 39 про marker sizes и scale visibility.
- Сейчас: `GameSessionScreen.Draw` рисует non-player objects через `canvas.DrawCircle(..., 4, ...)`; label safe radius использует отдельные константы.

Область изменений:

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelLayout.cs`
- возможный новый `TacticalMapMarkerPolicy` в Client
- `tests/DeepSpaceSaga.Client.Tests`

Требования:

- MUST задавать marker sizes в screen pixels, без масштабирования zoom.
- MUST использовать 10 px для UnknownSpaceObject, Asteroid, Station, NpcShip, PlayerShip.
- MUST использовать 25 px для Planet.
- MUST использовать 50 px для Sun.
- MUST на Small/Combat показывать Sun, Planet, Station, PlayerShip, NpcShip, Asteroid, UnknownSpaceObject.
- MUST на Medium/Large/System показывать только Sun, Planet, Station, PlayerShip.
- MUST фильтровать только client-side rendering; snapshot/buffer объекты не удаляются.

Acceptance Criteria:

1. Тесты доказывают 10/25/50 px screen-space sizes независимо от zoom.
2. На M10/M100/M1000 Asteroid, UnknownSpaceObject и NpcShip не попадают в render list.
3. PlayerShip остается видим на всех scale buttons.
4. Label safe radius использует ту же marker policy, что и drawing.

Проверка:

- `dotnet test tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~GameSessionScalePanelTests|FullyQualifiedName~ObjectLabelTests"`
- `dotnet build DeepSpaceSaga.sln`
