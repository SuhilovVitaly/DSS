---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0003-world-battle
title: Перехват запускает бой с конкретной группой
stage: approved
plan_status: approved
priority: P0
workstream: feature
dependencies: [EP-0035-US-0002-map-evasion, EP-0032-US-0003-persistent-outcomes, EP-0035-US-0011-pirate-signatures]
created: 2026-11-16T19:49:59Z
implementation_status: not-started
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Перехват запускает бой с конкретной группой

## Связь с техническим заданием

Одобренный план, Д5: Пиратские встречи и избегание перехвата. [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). Использовать географию мира и owned battle EP-0029. Новый объём — существующая в мире группа и её жизненный цикл, а не ещё один боевой движок.

## User story

Как игрок, я вступаю в бой собственным крылом против встреченной группы. Перед необратимым вступлением понимаю, что тактического отступления в демо нет.

## Зависимости

- [EP-0035-US-0002-map-evasion](../EP-0035-US-0002-map-evasion/EP-0035-US-0002-map-evasion.md)
- [EP-0032-US-0003-persistent-outcomes](../../EP-0032-demo-campaign-continuity/EP-0032-US-0003-persistent-outcomes/EP-0032-US-0003-persistent-outcomes.md)

## Completion evidence

- **AC-01.** Состав врага соответствует группе мира, игрок использует реальное крыло и расходники.
- **AC-02.** Исход изменяет именно эту группу и компанию; повторное взаимодействие не запускает заново уже уничтоженного противника.
- **AC-03.** Проверены загрузка до встречи, возвращение после боя и запрет бесплатного выхода вместо отступления.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

Существующий технический abort/выход в меню не должен давать обход последствий; политику прерывания закрепить до тикетов.

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

- **AC-01.** Состав врага соответствует группе мира, игрок использует реальное крыло и расходники.
- **AC-02.** Исход изменяет именно эту группу и компанию; повторное взаимодействие не запускает заново уже уничтоженного противника.
- **AC-03.** Проверены загрузка до встречи, возвращение после боя и запрет бесплатного выхода вместо отступления.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- [EP-0035-US-0002-map-evasion](../EP-0035-US-0002-map-evasion/EP-0035-US-0002-map-evasion.md)
- [EP-0032-US-0003-persistent-outcomes](../../EP-0032-demo-campaign-continuity/EP-0032-US-0003-persistent-outcomes/EP-0032-US-0003-persistent-outcomes.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0035-US-0003-TK-0001-contracts](EP-0035-US-0003-TK-0001-contracts/EP-0035-US-0003-TK-0001-contracts.md) | Контракт: Перехват запускает бой с конкретной группой | contracts | EP-0035-US-0002-TK-0003-client, EP-0032-US-0003-TK-0004-terminal-save-policy | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0035-US-0003-TK-0002-engine](EP-0035-US-0003-TK-0002-engine/EP-0035-US-0003-TK-0002-engine.md) | Правила: Перехват запускает бой с конкретной группой | engine | EP-0035-US-0003-TK-0001-contracts | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0035-US-0003-TK-0004-localized-feedback](EP-0035-US-0003-TK-0004-localized-feedback/EP-0035-US-0003-TK-0004-localized-feedback.md) | RU/EN условия, результаты и причины отказа | content-data | EP-0035-US-0003-TK-0002-engine | AC-01, AC-02, AC-03, AC-04 |
| 4. [EP-0035-US-0003-TK-0003-client](EP-0035-US-0003-TK-0003-client/EP-0035-US-0003-TK-0003-client.md) | Экран и причины отказа: Перехват запускает бой с конкретной группой | client | EP-0035-US-0003-TK-0004-localized-feedback | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.


## D-35-18 — общая физическая дистанция перехвата

D-35-18 (пользователь): «давай возьмем 100 пикселей на самом приближенном масштабе. сколько это будет? Одинаковая дистанция да».
Оба пиратских типа используют одинаковый capture radius в физических world units. Пиксели — запрос калибровки фиксированного радиуса, не runtime условие. Дистанция боя не зависит от текущего zoom, размера окна, DPI/framebuffer scale или типа пиратского носителя.
A-35-10 [SUPERSEDED D-35-19]: первоначально предлагалось 10 км = 100 world units по базовым 100 логическим px. Пользователь позднее выбрал 20 км = 200 world units; 10 км больше не active baseline.
G-35-10 [CLOSED D-35-19]: утверждены одинаковые 20 км = 200 world units. Историческая калибровка 100 px не используется runtime; dynamic max zoom не меняет physical radius.
Grounding: src/DeepSpaceSaga.Client/Settings.json:30–31 — metersPerPixel[0]=100; src/DeepSpaceSaga.Client/UI/MapScalePolicy.cs:14–20 — max(settings.MaximumPpu, shortSide, diagonal); src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:365–378 — framebuffer/window scale и RenderScreenStack с windowSize, то есть логические UI pixels. EngineRequirements: 1 world unit=100 м.
D-35-19: captureRadiusWorldUnits=200 в content; boundary tests проверяют <200, =200, >200 для обоих типов. Число 100 world units из старого A не использовать.

