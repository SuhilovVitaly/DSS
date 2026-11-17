---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0002-map-evasion
title: Манёвр ухода от преследования
stage: approved
plan_status: approved
priority: P0
workstream: feature
dependencies: [EP-0035-US-0001-visible-threats]
created: 2026-11-16T19:49:59Z
implementation_status: not-started
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Манёвр ухода от преследования

## Связь с техническим заданием

Одобренный план, Д5: Пиратские встречи и избегание перехвата. [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). Использовать географию мира и owned battle EP-0029. Новый объём — существующая в мире группа и её жизненный цикл, а не ещё один боевой движок.

## User story

Как игрок, я могу избежать перехвата на карте с помощью манёвра и преимущества носителя в скорости. Я вижу начало преследования, доступные действия и момент успешного ухода.

## Зависимости

- [EP-0035-US-0001-visible-threats](../EP-0035-US-0001-visible-threats/EP-0035-US-0001-visible-threats.md)

## Completion evidence

- **AC-01.** Воспроизводимые примеры показывают уход более быстрого игрока при правильном манёвре и перехват при неудачном.
- **AC-02.** Правило явно учитывает начальную дистанцию, направление, сравниваемую скорость и окончание преследования; пауза не меняет исход.
- **AC-03.** После успешного ухода та же встреча не запускается немедленно заново; игра объясняет результат.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

G-01: численные пороги и критерий правильного манёвра нужно определить до тикетов; сравнение реальной/предельной скорости нельзя выбирать молча. Отступление из тактического боя не входит.

Численные настройки и продуктовые допущения не являются согласованными значениями без отдельной фиксации. Engine остаётся владельцем мира, времени, денег и исходов; Client показывает подтверждённое состояние. Прежняя граница «без тикетов» заменена текущим поручением; декомпозиция и allowlist приведены ниже.

## Статус и review evidence

Пользователь одобрил общий план и поручил материализацию. Детализация этой истории подготовлена агентом в рамках одобренного объёма; отдельное human approval формулировок и допущений не заявляется. Исторически story detail был draft; текущий workflow status указан в frontmatter и новой карте ниже. Это не препятствует завершению запрошенного создания документов и не разрешает реализацию.

- 2026-11-16T19:49:59Z — время фиксации запроса в Board, не доказанное время отправки: «План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации.».
- Проверка автора: результат демонстрируем, зависимости указаны, completion evidence наблюдаемо; тикетная декомпозиция не выполнялась. При будущей подготовке соблюдать предел пяти тикетов на историю; если детализация не помещается, сначала разделить story.
- Implementation: NOT STARTED. Automated/runtime/native/product acceptance: NOT RUN / OPEN. Создание Markdown не закрывает эти gates.


## Текущая декомпозиция и review log

2026-11-16T20:32:46.441Z — время записи поручения: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Тикеты подготовлены workflow автоматически; отдельное человеческое утверждение допущений не заявляется. Implementation NOT STARTED; automated/native/audio/product gates NOT RUN / OPEN.

## Входное техническое задание

пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо Связь с сокращённым демо-контрактом и исходные критерии сохранены выше.

## Acceptance criteria

- **AC-01.** Воспроизводимые примеры показывают уход более быстрого игрока при правильном манёвре и перехват при неудачном.
- **AC-02.** Правило явно учитывает начальную дистанцию, направление, сравниваемую скорость и окончание преследования; пауза не меняет исход.
- **AC-03.** После успешного ухода та же встреча не запускается немедленно заново; игра объясняет результат.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- [EP-0035-US-0001-visible-threats](../EP-0035-US-0001-visible-threats/EP-0035-US-0001-visible-threats.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0035-US-0002-TK-0001-contracts](EP-0035-US-0002-TK-0001-contracts/EP-0035-US-0002-TK-0001-contracts.md) | Контракт: Манёвр ухода от преследования | contracts | EP-0035-US-0001-TK-0004-client | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0035-US-0002-TK-0002-engine](EP-0035-US-0002-TK-0002-engine/EP-0035-US-0002-TK-0002-engine.md) | Правила: Манёвр ухода от преследования | engine | EP-0035-US-0002-TK-0001-contracts | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0035-US-0002-TK-0004-localized-feedback](EP-0035-US-0002-TK-0004-localized-feedback/EP-0035-US-0002-TK-0004-localized-feedback.md) | RU/EN условия, результаты и причины отказа | content-data | EP-0035-US-0002-TK-0002-engine | AC-01, AC-02, AC-03, AC-04 |
| 4. [EP-0035-US-0002-TK-0003-client](EP-0035-US-0002-TK-0003-client/EP-0035-US-0002-TK-0003-client.md) | Экран и причины отказа: Манёвр ухода от преследования | client | EP-0035-US-0002-TK-0004-localized-feedback | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.


