# Deep Space Saga — структурированная модель требований

Источник: `deep_space_saga_engine_requirements.md`.

Назначение документа: пересобрать накопительный журнал вопросов и ответов в рабочую структуру требований. Исходный документ остаётся историческим источником решений; этот документ задаёт целевую раскладку по окнам, игровым механикам и сквозным техническим требованиям.

## Принципы структуризации

1. Решения группируются по предметной области, а не по хронологии обсуждения.
2. Каждый раздел должен быть пригоден для отдельного implementation task.
3. UI-окна описывают наблюдаемое поведение игрока и данные, которые им нужны.
4. Игровые механики описывают authoritative-правила мира и симуляции.
5. Технические требования описывают границы проектов, форматы данных, детерминизм, persistence, snapshot и тестируемость.
6. Старые номера разделов сохраняются как traceability, чтобы можно было проверить происхождение каждого требования.

## 1. Продуктовый контур и первый этап

### 1.1. Концепция игры

Содержит жанр, рамки первого этапа, карту Солнечной системы, прототипные ориентиры и принципы масштаба мира.

Источники:

- `1. Общая концепция первого этапа`
- `16. Статус этой контрольной точки`
- `36. Статус решений после v5`
- `53. Текущая точка продолжения`

### 1.2. Scope первой игровой итерации

Содержит границу первой реализуемой итерации: корабли, платформы, модули, команды, энергия, fuel, cargo, damage, save/load continuation. Производственные циклы станций, заводы и широкая экономика остаются future scope.

Источники:

- `55.10. Практический порядок внедрения`
- `56.1. Scope первой итерации`
- `56.7. Стартовые active modules`

### 1.3. Открытые решения

Содержит вопросы, которые нельзя тихо выбирать при реализации.

Источники:

- `51. Открытые вопросы для следующей сессии`
- `56.8. Engine commands первой итерации`
- `56.10. Engine fuel вместо Energy Cells`

Текущие открытые вопросы:

- Базовый расход топлива Engine задаётся как `BaseFuelConsumptionKgPerCycle` или как `FuelConsumptionKgPerSecond`.
- Может ли любая новая Engine command прерывать auto-repeat cycle, или только специальные cancel/replacement commands.

## 2. Окна и пользовательские сценарии

### 2.1. Main Menu

Назначение: стартовая точка приложения и запуск `New Game`.

Требования:

- `New Game` создаёт новую локальную игровую сессию.
- При `New Game` создаётся новый случайный `masterSeed`, чтобы разные новые игры давали разную процедурную генерацию.
- `masterSeed` сохраняется и загружается вместе с игрой.
- Переход из Main Menu в игровую сессию выполняется через экран игровой сессии.

Источники:

- `30. New Game и стартовый сценарий`
- `30.1. Стартовые временные астероиды текущего DefaultScenario`
- `15. Генераторы случайных чисел`

### 2.2. Game Session Screen

Назначение: основное окно игры с картой, камерой, world-to-screen projection, маркерами, орбитами, подписями, гридом и информационной панелью.

Состав экрана:

- tactical/world map;
- camera and zoom;
- object markers;
- object labels;
- orbital lines;
- adaptive world grid;
- selected/focused object context;
- game session information panel.

Источники:

- `19. Render architecture и 80 FPS`
- `20. Активная зона пяти экранов и отображение`
- `31.2. Камера и zoom`
- `39. Маркеры объектов на игровой карте`
- `40. Орбитальные линии`
- `41. Первая задача окна игровой сессии: адаптивный мировой грид`
- `42. Подписи объектов на карте`
- `54. GameSessionScreen — информационная панель игровой сессии`

### 2.3. Tactical Map

Назначение: визуализация мира в выбранном масштабе без синхронных запросов к Engine на каждом кадре.

Требования:

- Renderer работает на целевой частоте `80 FPS`.
- Renderer читает только client-side snapshot/prediction state.
- Marker sizes задаются в screen pixels и не зависят от zoom.
- Видимость объектов зависит от текущего масштаба.
- Unknown objects не раскрывают hidden authoritative facts через цвет, подпись или UI-метаданные.

