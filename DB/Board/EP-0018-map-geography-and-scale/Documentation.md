---
epic: EP-0018-map-geography-and-scale
title: "Переделка географии станций и масштаба карты"
stage: approved
approval_kind: delegated-planning
implementation_status: implemented_pending_acceptance
created: 2026-11-07T17:21:08Z
source_request: "отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами"
current_review: complete
dependencies: [EP-0002-procedural-solar-system, EP-0003-station-clusters-and-trade-geography, EP-0004-ai-territories-and-map-environment]
---
# Переделка географии станций и масштаба карты

## Статус и полномочия
9 историй / 29 тикетов. Функциональная реализация v2, automated corpus и raster/native
evidence поставлены; текущие контракты обновлены. Финальные navigation/graph/publication
оформляются документационной историей. Продуктовая приёмка OPEN: human/physical/baseline,
24cluster-fit detail gaps,3native p99 verdict FAIL и pre-existing Motion source guard.
Точные статусы, SHA и evidence — [ImplementationStatus](ImplementationStatus.md), полный
inventory — [DocumentationImpact](DocumentationImpact.md). stage approved сохраняет
историческую делегацию planning и не означает человеческую приёмку реализации.

Пользователь отменил последовательные подтверждения точным поручением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Дата 2026-11-07T17:21:08Z — UTC-время записи поручения; точное время исходного сообщения недоступно. Поля epic_approval с вымышленной фразой «утверждаю эпик» не создаются.

## Входное техническое задание
Предшествующее решение пользователя: «Давай вариант 3. Станции должны быть в игровых днях пути друг от друга, включая старые станции Default. На максимальном приближении — только локальная окрестность. Подготовь план переделки.»

Вариант 3 объединяет единые реальные расстояния между станциями, локальный максимум приближения и детализацию одной карты при отдалении. Новый запрос требует материализовать весь эпик со всеми историями и тикетами, без промежуточного review.

## Инструкции и продуктовый материал
Сообщения пользователя задают объём и процесс. Скриншот Default — иллюстрация проблемы читаемости, а не доказательство типа каждого объекта или точного zoom. Старые Board и комментарии кода — grounding, не новые инструкции и не доказательство актуальной приёмки. Канонические задачи создаются только в DB/Board.

## Grounding — фактическое состояние на момент планирования
Checkout: codex/ep-0016-tactical-battle-skins, исходно проверенный HEAD c43e8af5; перед реализацией перепроверить. Пути ниже от корня D:/DeepSpaceSaga/DSS.

| Факт | Доказательство |
|---|---|
| Мир создаётся последовательно: legacy торговая геометрия/ресурсы, solar, clusters/resources, AI, environment, POI; Load не генерирует solar заново. | src/DeepSpaceSaga.Engine/SimulationEngine.cs:253,290–315; MaterializeOrRestoreTradingMap(ScenarioFile, GameStateData, bool, ulong) |
| Старые станции копируются с прежними координатами; новый placement добавляет станции вдоль дуги; для preserved пар есть исключение при построении связей. | src/DeepSpaceSaga.Engine/Scenario/StationClusterGenerator.cs:122–192 |
| Дополнительные кластеры размещены в радиальных полосах того же стартового пояса. | StationClusterGenerator.Expand(ClusterGenerationResult, ClusterGenerationConfig, GameDataRegistry, ulong) |
| Старый trading map проверяет seed/template/геометрию при загрузке. Нельзя переставить станции, оставив противоречащие descriptors. | TradingMapGeometryGenerator.ValidateMaterialized(TradingMapStateData, IReadOnlyList<SpaceObjectData>, ulong, GameDataRegistry); SimulationEngine.cs:535 |
| Ресурсные астероиды принадлежат станции и получают общую угловую скорость при BindResources. | StationClusterGenerator.BindResources(ClusterGenerationResult); StationResourceFields.ExtendForClusters(GameStateData, ulong, StationResourceFieldConfig, GameDataRegistry) |
| Конфигурация и мир принимают только generatorVersion=1; cluster map rulesVersion=1. | SolarSystemGeneration.ValidateConfig; SolarSystemGeneration.ValidateWorld; StationClusterSaveValidation.Validate |
| В content 3–5 кластеров по 10–12 станций; neighbor 0.5–2d, diameter 3–7d, intercluster 15–35d. | src/DeepSpaceSaga.Client/Data/Maps/solar-system.json:1 |
| Максимум приближения задан 100 м/px, не временем пути. Группировка объектов — экранные ячейки, не доменное членство. | src/DeepSpaceSaga.Client/Settings.json:28; TacticalMapSettings.MaximumPpu; GameSessionScreen.MapView.cs:119 |
| Орбиты всех map.Orbits рисуются при showOrbits, включая малые объекты. | src/DeepSpaceSaga.Client/UI/Screens/GameSession/SolarSystemLayerRenderer.cs:8 |
| Время орбит абсолютное, период календарный; 1wu=100м, calendar/motion=300. | OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long); DB/Documentation/01-Requirements/EngineRequirements.md:20–40 |

