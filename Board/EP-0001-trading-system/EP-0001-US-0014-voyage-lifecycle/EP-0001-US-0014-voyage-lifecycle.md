---
epic: EP-0001-trading-system
story: EP-0001-US-0014-voyage-lifecycle
title: Жизненный цикл межстанционного рейса
stage: approved
dependencies: [EP-0001-US-0004-seeded-trading-map]
created: 2026-09-21T14:55:44Z
source_request: "сделай тикеты D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0014-voyage-lifecycle\\EP-0001-US-0014-voyage-lifecycle.md"
current_review: complete
revision: 1
---

# Жизненный цикл межстанционного рейса

## Входное техническое задание

Точное сообщение пользователя сохранено в `source_request`. Время фиксации grounding: 2026-09-21T14:55:44Z UTC; это не утверждение о времени отправки сообщения. ID эпика/story в пути, frontmatter, папке и имени файла совпадают; ticket folders до этого запроса отсутствовали, первый свободный номер — `0001`.

Исходный объём: `Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md:137–147`, `Documentation/02-FirstRelease/Mechanics/TradingSystemMvpStories.md:14–36,332–351`, решения эпика `../Documentation.md:97–104,136–170` и правило Undock `Documentation/01-Requirements/EngineRequirements.md:110–129`. Все относительные пути ниже считаются от `D:/DeepSpaceSaga/DSS`.

Исходный story-файл содержал устаревший факт, что Undock — placeholder. В текущем checkout кнопка возвращает `ScreenEvent.Undock` (`src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs:112–136`), `SkiaWindow` отправляет команду и закрывает modal (`src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:653–661`), а Engine очищает docking state с сохранением текущего motion (`src/DeepSpaceSaga.Engine/SimulationEngine.cs:1883–1903`). Поэтому история расширяет существующий Undock назначением и lifecycle, а не создаёт его заново.

## User story

Как игрок, я хочу выбрать связанное торговое направление на существующем Station screen, покинуть текущую станцию и видеть authoritative состояние рейса до стыковки с назначением. Рейс проходит наблюдаемые состояния `Docked → Undocking → InTransit → Docking → Docked`; он не перемещает корабль автоматически и не меняет скорость или правила Approach. После ухода рынок отправления недоступен, а после успешной стыковки Trade относится только к станции назначения и фактическому cargo. Active voyage, destination, монотонный progress и последняя причина блокировки продолжаются после Save/Load без второй стоянки или второго рейса.

## Acceptance criteria

- AC-01: при `Docked` authoritative snapshot публикует только соседние назначения материализованного графа US-0004 с ID, display name, distance class, travel estimate, доступностью и machine-readable причиной блокировки; Client не читает save DTO и не вычисляет маршруты.
- AC-02: `navigation.undock` с выбранным `TargetObjectId` атомарно создаёт один voyage, очищает docking state существующим правилом и публикует `Undocking`; повтор того же `CommandId` replay-ится без второго voyage. В legacy-сценарии без trading map существующий untargeted Undock остаётся совместимым.
- AC-03: следующий положительный шаг motion переводит `Undocking` в `InTransit`. Progress хранится в permille `0..1000`, не убывает и вычисляется Engine из сокращения прямой дистанции до выбранной неподвижной станции; отклонение/ручной курс не откатывают достигнутый progress.
- AC-04: во время active voyage Dock к другой станции отклоняется; валидированный Dock к destination публикует `Docking`, а успешный существующий docking dialogue завершает voyage как `Docked`. Отмена/отказ docking dialogue возвращает `InTransit`, не создавая стоянку.
- AC-05: в `Undocking/InTransit/Docking` `DockedStationTrade` равен `null`; после завершения Docking он строится только из `DockedStationObjectId` назначения. Cargo не копируется и не переносится отдельным voyage-state.
- AC-06: попытка старта без destination, по отсутствующему/несмежному/закрытому маршруту, при active voyage или `PortFeeDebt > 0` отклоняется с стабильным reason code; Station/GameSession показывают понятный текст authoritative причины. Код `voyage_insufficient_fuel` зарезервирован и отображается здесь, а фактический fuel evaluator и reservation принадлежат US-0009.
- AC-07: Save/Load сохраняет phase, voyage ID, origin, destination, start distance, progress и последнюю block reason. Продолжение не повторяет Undock/Dock, не создаёт второй active voyage и после одинакового следующего motion/docking action даёт тот же snapshot.
- AC-08: Station screen позволяет выбрать только route option из snapshot и отправляет destination через существующую async command boundary. GameSession показывает phase, destination, progress и block reason из snapshot без локальной симуляции.
- AC-09: история не меняет `navigation.approach`, `ApproachRoute`, скорость корабля, календарно-motion разделение или геометрию свободного полёта; existing untargeted legacy Undock/Dock regressions остаются зелёными.