Источники:

- `2. Координаты и масштабы`
- `19. Render architecture и 80 FPS`
- `22. Client-side motion prediction / dead reckoning`
- `23. Reconciliation и визуальное сглаживание`
- `39. Маркеры объектов на игровой карте`
- `39.1. Видимость по масштабу`
- `42. Подписи объектов на карте`

### 2.4. Game Menu и модальные окна

Назначение: overlay/navigation поверх игровой сессии.

Требования:

- Любое открытое модальное окно в single-player останавливает authoritative simulation.
- UI, rendering, transport и session infrastructure продолжают работать.
- При закрытии последнего модального окна восстанавливается предыдущая скорость, если игра не была на `Speed0` до открытия modal.
- Nested modal screens учитываются через modal depth.

Источники:

- `31. Скорость симуляции, Pause, модальные окна и камера`
- `31.1. Автоматическая пауза модальных окон`
- `52. Modal Pause Rule — обязательная остановка игрового цикла`

### 2.5. Module/Command UI

Назначение: отображение состояния корабельных модулей и отправка module-addressed commands.

Требования:

- UI отправляет команды с явным `(objectId, moduleId)`.
- UI не должен позволять запуск несовместимой команды на busy module.
- UI selection не является implicit authoritative target: target для `SpeedSynchronization` и `DirectionSynchronization` передаётся явно.
- Engine всегда выполняет authoritative validation независимо от UI.

Источники:

- `24. Команды игрока и секундный authoritative turn`
- `25. Module-addressed command model`
- `26. Command validation, supersession и конфликты`
- `56.2. Общая модель module lifecycle`
- `56.8. Engine commands первой итерации`
- `56.9. Match target commands`

## 3. Игровые механики мира

### 3.1. Координаты, масштаб и направления

Требования:

- Внутренняя мировая единица: `1 = 100 м`.
- Координаты хранятся как `double`.
- Пиксели используются только в отображении.
- Направление хранится как `DirectionDegrees` в диапазоне `0..359`.
- `0°` — вверх, `90°` — вправо, угол увеличивается по часовой стрелке.

Источники:

- `2. Координаты и масштабы`
- `5. Система направлений`
- `32. Целочисленные игровые величины и внешняя конфигурация`

### 3.2. Объекты мира

Требования:

- На первом этапе существуют `Sun`, `Planet`, `Asteroid`, `Station`, `PlayerShip`, `NpcShip`.
- `objectType` является authoritative-свойством Engine.
- `persistenceType` различает `Permanent` и `Temporary`.
- Неизвестность объекта для игрока не подменяет фактический `objectType`.

Источники:

- `3. Типы объектов`
- `14. Идентификаторы объектов`
- `33. Астероиды: классификация и знания игрока`
- `38. Знания игрока об объектах`

### 3.3. Орбитальное движение

Требования:

- По орбитам движутся планеты, постоянные астероиды и станции.
- Солнце неподвижно в `(0, 0)`.
- Орбита — геометрический эллипс вокруг Солнца без фокальной механики Кеплера.
- Текущая фаза вычисляется из `initialPhase`, `orbitalPeriodMs`, `orbitDirection` и `gameTimeMs`.
- `initialPhase`, если была сгенерирована, сохраняется и не генерируется повторно при load.

Источники:

- `6. Орбитальная модель`
- `40. Орбитальные линии`

### 3.4. Движение кораблей и синхронизация с объектом

Требования:

- NPC-корабли движутся по прямой к назначению.
- Корабль игрока может двигаться к назначению или менять курс вручную.
- При достижении объекта корабль синхронизируется с ним и получает скорость/направление объекта.
- Undock и новая цель снимают синхронизацию.
- При удалении объекта синхронизации корабль продолжает движение самостоятельно.

Источники:

