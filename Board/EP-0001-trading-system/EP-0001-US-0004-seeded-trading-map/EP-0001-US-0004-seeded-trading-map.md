---
epic: EP-0001-trading-system
story: EP-0001-US-0004-seeded-trading-map
title: Воспроизводимая карта с выбором торгового направления
stage: approved
dependencies: [EP-0001-US-0001-station-market-profiles]
created: 2026-09-21T09:16:09Z
source_request: "сделай следующую стори"
current_review: complete
revision: 2
---

# Воспроизводимая карта с выбором торгового направления

## Входное техническое задание

Точное сообщение пользователя: «сделай следующую стори». Время фиксации запроса при grounding: 2026-09-21T09:16:09Z (UTC; время получения сообщения недоступно).
Выбрана US-0004 после подготовленной US-0002 по порядку review в ../Documentation.md:158–160. Это assumption о выборе истории, а не новая зависимость от US-0002.

Исходный объём: Concept:149–161, 232; MVP:292–351, 392–396, 439–445. Полные относительные пути: Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md и TradingSystemMvpStories.md. Эпик: ../Documentation.md:77–100, 142. Правила masterSeed: Documentation/01-Requirements/EngineRequirements.md:314–350. Все пути и ссылки path:line ниже относятся к D:/DeepSpaceSaga/DSS; строки описывают снимок grounding, новые API явно обозначены как планируемые.

## User story

Как игрок, я хочу начинать игру на карте из пяти станций разных экономических ролей, чтобы выбирать торговое направление, а не следовать единственной цепочке. Стартовая транзитная станция входит в связную сеть с короткими, средними и дальними направлениями. Каждый производитель имеет потенциального потребителя, а минимум одна связь допускает полезную обратную загрузку. Одинаковый seed воспроизводит роли, связи и расположение; сохранение продолжает уже созданную карту. Default, Docked и Undocked используют одну модель сети с разным начальным состоянием корабля.

## Acceptance criteria

- AC-01: New Game для Default, Docked и Undocked материализует ровно пять известных неподвижных Station: по одной Transit, Mining, Industrial, Hydroponic, Scientific/Military. SPC-0002 остаётся стартовой Large; ship/loadout/начальная стыковка сохраняются.
- AC-02: Простой неориентированный экономический граф связен, degree каждого узла >=2, E−V+C>=2. Есть Mining–Industrial, Industrial–Hydroponic, Industrial–Scientific/Military. У каждого производителя есть смежный потребитель; хотя бы одна связь имеет непустые cargo flows в обе стороны. Transit не единственный покупатель.
- AC-03: У каждого ребра есть distanceKm, travelEstimateGameTimeMs, distanceClass, fuelMultiplierPermille, riskProfileId. Есть Short <=2 игровых часов, Medium >2–6, Long >6; старт имеет безопасное короткое направление, Science участвует в дальнем. Оценка по reference ship, пороги и параметры в scenario config.
- AC-04: Начальные координаты конечны, станции разделены minStationDistanceKm и не перекрывают начальные астероиды в пределах clearanceKm. Исключение — штатное соседство игрока со стартовой станцией. Approach к каждой станции строит конечный маршрут при положительной скорости; команда сохраняет скорость. Геометрия не ограничивает свободный полёт.
- AC-05: Одинаковые masterSeed и config дают одинаковые topology/roles/coordinates/edge metadata/cargo flows/RNG state, независимо от порядка JSON-элементов и других RNG-подсистем. New Game без seed использует штатный новый masterSeed. В наборе seeds есть несколько карт.
- AC-06: Save/Load сохраняет материализованную сеть и RNG без генерации, дублирования станций, сдвига координат или повторной инициализации рынков. Некорректные config/граф/ссылки/геометрия отвергаются до замены текущего мира. Старые сценарии без map fields сохраняют прежний путь.
- AC-07: Для одного seed три стартовых варианта имеют одинаковую сеть. Пять станций доходят до первого authoritative snapshot и видны существующими маркерами при подходящем zoom; контрольный save содержит классификацию/пары грузов. Default_500 остаётся прежним stress scenario (502 объекта). Точные удалённые цены не публикуются.

## Non-goals

