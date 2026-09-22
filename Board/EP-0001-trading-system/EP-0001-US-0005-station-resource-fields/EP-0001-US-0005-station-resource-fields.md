---
epic: EP-0001-trading-system
story: EP-0001-US-0005-station-resource-fields
title: Ресурсное окружение экономических районов
stage: approved
dependencies: [EP-0001-US-0004-seeded-trading-map]
created: 2026-09-21T09:56:58Z
source_request: "сделай тикеты для D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0005-station-resource-fields\\EP-0001-US-0005-station-resource-fields.md"
current_review: complete
revision: 2
---

# Ресурсное окружение экономических районов

## Входное техническое задание

Точное сообщение пользователя: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0005-station-resource-fields\EP-0001-US-0005-station-resource-fields.md». UTC фиксации контекста: 2026-09-21T09:56:58Z; время отправки сообщения недоступно.

Источник: Documentation/02-FirstRelease/Mechanics/TradingSystemMvpStories.md:353–366 — профильные астероидные поля, derived station seed, менее частые, но не уникальные углеродные поля, сохранение отдельного Default_500, без обязательного пополнения рынка добычей. Эпик: ../Documentation.md:80–81,94,99,143. Все относительные paths в документах — от D:/DeepSpaceSaga/DSS; path:line описывает grounding snapshot, production API ниже явно планируемые.

## User story

Как игрок, я хочу видеть ресурсное окружение, соответствующее специализации района, чтобы устройство торговой карты было понятно по игровому миру. Рядом с добывающей станцией преобладает сырьё, а ледяные, смешанные и углеродные поля различаются составом и распространённостью. Один seed воспроизводит поля вокруг тех же станций. Состав раскрывается успешным StructuralScan в существующей панели объекта. Поля сохраняются при загрузке, не перекрывают подходы к станциям и не делают добычу обязательным условием торговли.

## Acceptance criteria

- AC-01: При New Game на материализованной пятистанционной карте US-0004 создаются профильные постоянные неподвижные Asteroid. Mining имеет большинство металлических астероидов; Hydroponic — ледяное поле; Industrial — смешанное с малой долей silicon. Carbon-bearing поля есть минимум у двух различных станций и реже других типов по числу объектов. Значения задаются тематическим JSON.
- AC-02: Одинаковые masterSeed/config/station map дают одинаковые IDs, coordinates, mass, CompositionType, resource fractions и RNG states независимо от порядка JSON/SpaceObjects. Потоки полей производны от seed станции и не потребляют потоки рынков/карты/сканирования. ID не пересекаются с существующими объектами.
- AC-03: Соблюдены annulus/min asteroid spacing, clearance от ВСЕХ станций и стартовые свободные коридоры вдоль рёбер экономического графа. Непригодная геометрия отвергается до замены мира без сокращения заданного количества или бесконечного reroll. Approach и его постоянная скорость не меняются.
- AC-04: При старте видны тип/масса/скорость постоянных астероидов, но состав и ресурсы скрыты, включая image/name/labels. StructuralScan для сгенерированного поля проверяет prerequisite, модуль, дальность120km, длительность60000ms GameTime и success85%; только успешное завершение раскрывает состав. Выход из range/исчезновение target проваливают попытку без success RNG. Повторное раскрытие запрещено, неудачный RNG позволяет повтор.
- AC-05: Save/Load сохраняет materialized fields, mass/composition/resource fractions, knowledge, named RNG/counters и незавершённое сканирование. Нет новой генерации, дубликатов или повторного эффекта; malformed state не заменяет текущий мир.
- AC-06: Existing Object Info и Scanner button показывают unknown/known composition и ресурсы без нового окна; основной asteroid label остаётся ID. Данные не раскрываются клиентским расчётом. Новые поля DTO совместимы со старыми snapshots.
- AC-07: Default/Docked/Undocked с одинаковым seed/map получают одинаковые поля. Default_500 и legacy scenarios без trading map остаются прежними; генератор/сканирование не меняют stock, budget, Credits или cargo и не требуют добычи. Проверены known/reveal/save/approach paths на реальных content definitions.

## Non-goals

Нет добычи, автоматического переноса ресурсов на рынок, crafting, depletion/respawn, боёв, collision physics, orbital generator, runtime риска льда или UI экономических связей. GeneralScan/NearbySignatures и полный scanner subsystem для старых произвольных targets остаются backlog. Новые permanent resource asteroids изначально IsKnown=true, поэтому GeneralScan не является prerequisite этой истории. Не расширять базовый CompositionType новыми значениями Carbon/Silicon.

## Dependencies