- `7. Движение кораблей`
- `22. Client-side motion prediction / dead reckoning`
- `56.8. Engine commands первой итерации`
- `56.9. Match target commands`

### 3.5. Фокус карты и активные зоны

Требования:

- Фокус хранится как состояние сессии.
- В начале сессии фокус прикреплён к кораблю игрока.
- Фокус может быть свободным, сброшенным к кораблю или прикреплённым к объекту.
- Активные зоны строятся вокруг корабля игрока и фокуса.

Источники:

- `4. Фокус карты`
- `8. Временные малые астероиды`
- `9. Уровни частоты расчёта`
- `20. Активная зона пяти экранов и отображение`

### 3.6. Временные малые астероиды

Требования:

- Temporary asteroids существуют на масштабе Small / Combat.
- Создаются возле активных областей.
- Движутся по прямой без орбиты.
- Удаляются, когда находятся вне активных зон.
- Текущий `DefaultScenario` содержит два фиксированных стартовых temporary asteroids, но процедурная генерация должна опираться на session `masterSeed`.

Источники:

- `8. Временные малые астероиды`
- `30.1. Стартовые временные астероиды текущего DefaultScenario`
- `37. Масса малых астероидов`
- `15. Генераторы случайных чисел`
- `implementation_tasks_diffs_03_12.md`, `ТЗ-02A`

### 3.7. Знания игрока и сканирование

Требования:

- Authoritative facts объекта остаются в Engine.
- Client получает только player-visible projection.
- `GeneralScan` раскрывает общий тип/класс объекта.
- `StructuralScan` раскрывает структуру постоянных астероидов.
- Unknown object в UI отображается как `UnknownSpaceObject` / `Неизвестный объект`.

Источники:

- `33. Астероиды: классификация и знания игрока`
- `35. Scanner MK I`
- `38. Знания игрока об объектах`
- `42. Подписи объектов на карте`

## 4. Корабль, модули и ресурсы

### 4.1. Корпус корабля (hull grid)

Требования:

- Корабль (ship-объект) имеет `hullLayout`: `Width`, `Height` и список structural cells `{x, y}`. Понятия platform в этой модели нет.
- Module занимает список координат `occupiedCells: [{x, y}]`, каждая из которых обязана входить в `hullLayout.Cells` того же объекта.
- Координаты модулей одного объекта не пересекаются между собой; `occupiedCells.Count` равен `ModuleType.SlotSize`.
- Если у объекта есть modules, `hullLayout` обязателен; иначе — ошибка загрузки сценария.
- Разрушение module влияет на его собственный `StructurePoints`; последствия для несущей конструкции корпуса при hull-grid модели формализованы разделом 57 частично (см. `45. Попадания и распределение урона по кораблю` — требует отдельного пересмотра).

Источники:

- `57. Tetrarch Class — замена стартового корабля и hull grid`
- `44. Платформенная конструкция корабля` (отменён разделом 57, сохранён как исторический контекст)
- `50. Стартовый корабль игрока` (заменён разделом 57)
- `55.5. Корабельные модули как ECS-композиция`

### 4.2. Общая модель модуля

Требования:

- Installed module имеет `moduleId`, `moduleTypeId`, placement, `PowerState`, `OperationalState`, `StructurePoints`.
- Active modules могут иметь `ActiveCycle`.
- Passive modules не имеют `ActiveCycle`, если это не задано будущими требованиями.
- Один active module выполняет не более одного `ActiveCycle` одновременно.

Источники:

- `34. Общая модель циклических модулей и Structure Points`
- `43. Общая модель корабельного модуля`
- `56.2. Общая модель module lifecycle`

### 4.3. Стартовый корабль (Tetrarch Class)

Требования:

