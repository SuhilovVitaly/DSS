---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0003-world-battle
ticket: EP-0035-US-0003-TK-0004-localized-feedback
title: RU/EN условия, результаты и причины отказа
stage: approved
approval_kind: workflow
implementation_status: implemented-tests-pending
layer: content-data
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0035-US-0003-TK-0002-engine]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# RU/EN условия, результаты и причины отказа

## Why

Перехват запускает бой с конкретной группой. Тикет 3/4 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0035-US-0003-world-battle.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0035-US-0003-TK-0002-engine. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Существующий locale dictionary: `src/DeepSpaceSaga.Client/Data/Locale/Russian.json:1`. | Только ключи данной истории, форматные placeholders и тексты отказов. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Существующий locale dictionary: `src/DeepSpaceSaga.Client/Data/Locale/English.json:1`. | Только ключи данной истории, форматные placeholders и тексты отказов. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0035Us0003Tk0004Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Parity/reason coverage реальных словарей. |

Разрешены ровно 3 файла. Не менять численные правила Engine и не переводить persistent IDs.

## Public API after the change

No API change. Существующие словари Russian/English; язык в Settings — Russian или English, не новые ru/en IDs.

## Implementation steps

1. По контракту этой истории и API предыдущих тикетов добавить ключи для условий, стоимости/срока, доступности, pending, confirmed результата, каждого stable rejection code и empty state. Использовать одинаковые ключи/параметры в Russian.json и English.json.
2. Дать короткие конкретные RU/EN тексты: что произошло, почему действие недоступно и какое действие возможно. Не показывать class names, IDs и implementation jargon вместо объяснения.
3. Проверить все keys, используемые следующим UI тикетом, и отсутствие неизвестных placeholders; не менять чужие переводы. Сюжетные тексты, если есть, остаются в owning dialogue content.

## Served acceptance criteria

- **AC-01:** Состав врага соответствует группе мира, игрок использует реальное крыло и расходники.
- **AC-02:** Исход изменяет именно эту группу и компанию; повторное взаимодействие не запускает заново уже уничтоженного противника.
- **AC-03:** Проверены загрузка до встречи, возвращение после боя и запрет бесплатного выхода вместо отступления.
- **AC-04:** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Вклад этого тикета ограничен слоем `content-data`; полный AC принимается после всей карты истории, включая реальный пользовательский путь. DTO/test scaffold сам по себе не закрывает AC.

## Out of scope

Полная верфь, другие носители, фракционная симуляция, пленные, тактическое отступление, сохранение внутри боя, универсальный квестовый движок и публикация. Не переписывать поставленный EP-0029, не ремонтировать несвязанные файлы и не расширять лимит пяти файлов молча.

## Invariants

- `DB/Documentation/01-Requirements/EngineRequirements.md`, «Архитектура и авторитет» / «Команды, события и воспроизводимость»: Engine владеет исходами/временем/RNG/деньгами; render не мутирует мир.
- `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`: асинхронный transport. `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs:17`: использовать единый settlement, не создавать второй inventory.
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:40`: на grounding writer Save21; переход к Save22 принадлежит EP-0032-US-0007. Последующие тикеты не назначают конкурирующие версии writer.
- Reject/duplicate не даёт частичных денег, груза, награды, опыта или повторного звука. Pause не двигает deadlines; calendar и motion units не смешиваются.
- Реализация gameplay-инвариантов — в owning engine ticket; клиентский/content/tooling тикет их не дублирует.

## Tests

Будущие named tests (NOT RUN):

- `DemoEp0035Us0003Tk0004Tests.StoryKeysExistInBothLocales` — все ключи экрана и причины отказа имеют RU/EN значения.
- `DemoEp0035Us0003Tk0004Tests.FormatArgumentsMatch` — placeholders и типы форматируемых значений одинаковы.
- `DemoEp0035Us0003Tk0004Tests.NoRawReasonCodeLeaks` — known rejection code отображается объяснением, не техническим кодом.

Из `D:/DeepSpaceSaga/DSS`: создать уникальный абсолютный `D:/DeepSpaceSaga/artifacts/validation/EP-0035-US-0003-TK-0004-localized-feedback/<UTC-run>/`; команда `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0035Us0003Tk0004Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`. Запускать Start-Process с redirected stdout/stderr в run, WindowStyle Hidden, WaitForExit(300000); при timeout taskkill /PID <pid> /T /F и не объявлять PASS. Content-only: format не нужен; owning build `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build` с тем же process-tree cap. Реальную читаемость проверяет UI тикет и итоговая EP-0040-US-0005; unit parity не закрывает native/product gate.

## Definition of Done

- Все шаги выполнены только в allowlist, тесты проверяют указанные исходы и ошибки.
- Served AC имеют evidence вклада этого слоя; story end-to-end/native/product gates не закрыты автоматически.
- Focused checks и owning build/format проходят либо конкретное исходное падение записано как блокер; timeout не PASS.
- Public API, units, persistence, idempotency и ограничения соблюдены; нет скрытого follow-up вне Board.
- Evidence в `D:/DeepSpaceSaga/artifacts/validation/<ticket>/<unique-run>/`, status/решения в Board, актуальные контракты в Documentation.
- Удалены только собственные disposable scripts/caches/results; сохранены необходимые ссылки evidence, нет мусора в корнях workspace/repo.

## Self-containment check

История, контрактные причины и следующий экран доступны по карте этой story. Allowlist состоит из двух существующих словарей и одного теста; игровые правила и frontend wiring принадлежат соседним тикетам. Новые ключи согласовать с их объявленным API, а не с догадкой о скрытом состоянии.

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

Execution: RU/EN warning, explicit continue, pending and failure reasons added. JSON parse PASS; tests scheduled at US3 boundary.
