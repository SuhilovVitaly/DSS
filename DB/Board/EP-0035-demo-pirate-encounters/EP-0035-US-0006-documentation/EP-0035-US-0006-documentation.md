---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0006-documentation
title: Документация проекта по итогам эпика
stage: approved
plan_status: approved
priority: P0
workstream: feature
dependencies: [EP-0035-US-0001-visible-threats, EP-0035-US-0002-map-evasion, EP-0035-US-0003-world-battle, EP-0035-US-0004-encounter-continuation, EP-0035-US-0005-route-threat-choice, EP-0035-US-0007-carrier-sensors, EP-0035-US-0008-fog-of-war-debug, EP-0035-US-0009-sensor-operator, EP-0035-US-0010-sensor-contact-dialogue, EP-0035-US-0011-pirate-signatures, EP-0035-US-0012-pirate-squadrons, EP-0035-US-0013-squad-battle-assembly, EP-0035-US-0014-squad-battle-rules, EP-0035-US-0015-squad-battle-continuation, EP-0035-US-0016-encounter-recovery, EP-0035-US-0017-world-integration]
created: 2026-11-16T20:02:53Z
implementation_status: implemented
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Original planning baseline; current execution and validation: [ExecutionStatus](../ExecutionStatus.md).

# Документация проекта по итогам эпика

## Связь с техническим заданием

Одобренный план, Д5: обязательная завершающая история (DB/Board/EpicExecutionPrompt.md, раздел 6). [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). История добавлена при сверке Board с сокращённым планом 2026-11-16: у этого результата плана не было владельца.

## User story

Как исполнитель следующих эпиков и владелец демо, я читаю актуальные требования, механики и экраны, которые описывают фактический результат этого эпика. Board эпика показывает реальные статусы историй, проверки и ограничения. Прежние описания, заменённые этим эпиком, помечены ссылкой на новый контракт, а не переписаны задним числом.

## Зависимости

- [EP-0035-US-0001-visible-threats](../EP-0035-US-0001-visible-threats/EP-0035-US-0001-visible-threats.md)
- [EP-0035-US-0002-map-evasion](../EP-0035-US-0002-map-evasion/EP-0035-US-0002-map-evasion.md)
- [EP-0035-US-0003-world-battle](../EP-0035-US-0003-world-battle/EP-0035-US-0003-world-battle.md)
- [EP-0035-US-0004-encounter-continuation](../EP-0035-US-0004-encounter-continuation/EP-0035-US-0004-encounter-continuation.md)
- [EP-0035-US-0005-route-threat-choice](../EP-0035-US-0005-route-threat-choice/EP-0035-US-0005-route-threat-choice.md)

## Completion evidence

- **AC-01.** Инвентаризация влияния: по итоговому diff, контрактам, командам, конфигурации и версиям схем перечислены все затронутые документы с результатом проверки.
- **AC-02.** Обновлены канонические документы DB/Documentation (требования, механики, экраны, сохранения) и затронутые документы вне Documentation; исторические отчёты сохранены с пометкой о замене.
- **AC-03.** Board эпика, реестр DB/Board/README.md и DemoPlan синхронизированы; ссылки, ID и количества проверены; Graphify перестроен либо помечен NOT RUN с причиной.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

История выполняется последней в эпике и зависит от всех его функциональных историй. Не заменяет документационные AC внутри функциональных историй.

Численные настройки и продуктовые допущения не являются согласованными значениями без отдельной фиксации. Engine остаётся владельцем мира, времени, денег и исходов; Client показывает подтверждённое состояние. Прежняя граница «без тикетов» заменена текущим поручением; декомпозиция и allowlist приведены ниже.

## Статус и review evidence

Добавлена агентом по поручению владельца найти и добавить недостающие истории. Отдельное human approval формулировок не заявляется: story stage — draft, plan_status — approved (сокращённый план одобрен ранее).

- 2026-11-16T20:02:53Z — время фиксации поручения в Board, не доказанное время отправки: «на основе этого пройди по созданными эпикам и сторисам в  D:\DeepSpaceSaga\DSS\DB\Board\ и реши каких сторисов или эпиков нам не хватает. там же добавь недостающие».
- Проверка автора: результат демонстрируем, зависимости указаны, completion evidence наблюдаемо; тикетная декомпозиция не выполнялась.
- Implementation: NOT STARTED. Automated/runtime/native/product acceptance: NOT RUN / OPEN.

## Текущая декомпозиция и review log

2026-11-16T20:32:46.441Z — время записи поручения: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Тикеты подготовлены workflow автоматически; отдельное человеческое утверждение допущений не заявляется. Implementation NOT STARTED; automated/native/audio/product gates NOT RUN / OPEN.