- Стартовый `PlayerShip` (SPC-0001) — Tetrarch Class: hull grid `9×9`, 10 structural cells.
- Ровно 6 стартовых модулей, каждый на своей координате: Navigation Computer `(4,0)`, Living quarters `(4,1)` (`living.quarters.mk1`), Cargo hold `(4,2)` (`module.container.basic`), Scanner `(4,3)`, Reactor/Generator `(4,4)`, Engine `(4,5)`.
- 4 из 10 hull cells остаются незанятыми модулями.
- Battery, Drilling Unit, Combat Laser и старый Habitation Module в стартовом loadout отсутствуют (типы модулей остаются в каталоге).
- Container (`module.container.basic`) имеет `SlotSize = 1` (изменено с `4`).
- Cargo hold стартует с 1000 `Energy Cells`; Engine стартует без явно заданного `fuelAmountKg` (используется полный `fuelCapacityKg`).

Источники:

- `57. Tetrarch Class — замена стартового корабля и hull grid`
- `49. Обязательный Command Module / Bridge`
- `50. Стартовый корабль игрока` (заменён разделом 57)
- `56.7. Стартовые active modules`

### 4.4. Commands и ActiveCycle

Требования:

- Команды адресуются конкретному module через `(objectId, moduleId)`.
- `BaseCycleTimeMs` принадлежит `ModuleType`.
- `TimeFactor`, `ComplexityFactor`, `ConsumptionFactor` нормализуются во fixed-point representation.
- Success roll выполняется при завершении `ActiveCycle`.
- Для success roll используется persisted RNG stream `RngStream.ModuleCommandResolution`.
- Завершающиеся cycles обрабатываются в stable order `objectId -> moduleId -> activeCycleId`.

Источники:

- `24. Команды игрока и секундный authoritative turn`
- `25. Module-addressed command model`
- `26. Command validation, supersession и конфликты`
- `27. Command results, ship events и вахтенный журнал`
- `56.3. ModuleType cycle и command factors`
- `56.5. Success chance и command outcome`
- `56.6. ActiveCycle identity, save/load и logging`

### 4.5. Engine commands

Требования:

- Engine является active module.
- Двигатель выполняет только один active command одновременно.
- Минимальный набор команд: `Accelerate`, `Brake`, `MaintainSpeed`, `TurnLeftStep`, `TurnRightStep`, `TurnLeftUntilCancel`, `TurnRightUntilCancel`, `MaintainCourse`, `SpeedSynchronization`, `DirectionSynchronization`.
- `SpeedSynchronization` и `DirectionSynchronization` требуют явный `targetObjectId`.

Источники:

- `56.8. Engine commands первой итерации`
- `56.9. Match target commands`

### 4.6. Damage и Structure Points

Требования:

- Modules имеют `StructurePoints`.
- Damage распределяется через platform и module selection rules.
- Overflow damage и пустые platforms обрабатываются детерминированно.
- Damage events пишутся в watch log / ship events.

Источники:

- `34.1. Structure Points`
- `34.2. События повреждения в вахтенном журнале`
- `43.3. Structure Points`
- `45. Попадания и распределение урона по кораблю`

### 4.7. Energy, Battery, Generator и Fuel

Требования:

- Energy system учитывает generation, consumption, battery storage и emergency shutdown.
- `Energy Cells` являются fuel resource для Generator.
- Engine module не расходует `Energy Cells`; он использует fuel в килограммах.
- Engine fuel не является cargo stack.
- Starter Engine начинается с половиной бака.

Источники:

- `46. Грузовые модули, топливные ёмкости и вместимость`
- `47. Энергетическая система корабля`
- `48. Generator module и Energy Cells`
- `56.4. Activation cost и resource validation`
- `56.10. Engine fuel вместо Energy Cells`

## 5. Authoritative simulation requirements

### 5.1. Authoritative GameState

Требования:

- Доменная структура мира общая для стартового сценария, runtime state, save и snapshot projection.
- Engine хранит типизированное in-memory authoritative state.
- JSON не сериализуется на каждый tick.
- Snapshot и save являются производными от authoritative state.

Источники:

- `10. Authoritative GameState, General JSON и Settings.json`
- `28. AuthoritativeSnapshot и точка продолжения`
- `55.4. Blueprint JSON и instance/snapshot JSON`