## D-35-17 — потеря контакта и возврат на патруль

D-35-17 (пользователь): «Для демо пусть долетают до последней известной позиции и, если не нашли игрока, возвращаются на патруль. Добавляй в тикеты».
После потери собственного сенсорного контакта пират идёт к последней действительно наблюдённой позиции игрока. Сенсор продолжает работать по пути. При достижении этой точки без нового контакта группа возвращается к патрулю; поискового круга, прохода или дополнительного таймера поиска нет. Текущие скрытые координаты/маршрут игрока и debug visibility не используются.
A-35-08: если сенсор вновь обнаружил игрока по пути или на возврате, группа снова переходит в Pursuing; это логическое рабочее допущение, не отдельная дословная команда пользователя.
A-35-09: возвращаться к сохранённой следующей точке прерванного патруля; конкретный return waypoint policy не был отдельно одобрен.
G-35-10 [CLOSED D-35-19]: capture radius = 20 км = 200 world units, одинаковый обоим, distance <= radius между центрами.
Supersession: прежние A-01 escape radius/escapeDuration/max pursuit duration больше не завершают эту погоню. Escaped — наблюдаемый outcome после достижения lastKnown без повторного контакта, а не конкурирующая AI state machine. Старые cooldown параметры не дают иммунитет от нового настоящего контакта; duplicate events предотвращаются stable contact/encounter identity.
AC-05: потеря PirateDetectsPlayer фиксирует lastKnown только из реального наблюдения, движение туда и возврат без поискового круга. Motion pause/Load не меняют состояние. Reacquisition по A-35-08, radius боя G-35-10.

## D-35-18 — общая физическая дистанция перехвата

D-35-18 (пользователь): «давай возьмем 100 пикселей на самом приближенном масштабе. сколько это будет? Одинаковая дистанция да».
Оба пиратских типа используют одинаковый capture radius в физических world units. Пиксели — запрос калибровки фиксированного радиуса, не runtime условие. Дистанция боя не зависит от текущего zoom, размера окна, DPI/framebuffer scale или типа пиратского носителя.
A-35-10 [SUPERSEDED D-35-19]: первоначально предлагалось 10 км = 100 world units по базовым 100 логическим px. Пользователь позднее выбрал 20 км = 200 world units; 10 км больше не active baseline.
G-35-10 [CLOSED D-35-19]: утверждены одинаковые 20 км = 200 world units. Историческая калибровка 100 px не используется runtime; dynamic max zoom не меняет physical radius.
Grounding: src/DeepSpaceSaga.Client/Settings.json:30–31 — metersPerPixel[0]=100; src/DeepSpaceSaga.Client/UI/MapScalePolicy.cs:14–20 — max(settings.MaximumPpu, shortSide, diagonal); src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:365–378 — framebuffer/window scale и RenderScreenStack с windowSize, то есть логические UI pixels. EngineRequirements: 1 world unit=100 м.
D-35-19: captureRadiusWorldUnits=200 в content; boundary tests проверяют <200, =200, >200 для обоих типов. Число 100 world units из старого A не использовать.