## Non-goals

- Не выполнять production-код в StoryBuilder и не менять requirements/эпик.
- Не вводить abstract fast travel, автопрокладку физического маршрута, автопилот, изменение скорости, коллизии или бой.
- Не рассчитывать, не резервировать и не списывать `routeFuelCost`: формула, engine efficiency, refund/settlement принадлежат US-0009. Эта история фиксирует reason-code/UI boundary для результата того evaluator.
- Не реализовывать куплю/продажу, quote, market knowledge, риск маршрута, ledger или прибыль — это US-0003/0006/0008/0010/0011/0015/0016.
- Не сохранять копию cargo/рынка внутри voyage DTO и не раскрывать удалённые цены.
- Не изменять Station district travel (`StationTravelCommand`) — это перемещение внутри одной пристыкованной станции, а не межстанционный voyage (`src/DeepSpaceSaga.Contracts/StationTravelCommand.cs:3–8`; `src/DeepSpaceSaga.Engine/SimulationEngine.Time.cs:31–55`).

## Dependencies

Прямая зависимость: `EP-0001-US-0004-seeded-trading-map`. Планируемый контракт US-0004 задаёт материализованные station IDs и `TradingMapEdgeData` с endpoints, distance, travel estimate, class, fuel multiplier и risk (`EP-0001-US-0004-TK-0001-trading-map-schema`). На момент planning у US-0004 существуют только TK-0001/TK-0002, а TK-0003…TK-0005 из её approved map ещё не созданы; реализация этой истории начинается только после сверки exact merged signatures и materialized-map bootstrap.

US-0006 и US-0009 зависят от этой истории. Поэтому здесь создаются lifecycle и extension boundary, но не дублируются round-trip trade proof или fuel accounting. US-0012 позже владеет объединённой migration/version policy; текущая история добавляет optional voyage state и round-trip, не резервируя конфликтующий финальный SaveFormat.

## Grounding and invariants