Production-код не выполняется StoryBuilder. При реализации: нет нового экрана или рисования рёбер, API удалённого рынка, Undock/voyage lifecycle, изменения Approach/Motion, реального списания топлива, runtime рисков, генерации астероидных полей, динамических цен и доказательства положительной прибыли. Это US-0005/0006/0008/0009/0011/0013/0014/0016. Достижимость здесь означает связность и работоспособность Approach для движущегося корабля; нулевая скорость и docked остаются штатными ограничениями.

## Dependencies

Обязательна реализация US-0001: TK-0001-market-profile-schema, TK-0002-profile-market-bootstrap, TK-0003-market-catalog-content. Наличие approved planning artifacts не означает реализованный dependency. Их контракты: registry.StationMarketProfiles, SupplyItemTypeIds/DemandItemTypeIds, optional MarketProfileId/MarketProfileFingerprint, profile-aware bootstrap/save. US-0002 не является прямой зависимостью; если её additive schema уже есть, сохранить без изменений.

## Grounding

- Существующие scenario DTO: src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:28–73; GameStateData содержит seed, SpaceObjects и compatibility, торгового графа нет. ScenarioLoader.cs:27–30 запрещает неизвестные JSON-поля; :76–99 нормализует ссылки; :131–138 проверяет save/catalog version.
- Seed разрешается перед сборкой runtime; runtime полностью строится до замены мира: src/DeepSpaceSaga.Engine/SimulationEngine.cs:196–240. Save записывает materialized objects и seed: :756–820.
- Детерминированные имена/seed доступны через RngStreamSeedDerivation.DeriveStreamSeed(ulong, string) и RngStreamNames.CreateDeterministicRandom(ulong). Новый сохраняемый поток карты отсутствует; общий retrofit всех старых потоков не входит.
- Profile definition уже появился в рабочем дереве: src/DeepSpaceSaga.Engine/Content/StationMarketProfileDefinition.cs:9–18; registry property — GameDataRegistry.cs:60. Это параллельная работа, не завершённая US-0001.
- Три исходных сценария содержат по четыре объекта. Default/scenario.json:117–140 — Large SPC-0002 с explicit inventory, :143–166 — два астероида; Docked/Undocked хранят исходное состояние корабля. ScenarioEngineTests.cs:136–170 проверяет именно исходный JSON, поэтому четыре template-объекта остаются в нём, ещё четыре станции добавляет generator.
- EngineContentLoader.CreateEngineFromScenarioFile(string settingsPath, string scenarioPath) и CreateEngineFromSaveFile(string settingsPath, string savePath) уже различают New Game / Save; src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:29–50.
- Календарь и физическое время различаются: src/DeepSpaceSaga.Contracts/SimulationSpeed.cs:27–31; SimulationClock.cs:55–57. 1 world unit =100 m; Approach сохраняет скорость и не обходит препятствия: EngineRequirements.md:5337–5341.
- Snapshot уже переносит объекты. Существующая политика маркеров не скрывает типы: src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapMarkerPolicy.cs:5–8. Bootstrapping packaged scenarios покрывает tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs:436–472.

## Invariants and assumptions