## D-35-32: общая точка и лучший действующий сенсор эскадры
Пользователь: «Да, принимаем одну общую точку эскадры и лучший действующий сенсор без сложения радиусов».
Одна authoritative group position — центр map token. Её используют world range, bearing, ETA по D-35-29 и capture<=20км по D-35-19; tactical позиции отдельных carrier не становятся отдельными map contacts. Все расчёты в world units, zoom/DPI/window не меняют distance.
Squad sensor range=max operational radius её carriers, НЕ сумма. Два исправных Логова=>200км, один неработающий sensor=>200км от другого; оба неработающие=>0. Нулевая дальность означает отсутствие detection coverage, а не detection при coincident point. Не суммировать площадь/радиусы и не создавать дополнительный sensor origin для tactical member positions.
G-35-12 geometry/reference/sensor aggregation CLOSED. A-report-shape: один отчёт группы с двумя точными типами и одним approximate aggregate total; два automatic окна по членам недопустимы вследствие D-35-21 one/group. UI layout списка членов — implementation choice.
G-35-04 simultaneous batching CLOSED D-35-33: одна сводка всех новых групп текущего observation step; сортировка/selection/scroll — A.
Tests NOT RUN: TwoLairs_Max200NotSum400; OneSensorDisabled_UsesOther; AllSensorsDisabled_NoCoverage; GroupReference_SharedByRangeBearingEtaCapture; MemberTacticalPositions_NotMapContacts; CaptureAndSensorIndependentOfZoom.
## D-35-40…42: overrides, оператор и состоявшийся перехват
D-35-40 пользователь: «Да, настройки действуют до закрытия приложения, между запусками не сохраняются».
Host хранит explicit overrides только в app process memory: fog немедленно и после NewGame/Load; chance только следующий NewGame. Console main menu read/set без session допустим. Restart=>fogtrue/chance10; saves/profiles не содержат overrides. Failed/stale active application не выдаётся за effective success. Options применяются до первого engine tick/snapshot. G-35-13 lifetime и G-35-17 CLOSED.
D-35-41 пользователь: «Да, принимаем по наличию в экипаже и назначению, без смен и усталости».
Irina activecrew+sensor assignment=>264км,±1/±2; absent/unassigned=>script192км,±2/±4. Broken/disabled sensor=>0 coverage независимо от operator/script. No shifts/fatigue/injury schema. Reassignment восстанавливает бонус, не сбрасывает consumed groups; новые контакты расширенного радиуса идут через standard batch/autopause. G-35-06 CLOSED.
D-35-42 пользователь: «Да, принимаем: если перехват состоялся, после доклада и паузы начинается неизбежный бой».
При реальном pirate sensor contact и distance<=20км authoritative atomic capture фиксирует encounter. Player detection alone не capture. Для новой группы report+pause по batch policy; dismissal сохраняет Speed0; explicitcontinue начинает бой. Уже captured нельзя отменить сменой курса на паузе. Previously reported group не создаёт повторный automatic modal; используется существующий battle transition. Save/Load сохраняет pending capture, не даёт free escape. G-35-09 normal single-group inside-capture CLOSED; остаток: simultaneous capture CLOSED D-35-45: select earliest, no merge; recovery CLOSED D-35-47.
Tests NOT RUN: MainMenuOverrides_NoSession; OverridesSurviveNewGameLoadUntilExit; Restart_NormalDefaults; Save_NoOverrides; Chance_ExistingGroupsUnchanged; IrinaAbsentUnassigned_Fallback; AssignedIrina_Bonus; BrokenSensor_NoDetection; Reassignment_NoDuplicateReport; NewlyRevealedGroup_NormalBatch; CaptureThenReport_StaysPaused; ExplicitContinue_StartsUnavoidableBattle; PausedCourse_CannotUndoCapture; LoadPendingCapture_NoEscape; PlayerDetectionOnly_NoCapture.
Placement: all human station spawn400км A-35-17, START hard800км D-35-43/A-35-19; конкретные patrol routes ещё балансируются.
## D-35-43 / A-35-19: hard safezone стартовой станции800км
Пользователь: «Принимаем безопасную зону300км вокруг стартовой станции, в которую пираты не заходят даже при погоне? Да. Может быть даже800. Максимум потом исправим».
D-35-43: hard safezone START station, действует patrol/pursuit. A-35-19 исторический tuning800км; число затем явно подтверждено D-35-44,8000world units.
Остальные человеческие станции сохраняют только initial spawn exclusion400км A-35-17, НЕ runtime safezone. Start initial spawn minimum=max(400,800)=800км. Config хранит отдельные spawnExclusionRadius и startSafezoneRadius; изменение балансировки не требует новой механики.
Pirate group center всегда distance>=800км от start station. Player distance<=800км считается safe: никакого нового capture внутри/на boundary, даже если другая distance<=20км. При входе игрока в safezone текущая pursuit прекращается и pirates возвращаются patrol; это не телепорт и не мгновенное изменение физических скоростей. Уже committed capture D-35-42 не отменяется последующим приказом/телепортом; после capture ordinary movement не продолжается до разрешения боя.
No-entry проверяет actual swept motion segment целого шага, не только waypoint/endpoints; маршрут/поворот/торможение планировать заранее с действующими motion constraints. Нельзя пропустить пересечение круга большим dt, clamp/teleport корабль обратно или остановить инерцию мгновенно. Для невозможного patrol path нужен validated обход либо явный отказ content; радиус не уменьшать молча.
Start zone привязана к stable startstation identity; для текущего stationary demo используется её world position. Если станция станет подвижной — отдельное будущие правило, не вводить repulsion. Station turrets не входят в задачу.
Supersedes прежнее A-35-17 утверждение «нет hard safezone»: теперь исключение START station800. Для других stations это утверждение сохраняется: только initial400, позднее patrol/chase могут входить.
Tests NOT RUN: StartSpawnOutside800; OtherStationSpawnOutside400; PatrolSweptSegment_NeverEntersStart800; PursuitSweptSegment_NeverEntersStart800; SafePlayerEntry_AbortPursuitReturnPatrol; Boundary800_PlayerSafePirateNotInside; OtherStation_NoRuntimeSafehaven; CommittedCapture_CannotBeUndone; ConfigurableRadii; ImpossiblePlacementOrRoute_FailsWithoutRelaxation.
Owning US1 content/runtime validates initial placement/route; US2 motion/pursuit enforces segment/state; US4 save preserves safe return/pending capture without freeescape. Existing one-layer/max5file allowlists unchanged; при motion-owner drift сначала корректировать Board scope.
## D-35-44: явно утверждённые радиусы800/400
Пользователь: «Да, оставляем800км у стартовой и400км для спавна у остальных станций. Добавляй в тикеты».
Численные значения теперь explicit D, supersede provisional/delegated A-35-17/A-35-19: START station hard800км (spawn/patrol/pursuit no-entry); остальные человеческие станции initial spawn exclusion400км, без runtime защиты. Оба значения content parameters. Boundary/segment/safe-entry/committed capture invariants D-35-43 сохраняются.
G-35-18 CLOSED D-35-45: earliest continuous capture, deterministic tie, последовательные бои без merge; world frozen then paused, resume без immunity.
Остаток G-35-09/15: abnormal/partial battle outcomes и корректное world continuation без новой механики тактического отступления. Normal single capture/report/continue уже D-35-42.
A-35-18 стартовая позиция игрока отдельно от станции требует проверки actual scenario; отдельный clearance не утверждён. При обычном старте у start station800 уже покрывает sensor separation.
Прочие продуктовые decisions (radii,skill/ETA,equipment,spawn count/weights/norespawn,operator,fog,lifetime,batching) закрыты. Routine module physical stats/конкретные patrol waypoint coordinates остаются implementation/balance деталями; narrative deferred внеscope.
## D-35-45/46: последовательные бои и только победа/поражение
Пользователь: «Да, принимаем последовательные бои без присоединения соседних групп. Добавляй в тикеты. Давай теперь разберём выход из незавершённого боя, загрузку и сохранение последствий. Из боя выйти можно проиграв или победив».
D-35-45: несколько разных groups пересекают capture threshold=>выбрать earliest continuous crossing event; exact tie решается детерминированно по stable groupId (технический deterministic tiebreak). Только выбранная группа входит в бой, максимум2enemy carriers при squad2Логово; соседи не присоединяются/не подкрепляют. World motion frozen в бою. После результата world остаётся paused; explicitresume может позволить другой группе захватить игрока по обычным условиям. Нет postwin immunity.
G-35-18 CLOSED. Не путать arbitration с batch first-discovery: одна сводка шага может перечислить несколько новых групп, но только selected captured group участвует в текущем бою.
D-35-46: normal EP35 campaign battle заканчивается только win/loss. Нет retreat/abort/обычного выхода или загрузки другого мира, позволяющих уйти из ongoing battle. UI/command guard authoritative, не только скрытая кнопка. Debug/tutorial exit_battle существующие semantics — отдельный явный context; не разрешать ему незаметно обходить normal campaign capture и не менять tutorial tooling без owner scope.
G-35-19 CLOSED D-35-47: durable pending same-battle restart; no perturn resume, no freeworld.
Grounding LocalGameSessionConnection.cs:145–180: accepted settlement receipt cached by BattleId, повтор retry только continuation write; SortieSettlement.cs:17 применяет outcome до clear session. Переиспользовать existing exactly-once consequences; не считать Save failure повторной наградой.
Tests NOT RUN: EarliestContinuousCapture_WinsArbitration; ExactTie_StableGroupId; NeighborGroup_NotMerged; Battle_FreezesWorldMotion; ReturnWorld_Paused; Resume_AnotherCaptureAllowedNoImmunity; OngoingCampaignBattle_RejectNormalExitAndEscapeLoad; TutorialDebugContext_ExplicitSeparation; SettlementReplay_ExactlyOnce.