- Engine остаётся единственным владельцем gameplay-state, Client получает immutable snapshots через async boundary: `Documentation/00-Process/CLAUDE.md:21–54,72–103`; `EngineRequirements.md:352–372`.
- Текущий snapshot публикует docking state, trade только для фактически пристыкованного корабля и route arrival metadata: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11–57`; `src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs:66–73`; `src/DeepSpaceSaga.Engine/SimulationEngine.cs:512–533,560–597`.
- Существующий Undock сохраняет текущий motion и очищает `IsDocked/DockedStationObjectId`: `SimulationEngine.cs:1883–1903`; named regressions — `tests/DeepSpaceSaga.Engine.Tests/DockCommandTests.cs:281–310`.
- Dock проверяет station/range/speed/direction и начинает существующий dialogue: `SimulationEngine.cs:1906–1963`; реальный `DockPlayerToStation` коммитит `(1,1)`, station ID и port schedule: `src/DeepSpaceSaga.Engine/Dialogue/DialogueEffectTransaction.cs:79–94`.
- Save capture сначала догоняет motion и pending commands, затем пишет единый `GameStateData`: `SimulationEngine.cs:737–833`; load полностью строит runtime до commit и устанавливает time cursors: `SimulationEngine.cs:196–240,342–359`.
- Calendar и motion cursor разделены, физическое движение продвигается по simulation time: `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:8–52`. Voyage progress не может ускорять ship motion.
- Approach сохраняет скорость, не включает docking и не обходит препятствия: `EngineRequirements.md:5327–5343`.

## Assumptions

- A-01: target выбранного voyage передаётся существующим `PlayerCommand.TargetObjectId` команды `navigation.undock`; новый synchronous/session-control API не нужен.
- A-02: destination станции US-0004 неподвижны. Progress = `max(previous, round(clamp(1 - remainingDistance / initialDistance, 0, 1) × 1000))`; он описывает достигнутое физическое продвижение, а не ETA и не заставляет корабль двигаться.
- A-03: `Undocking` наблюдается в snapshot, обработавшем команду; `InTransit` начинается только на следующем положительном motion advance. `Docking` начинается после полной существующей проверки Dock и сохраняется на время docking dialogue.
- A-04: незакрытое обязательство этой истории — существующий `PortFeeDebt > 0`. Passenger/quest/contract restrictions добавляются только отдельным подтверждённым правилом, не угадываются.
- A-05: после завершения рейса snapshot показывает `Docked` и route options новой станции; active voyage ID/origin/destination очищены. Последняя block reason хранится до успешного старта или следующей отклонённой попытки.
- A-06: US-0008 сможет дополнить route availability, US-0009 — `voyage_insufficient_fuel`, не меняя контракт snapshot/reason code. До их реализации route считается открытым, если edge существует и нет локального debt/active-voyage blocker.
- A-07: voyage state — optional additive save section. Итоговый numeric SaveFormat bump/migration выполняет US-0012 поверх merged prerequisites; US-0014 не объявляет собственный будущий номер.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0014-TK-0001-voyage-contract | Snapshot-контракт жизненного цикла рейса | contracts | US-0004/TK-0001 | AC-01, AC-06, AC-08 |
| EP-0001-US-0014-TK-0002-authoritative-voyage-lifecycle | Authoritative lifecycle и продолжение voyage | engine | TK-0001; US-0004 | AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07, AC-09 |
| EP-0001-US-0014-TK-0003-station-route-departure | Выбор назначения и отправление со Station | client | TK-0001, TK-0002 | AC-01, AC-06, AC-08 |
| EP-0001-US-0014-TK-0004-voyage-status-presentation | Статус рейса на GameSession | client | TK-0001, TK-0002 | AC-03, AC-04, AC-06, AC-08 |

Implementation files: 4 / 5 / 5 / 2. Dependency order: TK-0001 → TK-0002 → (TK-0003 и TK-0004). Один production layer и matching test project указаны в каждом тикете; dependency graph ацикличен.

## Gaps and backlog

- G-01: исходная ссылка на placeholder устарела относительно checkout и `CLAUDE.md:150–156`; текущий код уже реализует basic Undock. Story исправлена на extension scope без изменения requirements.
- G-02: US-0004 ещё не завершила собственный ticket set, а её production API отсутствует. TK-0002 содержит exact expected dependency surface; несовпадение после merge возвращает тикет в review, а не расширяет files скрытно.
- G-03: route availability от событий и фактическая недостаточность топлива появляются в US-0008/US-0009. US-0014 фиксирует стабильные reason codes и UI, но не выдаёт downstream evaluator за реализованный.
- G-04: UI-тексты в текущем Client в основном английские и не имеют общего voyage localization catalog. Тикеты используют понятные английские строки в существующем стиле; отдельная локализация — backlog, если не появится до implementation.
- G-05: `requirements-engineer`, упомянутый repository guide, отсутствует среди доступных skills. Применён нормативный DSS-StoryBuilder workflow; блокирующих продуктовых вопросов нет.
- Production code, epic Documentation и requirements не изменялись; dotnet tests/build не запускались для planning-only работы.

## Decision and review log

- 2026-09-21T14:55:44Z — записано точное сообщение пользователя; canonical epic/story/path IDs совпадают, следующий ticket number `0001`.
- Grounding: прочитаны mandatory process/architecture/requirements, эпик, story, source concept/MVP, dependency US-0004, соседние US-0006/0009/0012 и текущие contracts/engine/client/test surfaces.
- Plan-review: четыре merge units приняты автоматическим workflow. A-01…A-07 разрешают lifecycle/progress/persistence/UI неопределённости без блокирующего вопроса.
- Созданы TK-0001…TK-0004 в dependency order; production code и соседние dirty planning artifacts не редактировались.
- 2026-09-21T14:55:44Z — complete: canonical paths/frontmatter, matching projects, file limits, AC coverage и dependency graph проверены.