Канонические предшественники: [EP-0002](../EP-0002-procedural-solar-system/Documentation.md), [EP-0003](../EP-0003-station-clusters-and-trade-geography/Documentation.md), [EP-0004](../EP-0004-ai-territories-and-map-environment/Documentation.md).
Основные области: EP-0002 US-0001/0004/0006/0007/0008; EP-0003 US-0001/0002/0003/0004/0005/0007/0008; EP-0004 US-0003/0006/0007/0008. Исторические stages/приёмка этих эпиков не переобъявляются.

## Решения пользователя
- D-01: выбран вариант 3: реальная география + локальное приближение + детализация карты.
- D-02: старые станции Default подчиняются тем же расстояниям, что новые.
- D-03: максимальное приближение показывает только локальное окружение.
- D-04: весь состав эпика/историй/тикетов создаётся без дополнительных подтверждений.
- D-05: предшествующий план получил «отлично»; это поддерживает направление, но не превращает все предложенные технические значения в дословные решения пользователя.

## Технические assumptions
Полный проверяемый контракт: [ImplementationContract.md](ImplementationContract.md).
- A-01: для всех пар человеческих станций минимум 1 календарный день по frozen generation Vmax; nearest внутри кластера 1–3 дня. AI bases имеют отдельную модель и не становятся человеческими торговыми станциями.
- A-02: фиксированная оценка генерации отличается от текущего ETA и времени полного Dock.
- A-03: новая circular rigid формация: 10–12 станций, diameter 10–16 дней; beltWidthFraction=0.20; центр соседнего кластера на старте 30–50 дней. Это исходные content значения для доказуемой укладки, а не измеренный баланс.
- A-04: отдельные радиальные полосы обеспечивают минимум между человеческими кластерами при любых фазах; соседство 1–3 дня относится к своему кластеру.
- A-05: сохраняем 100 м/px и текущие единицы. Локальный экран ограничиваем также охватом короткой стороны <=4 часов при опорной скорости; 1 день до станции не попадает в локальный viewport при установленных aspect limits. Контракт для широких окон уточнён в ImplementationContract.
- A-06: уровни Local/Region/Cluster/System — presentation policy в Client; gameplay неизменен при zoom.
- A-07: v2 New Game, материализованный v1 Load без перегенерации; Save20 не повышается автоматически.
- A-08: ресурсы остаются локальными около своей станции; их не нужно разносить на дни.
- A-09: v2 включается в shipped content только после save/load и сценарных интеграционных проверок. До этого — явный v2 config в тестах.