### 5.2. Simulation clock, turns and frequencies

Требования:

- Baseline текущего кода: authoritative snapshot loop `1 Hz` без обязательного внутреннего `100 ms` тика.
- Game time не двигается на `Speed0`.
- Modal pause останавливает authoritative simulation, но не UI/transport.
- Уровни расчёта объектов зависят от расстояния до player ship или focus.

Источники:

- `9. Уровни частоты расчёта`
- `21. Authoritative turn и частоты симуляции`
- `31. Скорость симуляции, Pause, модальные окна и камера`
- `52. Modal Pause Rule — обязательная остановка игрового цикла`

### 5.3. Commands lifecycle and validation

Требования:

- Acceptance validation выполняется при приёме команды.
- Повторная validation выполняется на границе authoritative turn.
- Deferred command имеет максимум три попытки.
- Invalid command не должна тихо заменять valid pending command.
- Results команд публикуются через snapshot pipeline.

Источники:

- `24. Команды игрока и секундный authoritative turn`
- `26. Command validation, supersession и конфликты`
- `27. Command results, ship events и вахтенный журнал`
- `56.6. ActiveCycle identity, save/load и logging`

### 5.4. Randomness and determinism

Требования:

- В сессии есть один immutable `masterSeed`.
- `New Game` создаёт новый случайный `masterSeed`.
- `masterSeed` сохраняется и загружается вместе с игрой.
- Подсистемы используют named persisted RNG streams.
- RNG simulation events не выполняются во время modal pause / `Speed0`.

Источники:

- `15. Генераторы случайных чисел`
- `30. New Game и стартовый сценарий`
- `52.1. Что именно останавливается`
- `56.5. Success chance и command outcome`

## 6. Data, content and persistence requirements

### 6.1. Settings and thematic JSON

Требования:

- `Settings.json` является главным входным конфигурационным файлом.
- Balance/configuration data выносится в thematic JSON.
- Loader строит immutable in-memory configuration до `LoadScenario`.
- Неизвестные поля в content JSON должны отклоняться там, где это является частью схемы.

Источники:

- `10. Authoritative GameState, General JSON и Settings.json`
- `32.2. Конфигурационные JSON`
- `55.3. Type registry и configuration loading`
- `implementation_tasks_diffs_03_12.md`, `ТЗ-03`

### 6.2. Scenario, save and load

Требования:

- `DefaultScenario` задаёт стартовый мир.
- General Save State является authoritative форматом сохранения/восстановления.
- Save/load сохраняет GameState, RNG state, ID counters, runtime calculation state и `masterSeed`.
- ActiveCycle ids и allocator/counters сохраняются, чтобы после load не было повторов.

Источники:

- `10. Authoritative GameState, General JSON и Settings.json`
- `11. Сохранение`
- `28. AuthoritativeSnapshot и точка продолжения`
- `30. New Game и стартовый сценарий`
- `56.6. ActiveCycle identity, save/load и logging`

### 6.3. Identifiers

Требования:

- Object ids, module ids, activeCycle ids и counters должны быть стабильными.
- Id generation должна быть детерминированной в пределах save/load continuation.
- Historical ids не переписываются при изменении config name/module kind.

Источники:

- `14. Идентификаторы объектов`
- `56.6. ActiveCycle identity, save/load и logging`

### 6.4. Snapshot contract

Требования:

- `AuthoritativeSnapshot` immutable и JSON-serializable.
- Snapshot включает `SnapshotSequence`, `GameTimeMs`, render-relevant projection, command results и ship events.
- Snapshot не должен раскрывать forbidden player knowledge.
- Client prediction использует snapshot как вход, не меняя authoritative state.

Источники:

- `18. Архитектурная граница Client / Engine`
- `22. Client-side motion prediction / dead reckoning`
- `27.1. Результаты команд в snapshot`
- `28. AuthoritativeSnapshot и точка продолжения`