## Интеграция D-35-21…26 с эскадрой
Нормативное текущее уточнение supersedes прежнюю single-carrier-only трактовку: stable pirate group может быть singleton или demo squad ровно2xЛогово, одна карта-фишка и один automatic report за кампанию. Member identities/loadouts независимы,6 drones каждому,12 squad total; скрытые классы не попадают в world report. Aggregate/runtime/schema принадлежат US0012; multi-carrier assembly US0013; rules US0014; UI/settlement/save US0015. Эти продолжения не заменять объединением drone lists под одним carrier.
Первый automatic sensor report ставит pause; повторные acquisitions не ставят pause и не открывают modal. consumed flag сохраняется по campaign/group. Simultaneous batching CLOSED D-35-33; manual report — отложенное A.
При уничтожении ПОСЛЕДНЕГО carrier стороны оставшиеся surviving drones получают existing Disabled/Speed0, Destroyed не меняются; до этого автоматического отключения нет. Disable не значит pilot death. D-35-26 закрывает orphan G-35-14.
Старые tickets дают prerequisite singleton behavior; итоговая acceptance EP35 требует US0012–0015. Capture20км и sensor200/240/264км остаются утверждёнными; aggregate reference/range policy согласована D-35-32: один group point, max operational radius.
## D-35-40…42: overrides, оператор и состоявшийся перехват
D-35-40 пользователь: «Да, настройки действуют до закрытия приложения, между запусками не сохраняются».
Host хранит explicit overrides только в app process memory: fog немедленно и после NewGame/Load; chance только следующий NewGame. Console main menu read/set без session допустим. Restart=>fogtrue/chance10; saves/profiles не содержат overrides. Failed/stale active application не выдаётся за effective success. Options применяются до первого engine tick/snapshot. G-35-13 lifetime и G-35-17 CLOSED.
D-35-41 пользователь: «Да, принимаем по наличию в экипаже и назначению, без смен и усталости».
Irina activecrew+sensor assignment=>264км,±1/±2; absent/unassigned=>script192км,±2/±4. Broken/disabled sensor=>0 coverage независимо от operator/script. No shifts/fatigue/injury schema. Reassignment восстанавливает бонус, не сбрасывает consumed groups; новые контакты расширенного радиуса идут через standard batch/autopause. G-35-06 CLOSED.
D-35-42 пользователь: «Да, принимаем: если перехват состоялся, после доклада и паузы начинается неизбежный бой».
При реальном pirate sensor contact и distance<=20км authoritative atomic capture фиксирует encounter. Player detection alone не capture. Для новой группы report+pause по batch policy; dismissal сохраняет Speed0; explicitcontinue начинает бой. Уже captured нельзя отменить сменой курса на паузе. Previously reported group не создаёт повторный automatic modal; используется существующий battle transition. Save/Load сохраняет pending capture, не даёт free escape. G-35-09 normal single-group inside-capture CLOSED; остаток: simultaneous capture CLOSED D-35-45: select earliest, no merge; recovery CLOSED D-35-47.
Tests NOT RUN: MainMenuOverrides_NoSession; OverridesSurviveNewGameLoadUntilExit; Restart_NormalDefaults; Save_NoOverrides; Chance_ExistingGroupsUnchanged; IrinaAbsentUnassigned_Fallback; AssignedIrina_Bonus; BrokenSensor_NoDetection; Reassignment_NoDuplicateReport; NewlyRevealedGroup_NormalBatch; CaptureThenReport_StaysPaused; ExplicitContinue_StartsUnavoidableBattle; PausedCourse_CannotUndoCapture; LoadPendingCapture_NoEscape; PlayerDetectionOnly_NoCapture.
Placement: all human station spawn400км A-35-17, START hard800км D-35-43/A-35-19; конкретные patrol routes ещё балансируются.
## D-35-45/46: последовательные бои и только победа/поражение
Пользователь: «Да, принимаем последовательные бои без присоединения соседних групп. Добавляй в тикеты. Давай теперь разберём выход из незавершённого боя, загрузку и сохранение последствий. Из боя выйти можно проиграв или победив».
D-35-45: несколько разных groups пересекают capture threshold=>выбрать earliest continuous crossing event; exact tie решается детерминированно по stable groupId (технический deterministic tiebreak). Только выбранная группа входит в бой, максимум2enemy carriers при squad2Логово; соседи не присоединяются/не подкрепляют. World motion frozen в бою. После результата world остаётся paused; explicitresume может позволить другой группе захватить игрока по обычным условиям. Нет postwin immunity.
G-35-18 CLOSED. Не путать arbitration с batch first-discovery: одна сводка шага может перечислить несколько новых групп, но только selected captured group участвует в текущем бою.
D-35-46: normal EP35 campaign battle заканчивается только win/loss. Нет retreat/abort/обычного выхода или загрузки другого мира, позволяющих уйти из ongoing battle. UI/command guard authoritative, не только скрытая кнопка. Debug/tutorial exit_battle существующие semantics — отдельный явный context; не разрешать ему незаметно обходить normal campaign capture и не менять tutorial tooling без owner scope.
G-35-19 CLOSED D-35-47: durable pending same-battle restart; no perturn resume, no freeworld.
Grounding LocalGameSessionConnection.cs:145–180: accepted settlement receipt cached by BattleId, повтор retry только continuation write; SortieSettlement.cs:17 применяет outcome до clear session. Переиспользовать existing exactly-once consequences; не считать Save failure повторной наградой.
Tests NOT RUN: EarliestContinuousCapture_WinsArbitration; ExactTie_StableGroupId; NeighborGroup_NotMerged; Battle_FreezesWorldMotion; ReturnWorld_Paused; Resume_AnotherCaptureAllowedNoImmunity; OngoingCampaignBattle_RejectNormalExitAndEscapeLoad; TutorialDebugContext_ExplicitSeparation; SettlementReplay_ExactlyOnce.
## D-35-47: восстановление captured боя с начала
Пользователь: «Да, принимаем восстановление того же боя с начала после закрытия приложения или сбоя».
Durable prebattle pending checkpoint ДО tactical start: committed capture, fixed group/member/drone composition, exact player/company prebattle state, stable encounter/battleId и replay seed. Continue после close/crash запускает тот же бой с начала, не navigable world. Ordinary midbattle save/load/menu retreat запрещены; perturn resume не вводится.
Win/loss settlement exactly once, затем durable finished checkpoint до map/end. Failed write удерживает pending result/retry storage, не restart/reapply. Crash при atomic persistence даёт либо old durable pending battle replay, либо durable finished state. Intermediate damage persistence не обещается.
G-35-19 CLOSED. US0016 owns engine checkpoint, local-client durable storage и client recovery. Legacy aborted settlement не используется для ordinary EP35 unfinished battle; debug/tutorial context отдельно.
Tests NOT RUN: checkpoint-before-start; same composition/playerstate/seed replay; no navigable escape; failedwrite retryonly; atomicoldpending-orfinished; exactlyonce rewards/losses; no pending loss via autosave.
## D-35-49: истечение лимита ходов — поражение игрока
Пользователь ответил «да» на вопрос «Истечение лимита ходов тоже считаем поражением?».
EP35 campaign outcome/UI считает existing Timeout/SeriesTurnCap поражением игрока, как Draw по D-35-48. Сохранить actual hull/ammo/disabled/destroyed/pilot state; не фабриковать уничтожение носителя/дронов или смерть пилотов. Применить штатные defeat consequences exactlyonce, без victory rewards, затем durable finished checkpoint до map/end по D-35-47. Сам label defeat не вводит новый gameover при живом carrier.
G-35-20T CLOSED. Продуктовый грумминг EP35 завершён; технические A/allowlist/schema drift проверяет owning implementation, это не новые user blockers. Implementation NOT STARTED, tests/build/native/product acceptance NOT RUN/OPEN.
Tests NOT RUN: SeriesTurnCap_PlayerDefeat; TimeoutPreservesActualDamageAndPilotState; Timeout_NoVictoryRewards; TimeoutSettlement_ExactlyOnce; TimeoutFinishedCheckpoint_DurableBeforeRelease; TimeoutReplayReceipt_NoReapply.
## D-35-50: завершённая группа больше не участвует в мире
Пользователь: «После завершённого боя убирать группу с карты».
После завершённого EP35 боя (win/loss, включая Draw и Timeout/SeriesTurnCap) участвовавшая pirate group становится retired: исчезает с карты, не патрулирует, не обнаруживает игрока и не может начать новый encounter. Это логическое завершение участия группы, не искусственное уничтожение: actual carrier hull/ammo и drone/pilot statuses сохраняются в settlement/history. Соседние группы не меняются; для них обычный перехват на resume сохраняется без иммунитета.
Retirement входит в exactly-once settlement receipt и durable finished checkpoint. До завершённого settlement, включая pending battle/recovery или отказ записи результата, navigable world не публикуется; повтор callback/Load не создаёт группу заново и не повторяет последствия. Continue pending по D-35-47 восстанавливает тот же бой с начала; retirement не служит выходом из незавершённого боя. A-35-20 и остаток G-35-15 о surviving world group закрыты этим решением; no-respawn сохраняется.
Acceptance / tests NOT RUN: WinLossDrawTimeout_SelectedGroupRetired; SurvivingHullAndPilotStateNotFabricated; RetiredGroupNeverCapturesAgain; NeighborGroupsRemainActive; RetirementAndReceiptExactlyOnce; PendingRecoveryDoesNotRetireEarly; FinishedCheckpointRestoresRetirement. UI использует authoritative retired state, а не локальное скрытие маркера.