## Критерии эпика
- E18-AC-01: все человеческие станции, включая исходные Default, удовлетворяют min 1d; каждый участник имеет соседа своего кластера 1–3d.
- E18-AC-02: взаимные расстояния внутри кластера постоянны; радиальные полосы сохраняют глобальный min при любых фазах.
- E18-AC-03: роли/ID/явный inventory/ссылки сохраняются; ресурсы у владельцев, Docked/Undocked корректны, фактический старт 50–75d от Солнца.
- E18-AC-04: сохранения v1 и v2 загружаются без генерации; unknown versions/некорректный v2 отвергаются атомарно.
- E18-AC-05: торговая сеть двунаправленно достижима, текущие оценки/топливо согласованы с геометрией, AI placement остаётся bounded и trade-compatible.
- E18-AC-06: Local показывает окрестность; max zoom, resize, UI scale и старые settings не показывают станции за день пути в одном локальном viewport.
- E18-AC-07: Region/Cluster/System читаемы; видимы доменные кластеры; ресурсы и орбиты не образуют нечитаемую полосу; выбор/навигация сохраняются.
- E18-AC-08: детерминированность, corpus, rendered/native evidence воспроизводимы; заявленные latency/FPS имеют измерения, human acceptance явно отдельно.
- E18-AC-09: актуальные контракты и Board согласованы с фактической поставкой, а прежние результаты сохранены как история.

## Границы
Нет N-body, иной скорости времени, warp, переработки тактической боевой камеры, AI combat, новой добычи, remote торговли, автоматической миграции координат старых миров или общего ребаланса цен. Отображение уже известных объектов не раскрывает состав неизвестных астероидов. Количество планет/поясов и стартовая дистанция до Солнца сохраняются.

## Порядок историй и зависимости
| ID | История | Зависимости | Тикеты |
|---|---|---|---|
| [US-0001](EP-0018-US-0001-unified-station-geography/EP-0018-US-0001-unified-station-geography.md) | Все станции стартового кластера разделены игровыми днями | none | 4 |
| [US-0002](EP-0018-US-0002-stable-orbital-clusters/EP-0018-US-0002-stable-orbital-clusters.md) | Кластеры остаются разнесёнными при орбитальном движении | US-0001 | 3 |
| [US-0003](EP-0018-US-0003-preserved-starts-and-resources/EP-0018-US-0003-preserved-starts-and-resources.md) | Привычные старты, ресурсы и рейсы работают на новой карте | US-0001, US-0002 | 3 |
| [US-0004](EP-0018-US-0004-materialized-world-compatibility/EP-0018-US-0004-materialized-world-compatibility.md) | Новый и прежний мир продолжаются после сохранения | US-0001, US-0002; partial US-0003 ticket gates | 4 |
| [US-0005](EP-0018-US-0005-local-map-scale/EP-0018-US-0005-local-map-scale.md) | Максимальное приближение показывает локальную окрестность | US-0001 | 3 |
| [US-0006](EP-0018-US-0006-multiscale-map-readability/EP-0018-US-0006-multiscale-map-readability.md) | Район, кластер и система читаются при отдалении карты | US-0004, US-0005 | 4 |
| [US-0007](EP-0018-US-0007-map-navigation-and-estimates/EP-0018-US-0007-map-navigation-and-estimates.md) | Переход от обзора системы к станции и понятное время пути | US-0003, US-0004, US-0005, US-0006 | 2 |
| [US-0008](EP-0018-US-0008-system-map-acceptance/EP-0018-US-0008-system-map-acceptance.md) | География и читаемость подтверждены воспроизводимой приёмкой | US-0001, US-0002, US-0003, US-0004, US-0005, US-0006, US-0007 | 3 |
| [US-0009](EP-0018-US-0009-project-documentation/EP-0018-US-0009-project-documentation.md) | Контракты карты и передача результата отражают фактическую поставку | US-0001, US-0002, US-0003, US-0004, US-0005, US-0006, US-0007, US-0008 | 3 |

| Критерий | Истории |
|---|---|
| E18-AC-01 | US-0001, US-0002, US-0008 |
| E18-AC-02 | US-0002, US-0004, US-0008 |
| E18-AC-03 | US-0001, US-0003, US-0008 |
| E18-AC-04 | US-0004, US-0008 |
| E18-AC-05 | US-0002, US-0003, US-0007, US-0008 |
| E18-AC-06 | US-0005, US-0008 |
| E18-AC-07 | US-0006, US-0007, US-0008 |
| E18-AC-08 | US-0008 |
| E18-AC-09 | US-0009 |