## Входное техническое задание

пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо Связь с сокращённым демо-контрактом и исходные критерии сохранены выше.

## Acceptance criteria

- **AC-01.** Инвентаризация влияния: по итоговому diff, контрактам, командам, конфигурации и версиям схем перечислены все затронутые документы с результатом проверки.
- **AC-02.** Обновлены канонические документы DB/Documentation (требования, механики, экраны, сохранения) и затронутые документы вне Documentation; исторические отчёты сохранены с пометкой о замене.
- **AC-03.** Board эпика, реестр DB/Board/README.md и DemoPlan синхронизированы; ссылки, ID и количества проверены; Graphify перестроен либо помечен NOT RUN с причиной.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- [EP-0035-US-0001-visible-threats](../EP-0035-US-0001-visible-threats/EP-0035-US-0001-visible-threats.md)
- [EP-0035-US-0002-map-evasion](../EP-0035-US-0002-map-evasion/EP-0035-US-0002-map-evasion.md)
- [EP-0035-US-0003-world-battle](../EP-0035-US-0003-world-battle/EP-0035-US-0003-world-battle.md)
- [EP-0035-US-0004-encounter-continuation](../EP-0035-US-0004-encounter-continuation/EP-0035-US-0004-encounter-continuation.md)
- [EP-0035-US-0005-route-threat-choice](../EP-0035-US-0005-route-threat-choice/EP-0035-US-0005-route-threat-choice.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0035-US-0006-TK-0001-current-contracts](EP-0035-US-0006-TK-0001-current-contracts/EP-0035-US-0006-TK-0001-current-contracts.md) | Актуальные механики и технические контракты | tooling | EP-0035-US-0001-TK-0004-client, EP-0035-US-0002-TK-0003-client, EP-0035-US-0003-TK-0003-client, EP-0035-US-0004-TK-0002-encounter-ui, EP-0035-US-0005-TK-0003-client | AC-01, AC-02, AC-04 |
| 2. [EP-0035-US-0006-TK-0002-board-status](EP-0035-US-0006-TK-0002-board-status/EP-0035-US-0006-TK-0002-board-status.md) | Board, реестр и состояние демо | tooling | EP-0035-US-0006-TK-0001-current-contracts | AC-01, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.


## Уточнение грумминга 2026-11-17

US-0006 последняя после новых US-0007 и US-0008; документирует сенсоры, +20%, normal defaulttrue/explicit debugfalse (D-35-37), команду и границы AI knowledge. Теперь 8 историй / 28 тикетов.

## Уточнение оператора и доклада 2026-11-17

Новые prerequisites US-0009 и US-0010; US-0006 последняя. Документировать реального оператора/skill, pause policy, наблюдаемый доклад, waypoint patrol. Всего 10 историй / 38 тикетов.

## Уточнение сигнатур и скоростей 2026-11-17

US-0011 также prerequisite; итог 11 историй / 43 тикета. Документировать frozen speed ratios, fixed totals, signature estimation и private composition.