## 7. Architecture requirements

### 7.1. Project boundaries

Требования:

- `DeepSpaceSaga.Engine` остаётся graphics-free.
- Dependency direction: `Client -> Engine.LocalClient -> Engine`.
- `Contracts` остаётся dependency-free and DTO-focused.
- `DeepSpaceSaga.Motion` содержит общую deterministic motion math.
- Client не ссылается напрямую на Engine.

Источники:

- `17. Раздельная архитектура движка и отрисовки`
- `18. Архитектурная граница Client / Engine`
- `29. Синхронизация с репозиторием DSS и PR #3`
- `55.9. ECS-библиотека и граница зависимости`

### 7.2. Session boundary

Требования:

- Взаимодействие Client/Engine идёт через async/message-oriented `IGameSessionConnection`.
- Same shape должен подходить для local и future network session.
- Render loop не вызывает Engine синхронно per frame.
- `SetSimulationSpeedAsync` является session-control API для pause/speed.

Источники:

- `18. Архитектурная граница Client / Engine`
- `29.2. Асинхронная граница игровой сессии`
- `29.9. Lifecycle local connection`
- `52.4. Session-control API`

### 7.3. Client rendering and prediction

Требования:

- `SnapshotBuffer` публикует snapshot atomically.
- Client-side prediction является визуальной и не мутирует authoritative state.
- At `Speed0`, prediction delta is zero.
- Reconciliation сглаживает визуальные расхождения.

Источники:

- `19. Render architecture и 80 FPS`
- `22. Client-side motion prediction / dead reckoning`
- `23. Reconciliation и визуальное сглаживание`
- `29.5. Immutable publication и клиентский buffer`
- `29.7. Client-side prediction`
- `52.9. Client prediction во время Pause`

### 7.4. ECS boundary

Требования:

- ECS является внутренним Engine implementation detail.
- Contracts/Client не получают ECS internals.
- Immutable type/config data хранится в registry/configuration, не как ECS component.
- Client-only presentation state не кладётся в Engine ECS.
- Deterministic order обязателен для systems, save/load и command resolution.

Источники:

- `55. ECS-архитектурный слой Engine для модулей, заводов и кораблей`
- `55.11. Что не класть в ECS и запрещённые антипаттерны`

## 8. Diagnostics, logging and tests

### 8.1. Logging and diagnostics

Требования:

- Critical errors and diagnostics должны быть machine-readable там, где это влияет на обработку.
- Command results and ship events попадают в snapshot pipeline.
- Watch log отображает gameplay-significant events корабля игрока.
- Failed/interrupted command completions тоже диагностируются.

Источники:

- `12. Журналирование`
- `13. Критические ошибки и диагностика`
- `27. Command results, ship events и вахтенный журнал`
- `34.2. События повреждения в вахтенном журнале`
- `56.6. ActiveCycle identity, save/load и logging`

### 8.2. Acceptance tests by domain

Требования к тестированию должны быть сгруппированы по доменам:

- architecture boundary tests;
- content/schema loading tests;
- scenario/save/load continuation tests;
- deterministic motion and RNG tests;
- command lifecycle tests;
- module placement and damage tests;
- energy/fuel tests;
- client rendering geometry tests;
- modal pause and prediction tests.

Источники:

- `29.10. Проверки архитектуры`
- `41.8. Acceptance criteria первой задачи окна игровой сессии`
- `52.11. Обязательные тестовые сценарии`
- `54.7. Acceptance criteria`
- `implementation_tasks_diffs_03_12.md`

## 9. Рекомендуемая новая структура Notion/Markdown