Исполнение сериализуется по точным ticket edges в [Tickets.md](Tickets.md). Client US-0005 может разрабатываться после контрактов US-0001; новая content activation после US-0004. Итоговая приёмка US-0008 после всех функциональных историй. Документация US-0009 последняя.

## Review log
2026-11-07T17:21:08Z (время записи): получено точное поручение «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами». Последовательный review отключён прямым указанием пользователя. Состав и ticket map подготовлены автономно; все planning approvals имеют вид delegated-planning/workflow.

## Epic review log
Индивидуальное сообщение «утверждаю эпик» не получено и не приписывается пользователю. stage approved установлен на основании прямой делегации полного планирования. На момент planning: execution not_started; automated/native/human acceptance NOT RUN. Текущее исполнение см. в ledger выше.

## Gaps, риски и backlog
- G-01: текущий screenshot не содержит доказанного seed/save; не обещать пиксельное воспроизведение именно этого кадра. Воспроизводимый корпус использует объявленные seeds.
- G-02: diameter/belt/intercluster настройки A-03 требуют corpus feasibility; если не укладываются, исправить алгоритм или явно пересмотреть Board, не ослаблять min1d.
- G-03: совместимость старых схем/strict JSON должна быть доказана реальными fixtures; новый DTO не означает готовую миграцию.
- G-04: физический ввод и человеческая оценка читаемости не заменяются unit/render tests.
- G-05: 80 FPS из прежних Board — открытый продуктовый ориентир, не текущий измеренный результат; US-0008 фиксирует отдельный статус без подмены.
- Backlog: распределение кластеров по разным поясам, новое наполнение локальных пространств и общий экономический баланс требуют отдельного поручения.

## Проверка планирования
Фактический статический аудит planning: 41 Markdown-файл, 9 историй, 29 тикетов, 38 узлов зависимостей. Проверены совпадение имён/frontmatter/канонических путей, <=5 tickets/story, <=5 implementation files/ticket, один production layer и owning test project, все локальные Markdown-ссылки, разрешение всех dependency IDs и отсутствие циклов, coverage AC каждой истории, существование current-state paths и маркировка future files. Ошибок структуры/ссылок/покрытия нет. Exact focused test filters включают все named tests тикета, scoped format commands перечисляют точные allowlist paths. Whitespace проверен для новых untracked файлов через git diff --no-index --check; настройки Git постоянно не менялись.

Скрипты аудита выполнялись из stdin без временных файлов. Runtime/build/tests/native/product acceptance NOT RUN: изменены только новые документы этого эпика. Исходная рабочая копия вне EP-0018 сохранена.

## Начало исполнения — 2026-11-07
Текущее прямое поручение пользователя выполнить EP-0018 по EpicExecutionPrompt.md разрешает реализацию, актуализацию Board, отдельные коммиты и немедленный push каждого тикета. Planning записи выше сохраняют историческое значение. База c43e8af5cfbc76844e6a9109921fbae41153d370; подтверждённый upstream origin/codex/ep-0016-tactical-battle-skins. Исходные изменения: только untracked папка данного эпика.
Порядок: US-0001→0002→0003→0004→0005→0006→0007→0008→0009. Проверка документационной истории: дополнительно требуется инвентаризация всего репозитория и обновление графа по итоговому diff; ограничения пяти файлов не сокращают обязательный scope раздела 6 промта. Фактический ход: [ImplementationStatus.md](ImplementationStatus.md).

## Исправление реального dependency order — 2026-11-07

Сохранение активногоv2рейса изUS0003TK0003 требует strictfileingress и materialized
trading validation US0004TK0001/0002. Поэтому эти2тикета выполняются послеUS0003TK0002,
затем voyageTK0003, затем local-file-roundtrip/activation. Story4 больше не требует
завершения всейStory3 перед началом; точные частичные gates перечислены в карточках.
Это устраняет логическую зависимость тестов от ещё не реализованногоloader без
переноса FAIL вPASS или объединения тикетов. 9историй/29тикетов остаются прежними.