US-0004 должна предоставить материализованную карту до вызова resource generator: `GameStateData.TradingMap`, её Rules.Stations(ObjectId,MarketProfileId), Edges(FromStationObjectId,ToStationObjectId), пять actual SpaceObjectData stations и resolved masterSeed. На момент grounding у US-0004 есть только TK-0001/TK-0002; геометрия/bootstrap/content не закончены даже на уровне tickets. Это внешний implementation gate, не скрытая работа US-0005 и не повод менять соседнюю story.

Контракт US-0004 schema из её TK-0001: `TradingMapStateData` содержит `Rules`, `Edges`, `RngStreams`; координаты находятся в GameStateData.SpaceObjects. Resource generator вызывается после успешной материализации карты, до runtime build/commit. Он не переписывает её state/streams. US-0001 profiles/catalog — транзитивная зависимость: item.ice, item.iron-ore, item.magnesium-ore, item.silicon, item.carbon-ore существуют и относятся к Resource/Cargo.

## Grounding

- ScenarioData.cs:51–73 хранит GameStateData; :85–105 SpaceObjectData уже содержит MassKg, CompositionType, IsKnown. Runtime/save переносит их: src/DeepSpaceSaga.Engine/SimulationEngine.cs:291–293,777–780. Resource manifest/knowledge состояния состава пока нет.
- Seed определяется перед runtime build: SimulationEngine.cs:196–240. New Game и Save различаются в EngineContentLoader.CreateEngineFromScenarioFile/CreateEngineFromSaveFile; TypeDataPaths вложен в EngineContentLoader.cs:622–633, optional тематический путь можно добавить там.
- Base composition только Ice/Silicate/Iron: EngineRequirements.md:1227–1267. Supplemental resource fractions ниже — новый field metadata, а не замена этих типов или обещание добываемого yield.
- Mass ranges и веса находятся в JSON: EngineRequirements.md:1461–1504. Постоянные астероиды изначально известны, состав неизвестен: :1546–1574. StructuralScan правила: :1404–1432 (60000ms именно GameTime), :1562–1574.
- Scanner content уже содержит scanner.structuralScan, но Engine dispatch направляет прочие команды в engine handler: SimulationEngine.cs:1830–1843; IsEngineCommandType ограничивает команды двигателем. Это незавершённая feature, а не работающий reveal API.
- Existing DTO ObjectMotionSnapshot не передаёт состав/массу; src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs:11 и конец record. ObjectInfoPanel.BuildLines(ObjectInfoPanelData?) показывает Name/Speed/Direction: Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs:161–180; data record :383. GameSessionScreen.cs:702–728 пока включает target commands только по selection.
- Ice sprite выбирается по фактическому составу: SimulationEngine.cs:1166–1182, а известному объекту Image публикуется :479. Для новых полей нужна маскировка изображения до survey, иначе composition утечёт до сканирования.
- Общая масса/физический радиус не являются collision model: EngineRequirements.md:1478; Approach не обходит препятствия: :5337–5341. Clearance — правило начального размещения точек.
- AdvanceWorldTo(calendar,motion) раздельный: SimulationEngine.EconomyTime.cs:12–49; BuildSnapshot сначала двигает world, затем команды (SimulationEngine.cs:420–422). Новое сканирование использует отдельный календарный job; существующие engine motion cycles остаются без изменения шкалы.

## Invariants and assumptions