```text
DSS Requirements
├── 00. Product Scope
│   ├── Concept and First Stage
│   ├── First Iteration Scope
│   └── Open Questions
├── 10. Screens and User Flows
│   ├── Main Menu and New Game
│   ├── Game Session Screen
│   ├── Tactical Map
│   ├── Game Menu and Modal Pause
│   └── Module Command UI
├── 20. World Mechanics
│   ├── Coordinates, Scale and Direction
│   ├── World Objects and Identity
│   ├── Orbital Motion
│   ├── Ship Motion and Docking
│   ├── Map Focus and Active Zones
│   ├── Temporary Asteroids
│   └── Player Knowledge and Scanning
├── 30. Ship, Modules and Resources
│   ├── Platforms
│   ├── Module Lifecycle
│   ├── Starter Ship
│   ├── Commands and ActiveCycle
│   ├── Engine Commands
│   ├── Damage and Structure Points
│   └── Energy, Battery, Generator and Fuel
├── 40. Authoritative Simulation
│   ├── GameState
│   ├── Simulation Clock and Frequencies
│   ├── Command Lifecycle and Validation
│   └── Randomness and Determinism
├── 50. Data, Content and Persistence
│   ├── Settings and Thematic JSON
│   ├── Scenario, Save and Load
│   ├── Identifiers
│   └── Snapshot Contract
├── 60. Architecture
│   ├── Project Boundaries
│   ├── Session Boundary
│   ├── Client Rendering and Prediction
│   └── ECS Boundary
├── 70. Diagnostics and Tests
│   ├── Logging and Diagnostics
│   └── Acceptance Tests by Domain
└── 90. Implementation Tasks
    └── Diffs 02A, 03-12
```

## 10. Миграционная карта старых разделов

| Старый раздел | Новое место |
| --- | --- |
| 1, 16, 36, 53 | `00. Product Scope / Concept and First Stage` |
| 30, 30.1 | `10. Screens and User Flows / Main Menu and New Game` |
| 19, 20, 31.2, 39, 40, 41, 42, 54 | `10. Screens and User Flows / Game Session Screen` and `Tactical Map` |
| 31, 31.1, 52 | `10. Screens and User Flows / Game Menu and Modal Pause` |
| 24, 25, 26, 56.8, 56.9 | `10. Screens and User Flows / Module Command UI` and `30. Ship, Modules and Resources / Commands` |
| 2, 5, 32 | `20. World Mechanics / Coordinates, Scale and Direction` |
| 3, 14, 33, 38 | `20. World Mechanics / World Objects and Identity` and `Player Knowledge and Scanning` |
| 6, 40 | `20. World Mechanics / Orbital Motion` |
| 7, 22, 56.8, 56.9 | `20. World Mechanics / Ship Motion and Docking` |
| 4, 8, 9, 20 | `20. World Mechanics / Map Focus and Active Zones` and `Temporary Asteroids` |
| 35, 38, 42 | `20. World Mechanics / Player Knowledge and Scanning` |
| 43, 44, 49, 50, 55.5, 56.2, 56.7 | `30. Ship, Modules and Resources` |
| 34, 45 | `30. Ship, Modules and Resources / Damage and Structure Points` |
| 46, 47, 48, 56.4, 56.10 | `30. Ship, Modules and Resources / Energy, Battery, Generator and Fuel` |
| 10, 11, 15, 21, 28, 52 | `40. Authoritative Simulation` and `50. Data, Content and Persistence` |
| 17, 18, 29, 55 | `60. Architecture` |
| 12, 13, 27, 34.2, 56.6 | `70. Diagnostics and Tests / Logging and Diagnostics` |
| `implementation_tasks_diffs_03_12.md` | `90. Implementation Tasks / Diffs 02A, 03-12` |

## 11. Что делать с исходным документом

Рекомендуемый порядок:

1. Оставить `deep_space_saga_engine_requirements.md` как historical decision log до завершения переноса.
2. Использовать этот структурный документ как новую карту Notion/Markdown.
3. Переносить текст по одному домену, начиная с `Screens and User Flows` и `Authoritative Simulation`, потому что они чаще всего используются в implementation tasks.
4. После переноса каждого домена добавить traceability block `Moved from sections: ...`.
5. После полного переноса сделать старый файл read-only архивом или переименовать в `deep_space_saga_engine_decision_log.md`.