## Интеграция D-35-21…26 с эскадрой
Нормативное текущее уточнение supersedes прежнюю single-carrier-only трактовку: stable pirate group может быть singleton или demo squad ровно2xЛогово, одна карта-фишка и один automatic report за кампанию. Member identities/loadouts независимы,6 drones каждому,12 squad total; скрытые классы не попадают в world report. Aggregate/runtime/schema принадлежат US0012; multi-carrier assembly US0013; rules US0014; UI/settlement/save US0015. Эти продолжения не заменять объединением drone lists под одним carrier.
Первый automatic sensor report ставит pause; повторные acquisitions не ставят pause и не открывают modal. consumed flag сохраняется по campaign/group. Simultaneous batching CLOSED D-35-33; manual report — отложенное A.
При уничтожении ПОСЛЕДНЕГО carrier стороны оставшиеся surviving drones получают existing Disabled/Speed0, Destroyed не меняются; до этого автоматического отключения нет. Disable не значит pilot death. D-35-26 закрывает orphan G-35-14.
Старые tickets дают prerequisite singleton behavior; итоговая acceptance EP35 требует US0012–0015. Capture20км и sensor200/240/264км остаются утверждёнными; aggregate reference/range policy согласована D-35-32: один group point, max operational radius.
## D-35-37: player fog включён, owner debug обзор выключает fog
Пользователь: «туман войны для игрока будет включен. я пока хочу видеть без тумана войны. Давай в этом эпике не будем обсудать сюжет».
Normal player demo default FogOfWarEnabled=true. Owner/developer использует явный debug override false через console для обзора карты. Прежние D-35-03 universal defaultfalse и A-35-02 resetfalse SUPERSEDED; unconditional false при Load/NewGame запрещён. Без явного override normal/legacy/new session safe default=true.
No-fog меняет только разрешённую debug projection, не sensor acquisition/AI knowledge/pursuit/доклады/генерацию loadout. Returntrue скрывает out-of-sensor live positions/tooltip/hit targets. Console показывает actual value/override status.
G-35-17 CLOSED D-35-40: app process memory, survives NewGame/Load, discarded on exit; defaults true/10.
Сюжет и named captain вне текущего EP35 grooming. Прежний G-35-16 перенесён в deferred integration note EP39, НЕ обязательный blocker EP35; EP39 не изменён. Не утверждено, что captain является пятой группой, появляется заранее или является исключением. Текущее D остаётся4 ORDINARY группы на старте/no respawn.
Tests NOT RUN: NormalNewGame_FogTrue; ExplicitDebugFalse_RevealsOnlyProjection; ReturnTrue_HidesUndetectedLiveContacts; LegacyAndNewSession_DefaultTrueWithoutOverride; FogOverride_NoSensorEventsOrAiKnowledge; OverrideLifetime_AccordingToResolvedG35_17.
## D-35-38: только лёгкие и тяжёлые дроны
Пользователь: «оставим только тяжелых и легких».
Текущий pool классов light/heavy; прежнее medium/heavy SUPERSEDED. Сохраняются independent50/50 per slot, отсутствие caps/forced mix, actual totals6/10/12, скрытый composition на first actual sensor acquisition. Оператор не раскрывает class distribution.
G-35-08 CLOSED: новый medium frame не нужен. Использовать existing Frames/light.json и Frames/heavy.json. Grounding layouts.json:3,16 — heavy sword/spear с2 shields; :29,41 — light sword/spear с1 shield. Equipment/layout IDs и weights теперь CLOSED D-35-39: два existing sword/spear варианта каждого класса,50/50 equipment.
Stable ticket EP-0035-US-0011-TK-0003-medium-content сохраняет исторический slug/ID, но теперь владеет существующим light/heavy layout pool; medium frame creation удалена из allowlist/steps.
## D-35-40…42: overrides, оператор и состоявшийся перехват
D-35-40 пользователь: «Да, настройки действуют до закрытия приложения, между запусками не сохраняются».
Host хранит explicit overrides только в app process memory: fog немедленно и после NewGame/Load; chance только следующий NewGame. Console main menu read/set без session допустим. Restart=>fogtrue/chance10; saves/profiles не содержат overrides. Failed/stale active application не выдаётся за effective success. Options применяются до первого engine tick/snapshot. G-35-13 lifetime и G-35-17 CLOSED.
D-35-41 пользователь: «Да, принимаем по наличию в экипаже и назначению, без смен и усталости».
Irina activecrew+sensor assignment=>264км,±1/±2; absent/unassigned=>script192км,±2/±4. Broken/disabled sensor=>0 coverage независимо от operator/script. No shifts/fatigue/injury schema. Reassignment восстанавливает бонус, не сбрасывает consumed groups; новые контакты расширенного радиуса идут через standard batch/autopause. G-35-06 CLOSED.
D-35-42 пользователь: «Да, принимаем: если перехват состоялся, после доклада и паузы начинается неизбежный бой».
При реальном pirate sensor contact и distance<=20км authoritative atomic capture фиксирует encounter. Player detection alone не capture. Для новой группы report+pause по batch policy; dismissal сохраняет Speed0; explicitcontinue начинает бой. Уже captured нельзя отменить сменой курса на паузе. Previously reported group не создаёт повторный automatic modal; используется существующий battle transition. Save/Load сохраняет pending capture, не даёт free escape. G-35-09 normal single-group inside-capture CLOSED; остаток: simultaneous capture CLOSED D-35-45: select earliest, no merge; recovery CLOSED D-35-47.
Tests NOT RUN: MainMenuOverrides_NoSession; OverridesSurviveNewGameLoadUntilExit; Restart_NormalDefaults; Save_NoOverrides; Chance_ExistingGroupsUnchanged; IrinaAbsentUnassigned_Fallback; AssignedIrina_Bonus; BrokenSensor_NoDetection; Reassignment_NoDuplicateReport; NewlyRevealedGroup_NormalBatch; CaptureThenReport_StaysPaused; ExplicitContinue_StartsUnavoidableBattle; PausedCourse_CannotUndoCapture; LoadPendingCapture_NoEscape; PlayerDetectionOnly_NoCapture.
Placement: all human station spawn400км A-35-17, START hard800км D-35-43/A-35-19; конкретные patrol routes ещё балансируются.
## D-35-44: явно утверждённые радиусы800/400
Пользователь: «Да, оставляем800км у стартовой и400км для спавна у остальных станций. Добавляй в тикеты».
Численные значения теперь explicit D, supersede provisional/delegated A-35-17/A-35-19: START station hard800км (spawn/patrol/pursuit no-entry); остальные человеческие станции initial spawn exclusion400км, без runtime защиты. Оба значения content parameters. Boundary/segment/safe-entry/committed capture invariants D-35-43 сохраняются.
G-35-18 CLOSED D-35-45: earliest continuous capture, deterministic tie, последовательные бои без merge; world frozen then paused, resume без immunity.
Остаток G-35-09/15: abnormal/partial battle outcomes и корректное world continuation без новой механики тактического отступления. Normal single capture/report/continue уже D-35-42.
A-35-18 стартовая позиция игрока отдельно от станции требует проверки actual scenario; отдельный clearance не утверждён. При обычном старте у start station800 уже покрывает sensor separation.
Прочие продуктовые decisions (radii,skill/ETA,equipment,spawn count/weights/norespawn,operator,fog,lifetime,batching) закрыты. Routine module physical stats/конкретные patrol waypoint coordinates остаются implementation/balance деталями; narrative deferred внеscope.
## D-35-47: восстановление captured боя с начала
Пользователь: «Да, принимаем восстановление того же боя с начала после закрытия приложения или сбоя».
Durable prebattle pending checkpoint ДО tactical start: committed capture, fixed group/member/drone composition, exact player/company prebattle state, stable encounter/battleId и replay seed. Continue после close/crash запускает тот же бой с начала, не navigable world. Ordinary midbattle save/load/menu retreat запрещены; perturn resume не вводится.
Win/loss settlement exactly once, затем durable finished checkpoint до map/end. Failed write удерживает pending result/retry storage, не restart/reapply. Crash при atomic persistence даёт либо old durable pending battle replay, либо durable finished state. Intermediate damage persistence не обещается.
G-35-19 CLOSED. US0016 owns engine checkpoint, local-client durable storage и client recovery. Legacy aborted settlement не используется для ordinary EP35 unfinished battle; debug/tutorial context отдельно.
Tests NOT RUN: checkpoint-before-start; same composition/playerstate/seed replay; no navigable escape; failedwrite retryonly; atomicoldpending-orfinished; exactlyonce rewards/losses; no pending loss via autosave.
## D-35-48: ничья считается поражением игрока
Пользователь: «ничья равно поражению. игрок проиграл».
Existing Draw, включая NoCombatCapability и simultaneousdefeat, отображается/учитывается как player defeat для EP35 campaign outcome. Сохранить actual hull/ammo/disabled/destroyed/pilot state; не фабриковать hull0/смерть пилотов ради label. Победные награды не выдавать; exactly-once settlement/durable boundary D-35-47 сохраняются. Поражение боя само по себе не вводит новый gameover при живом player carrier; existing destroyed-player terminal semantics сохраняются.
G-35-20 Draw CLOSED D-35-48; G-35-20T Timeout CLOSED D-35-49. Interactive SeriesTurnCap также player defeat.
Tests NOT RUN: DrawNoCombatCapability_PlayerDefeat; SimultaneousDefeat_PlayerDefeat; DrawDoesNotFabricateDamageOrPilotDeath; Defeat_NoVictoryReward; OutcomeReceiptIdempotent. Timeout policy CLOSED D-35-49; проверки только запланированы, не PASS реализации.
## D-35-49: истечение лимита ходов — поражение игрока
Пользователь ответил «да» на вопрос «Истечение лимита ходов тоже считаем поражением?».
EP35 campaign outcome/UI считает existing Timeout/SeriesTurnCap поражением игрока, как Draw по D-35-48. Сохранить actual hull/ammo/disabled/destroyed/pilot state; не фабриковать уничтожение носителя/дронов или смерть пилотов. Применить штатные defeat consequences exactlyonce, без victory rewards, затем durable finished checkpoint до map/end по D-35-47. Сам label defeat не вводит новый gameover при живом carrier.
G-35-20T CLOSED. Продуктовый грумминг EP35 завершён; технические A/allowlist/schema drift проверяет owning implementation, это не новые user blockers. Implementation NOT STARTED, tests/build/native/product acceptance NOT RUN/OPEN.
Tests NOT RUN: SeriesTurnCap_PlayerDefeat; TimeoutPreservesActualDamageAndPilotState; Timeout_NoVictoryRewards; TimeoutSettlement_ExactlyOnce; TimeoutFinishedCheckpoint_DurableBeforeRelease; TimeoutReplayReceipt_NoReapply.