- A-01: Добавляется минимальное StructuralScan только для generated field asteroids: иначе AC раскрытия невыполним текущим кодом. Полный scanner rewrite не входит. Параметры120km/60000 GameTime ms/85% — действующие требования, хранятся в том же тематическом JSON.
- A-02: CompositionType остаётся Ice/Silicate/Iron. Resource fractions (ItemTypeId,Permille; сумма1000) описывают минералогию для UI, не inventory, yield или новую номенклатуру. Carbon-bearing имеет Silicate + преобладающую долю item.carbon-ore.
- A-03: Поля — Permanent/Stationary/SpeedMps0/IsKnown=true с неизвестным составом. Масса использует требования Small60/Medium30/Large8/VeryLarge2 и существующие диапазоны. Стартовые два temporary астероида не удаляются и не изменяются.
- A-04: Начальный баланс: Mining metal12/ice6/carbon3; Industrial mixed9/carbon3; Hydroponic ice12; Transit mixed6; Science mixed6. Всего57 новых астероидов,6 carbon-bearing у двух станций. Это tuning baseline, не доказательство баланса добычи.
- A-05: Clearance проверяется при генерации; последующее движение legacy астероида не делает save некорректным. Annulus2.5–4.5km, station clearance2km, asteroid spacing0.1km, логические коридоры half-width0.25km,64 попытки на объект. Соседние поля размещаются с проверкой всех станций/коридоров. Невозможность вместить полный config — явная ошибка; параметры должны быть проверены по всем шаблонам US-0004.
- A-06: IDs используют существующий SPC prefix и конфиг firstObjectNumber1000, четыре цифры минимум. Number assignment — canonical station/field/ordinal order, collisions отвергаются; saved nextObjectNumber хранится для неиспользования ранее выделенных номеров. Respawn и общий глобальный allocator не добавляются.
- A-07: Генерация включается только когда thematic config задан и New Game содержит материализованный TradingMap. Save без field state не генерирует поля задним числом. Saved rules — immutable authoritative snapshot конфигурации для продолжения, latest JSON не меняет уже созданные поля.
- A-08: Все RNG named/derived, counter starts10000 после1000 пропусков, +10 за draw, сохраняется. Сканирование имеет отдельный поток; rejected/busy/out-of-range не тратит success draw. Новая генерация не трогает чужие streams.
- A-09: Scanner jobs живут по календарю, как требует EngineRequirements:1422; двигатель и Approach продолжают использовать physical timeline. Это не общая миграция времени циклов. Пауза останавливает оба времени.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0005-TK-0001-resource-survey-contract | Контракт разрешённого состава | contracts | none | AC-04, AC-06 |
| EP-0001-US-0005-TK-0002-seeded-resource-fields | Конфигурация, генерация и сохранение полей | engine | TK-0001; US-0004 materialized map | AC-01, AC-02, AC-03, AC-05, AC-07 |
| EP-0001-US-0005-TK-0003-resource-field-content | Профили ресурсного окружения | content-data | TK-0002 | AC-01, AC-02, AC-03, AC-07 |
| EP-0001-US-0005-TK-0004-structural-resource-survey | Сканирование и сохранённые знания | engine | TK-0002, TK-0003 | AC-04, AC-05, AC-07 |
| EP-0001-US-0005-TK-0005-resource-info-panel | Состав в существующей панели объекта | client | TK-0004 | AC-04, AC-06, AC-07 |

Порядок: TK-0001 → TK-0002 → TK-0003 → TK-0004 → TK-0005. Implementation file counts:2/5/3/4/4. Matching tests: Contracts.Tests, Engine.Tests, Client.Tests, Engine.Tests, Client.Tests соответственно.

## Gaps and backlog

- G-01: US-0004 не завершена. Прежде исполнения TK-0002 завершить prerequisite map; отсутствующие TK-0003/0004/0005 US-0004 не записаны как существующие dependencies.
- G-02: Scanner сейчас catalog-only. Требуемая частью US-0005 реализация ограничена permanent generated fields; GeneralScan/остальные targets/energy subsystem — отдельный backlog.
- G-03: Carbon-bearing не вводит новый CompositionType; field resource fractions пока не ресурс для автоматической добычи. Rates/yields/устойчивость экономики — другие истории и balance review.
- G-04: Полного runtime object allocator и общей RNG persistence старых подсистем нет; state этой истории сохраняет собственные ID range/streams, не объявляя глобальную миграцию.
- G-05: Geometry feasibility зависит от окончательных layouts US-0004; content test corpus обязан проверить её, а failure не закрывается уменьшением количества без изменения config.
- G-06: requirements-engineer отсутствует в доступных/проверенных skills; используется явно предоставленный DSS-StoryBuilder. Требования/эпик не редактируются.
- Блокирующих продуктовых вопросов нет; boundaries и balance assumptions заданы явно. Production readiness не заявляется.

## Decision and review log

- 2026-09-21T09:56:58Z — точное сообщение пользователя и выбранный файл записаны выше; ID пути/frontmatter/folder совпадают, тикетов пока нет, первый номер0001.
- Grounding: обязательные требования/архитектура, эпик, story, US-0004 и code/test surface прочитаны. Найдены scanner/knowledge и image-leak gaps; минимальное покрытие включено явно.
- Plan-review: пять тикетов приняты автоматическим workflow; production код не выполняется, пользовательского approval не требуется.
- Создан TK-0001: contracts, 2 файла; unknown/known survey metadata. Следующий TK-0002.
- Создан TK-0002: engine, 5 файлов; full-count seeded generation, saved manifest и neutral projection. Следующий TK-0003.
- Созданы TK-0003/TK-0004/TK-0005: content-data3 / engine4 / client4 файла. Проверяется согласованность всех пяти тикетов.
- Review: уточнены generation-only spacing с legacy moving asteroids, unique scan target, Busy как отказ без отложенного запуска и stable overflow reason; scope и количество тикетов не расширены.
- 2026-09-21T10:16:50Z — complete: создано пять canonical tickets; проверены folder/file/frontmatter IDs, обязательные разделы, layer/test project, file counts2/5/3/4/4, dependency DAG и coverage AC-01..AC-07. Whitespace scan всех шести Markdown и git diff --check пройдены. Это завершение planning artifacts; production реализация и dotnet checks не выполнялись. US-0004 остаётся внешним gate, открытых продуктовых вопросов нет.