- A-01: Следующая story = US-0004 по эпическому review order. План принимается автоматическим workflow без отдельного подтверждения.
- A-02: Экономические циклы — топологические циклы; направления cargo flow определяются пересечением supply/demand. Не обещаем потребителя для каждого отдельного товара: эпик требует потребителя у каждого производителя.
- A-03: Reference speed =700 m/s; travel estimate =ceil(distanceKm / 0.7 * 1000 * 300), в игровых ms, без манёвра/стыковки. Пороги 2/6 ч; max12 ч; min station separation5 km, asteroid clearance2 km. Это стартовые configurable balance assumptions, не изменения clocks или скорости игрока.
- A-04: SPC-0002 сохраняет (11000,11000), имя, explicit inventory и служебные данные. Новые станции SPC-0005..0008. Корабль и два астероида не сдвигаются. Вся случайность карты влияет на выбор одного из трёх шаблонов и его поворот вокруг стартовой станции.
- A-05: Координаты/видимость пяти станций доступны сейчас, рёбра/потоки/оценки доступны в контрольном save. Отдельный UI графа не требуется completion evidence; knowledge/prices отложены до US-0016.
- A-06: Optional gameState.tradingMapGeneration — запрос New Game; optional gameState.tradingMap — результат. Они взаимоисключающие. Save пишет только результат, с копией использованного config. schemaVersion=1 внутри map blocks; глобальную SaveFormat не понижать и не резервировать номер поверх US-0001/0002: добавление optional блока не требует несовместимой миграции по ScenarioData.cs:10–12.
- A-07: Clearance проверяется в начальный момент против астероидов и прочих станций, не против всех будущих орбит/траекторий. Approach не имеет obstacle avoidance; это не обещание коллизионной модели.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0004-TK-0001-trading-map-schema | Схема запроса и сохранённой карты | engine | US-0001/TK-0002 | AC-05, AC-06 |
| EP-0001-US-0004-TK-0002-economic-graph | Seeded граф и грузовые связи | engine | TK-0001; US-0001/TK-0001 | AC-02, AC-05 |
| EP-0001-US-0004-TK-0003-map-geometry | Геометрия и классы расстояний | engine | TK-0002 | AC-03, AC-04, AC-05 |
| EP-0001-US-0004-TK-0004-map-bootstrap-save | New Game и восстановление сети | engine | TK-0003; US-0001/TK-0002 | AC-01, AC-04, AC-05, AC-06 |
| EP-0001-US-0004-TK-0005-trading-scenario-content | Три стартовых варианта одной сети | content-data | TK-0004; US-0001/TK-0003 | AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07 |

Внутренний порядок: TK-0001 → TK-0002 → TK-0003 → TK-0004 → TK-0005. Лимиты implementation files: 4, 2, 2, 2, 4. Все engine tickets используют tests/DeepSpaceSaga.Engine.Tests, content-data — tests/DeepSpaceSaga.Client.Tests.

## Gaps and backlog

- G-01: Профильный bootstrap и runtime content US-0001 ещё не подтверждены завершёнными. Это явная implementation dependency; при её несовпадении с approved API вернуть тикет в review, не расширять scope.
- G-02: Transit только потребляет; protein-mass и silicon не образуют полную замкнутую цепь в профилях. Эта история проверяет существование направлений, не десятидневный баланс. Баланс и высокая маржа — US-0013/US-0011.
- G-03: Undock не реализуется здесь; физический рейс из Docked будет включён US-0014. Approach при SpeedKmS=0 по текущим правилам отклоняется.
- G-04: Полный каталог RNG-потоков старых подсистем отсутствует в GameStateData; здесь сохраняются только два новых map streams. Глобальный retrofit — backlog вне эпика.
- G-05: Skill requirements-engineer, упомянутый Documentation/00-Process/AGENTS.md, не найден среди доступных/local plugin skills. Применён обязательный DSS-StoryBuilder workflow; отсутствующая skill не нужна для выполнения этого формата.
- Блокирующих продуктовых вопросов нет. Начальные коэффициенты/геометрия записаны как assumptions; прибыль и риск не выдаются за проверенные.

## Decision and review log

- 2026-09-21T09:16:09Z — точное сообщение: «сделай следующую стори»; продолжение по существующим artifacts, выбран US-0004. Других технических решений пользователя в этом запросе нет.
- Grounding завершён: требования, эпик, история, dependencies, scenario/RNG/bootstrap/snapshot/test surface проверены; IDs пути/frontmatter совпадают. Чужие изменения кода и US-0002 сохранены.
- Plan-review: карта пяти тикетов принята автоматическим workflow. Не требуется ответ пользователя.
- Создан TK-0001: schema и structural checks; 4 разрешённых файла. Следующий TK-0002.
- Создан TK-0002: граф, cargo flows, независимый RNG; 2 разрешённых файла. Следующий TK-0003.
- 2026-09-22T19:26:16Z — точное сообщение пользователя: «создай недостающие»; workflow возобновлён по существующим artifacts.
- Создан TK-0003: geometry и edge metadata; 2 разрешённых файла. Следующий TK-0004.
- Создан TK-0004: New Game/bootstrap и Save/Load; 2 разрешённых файла. Следующий TK-0005.
- Создан TK-0005: Default/Docked/Undocked content и Client integration coverage; 4 разрешённых файла.
- Все пять тикетов созданы в dependency order; блокирующих вопросов нет; current_review переведён в complete.
