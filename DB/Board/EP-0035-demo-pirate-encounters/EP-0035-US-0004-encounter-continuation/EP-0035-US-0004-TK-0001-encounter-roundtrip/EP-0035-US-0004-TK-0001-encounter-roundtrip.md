---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0004-encounter-continuation
ticket: EP-0035-US-0004-TK-0001-encounter-roundtrip
title: Непрерывность угроз и преследования
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0035-US-0003-TK-0003-client]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04, AC-05]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Original planning baseline; implementation status and published commits: see current execution ledger.

# Непрерывность угроз и преследования

## Why

Сохранение преследования и результата встречи. Тикет 1/2 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0035-US-0004-encounter-continuation.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0035-US-0003-TK-0003-client. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.MapEvasion.cs` | Планируемый файл; создаётся в `EP-0035-US-0002-TK-0002-engine`, не текущая реализация. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.PirateGroups.cs` | Планируемый файл; создаётся в `EP-0035-US-0001-TK-0003-engine`, не текущая реализация. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs` | Планируемый файл; создаётся в `EP-0032-US-0007-TK-0002-campaign-state`, не текущая реализация. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0035Us0004Tk0001Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Именованные проверки этого тикета; не менять production из тестов. |

Разрешены только эти 4 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

No API change.

## Implementation steps

1. Сохранить группу, Patrol/Pursuing/InvestigatingLastKnown/ReturningToPatrol/Intercepted/Settled state, private lastKnownPosition/lastSeenMotionTime, patrolRouteId/resumeWaypointIndex, motion параметры, contact/encounter IDs и наблюдённое знание; валидировать ссылки и не допускать возрождения уничтоженной группы. Старые escape/cooldown timers не запускают отменённую логику.
2. Проверить checkpoints Patrol/Pursuing/InvestigatingLastKnown/ReturningToPatrol/Intercepted/Settled и однократный Escaped outcome. Load при том же motion time сохраняет lastKnown/waypoint, возможный перехват и уход, не заменяет знание текущей скрытой позицией игрока.
3. Два фиксированных seed/маршрута с пассажирами и собственным товаром: безопасный обход не пересекает pursuit zone, прямой путь допускает перехват. Пауза и speed partition не меняют результат.

## Served acceptance criteria

- **AC-01:** До/после загрузки совпадают группы, наблюдаемое состояние и результат ухода.
- **AC-02:** Нельзя сбросить преследование или воскресить уничтоженную группу простым сохранением и загрузкой.
- **AC-03:** С пассажирами и торговым товаром показаны безопасный обход и рискованный маршрут.
- **AC-04:** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Вклад этого тикета ограничен слоем `engine`; полный AC принимается после всей карты истории, включая реальный пользовательский путь. DTO/test scaffold сам по себе не закрывает AC.

## Out of scope

Полная верфь, другие носители, фракционная симуляция, пленные, тактическое отступление, сохранение внутри боя, универсальный квестовый движок и публикация. Не переписывать поставленный EP-0029, не ремонтировать несвязанные файлы и не расширять лимит пяти файлов молча.

## Invariants

- `DB/Documentation/01-Requirements/EngineRequirements.md`, «Архитектура и авторитет» / «Команды, события и воспроизводимость»: Engine владеет исходами/временем/RNG/деньгами; render не мутирует мир.
- `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`: асинхронный transport. `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs:17`: использовать единый settlement, не создавать второй inventory.
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:40`: на grounding writer Save21; переход к Save22 принадлежит EP-0032-US-0007. Последующие тикеты не назначают конкурирующие версии writer.
- Reject/duplicate не даёт частичных денег, груза, награды, опыта или повторного звука. Pause не двигает deadlines; calendar и motion units не смешиваются.
- Реализация gameplay-инвариантов — в owning engine ticket; клиентский/content/tooling тикет их не дублирует.

## Tests

Будущие тесты (ещё не реализованы/не запущены):

- `DemoEp0035Us0004Tk0001Tests.Pursuit_RoundTripsWithoutReset` — проверяет шаг 1 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0035Us0004Tk0001Tests.DestroyedGroup_DoesNotRespawn` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0035Us0004Tk0001Tests.DetourAndDirectRoute_HaveDifferentExposure` — проверяет шаг 3 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.

Запуск из `D:/DeepSpaceSaga/DSS`; создать уникальный run, ограничить **каждый process tree 300000 ms**, не продолжать после timeout/failure. Пример PowerShell для именованного test corpus:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0035-US-0004-TK-0001-encounter-roundtrip/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0035Us0004Tk0001Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build`. Format: `dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.MapEvasion.cs src/DeepSpaceSaga.Engine/SimulationEngine.PirateGroups.cs src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0035Us0004Tk0001Tests.cs`; тот же timeout wrapper и абсолютные stdout/stderr. Не запускать format для content-only. Длительный corpus делить по указанным полным IDs, не увеличивать cap. Native/manual сценарий: AC-01: До/после загрузки совпадают группы, наблюдаемое состояние и результат ухода. AC-02: Нельзя сбросить преследование или воскресить уничтоженную группу простым сохранением и загрузкой. AC-03: С пассажирами и торговым товаром показаны безопасный обход и рискованный маршрут. AC-04: Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Native запуск — только после проверки display layout, на меньшем мониторе с isolated profile/explicit index и `DSS_VALIDATION_SMALL_MONITOR=1`; иначе NOT RUN. Автотест не закрывает native, слуховую, независимую или продуктовую приёмку.

## Definition of Done

- Все шаги выполнены только в allowlist, тесты проверяют указанные исходы и ошибки.
- Served AC имеют evidence вклада этого слоя; story end-to-end/native/product gates не закрыты автоматически.
- Focused checks и owning build/format проходят либо конкретное исходное падение записано как блокер; timeout не PASS.
- Public API, units, persistence, idempotency и ограничения соблюдены; нет скрытого follow-up вне Board.
- Evidence в `D:/DeepSpaceSaga/artifacts/validation/<ticket>/<unique-run>/`, status/решения в Board, актуальные контракты в Documentation.
- Удалены только собственные disposable scripts/caches/results; сохранены необходимые ссылки evidence, нет мусора в корнях workspace/repo.

## Self-containment check

Prerequisites, allowlist, новые схемы/команды, concrete steps, критерии и named tests перечислены здесь. Для новых файлов указан создатель; их наличие до исполнения dependency не предполагается. Чтение источников допустимо для проверки drift, но новый продуктовый выбор или расширение files требует явной правки Board.

## D-35-17 — потеря контакта и возврат на патруль

D-35-17 (пользователь): «Для демо пусть долетают до последней известной позиции и, если не нашли игрока, возвращаются на патруль. Добавляй в тикеты».
После потери собственного сенсорного контакта пират идёт к последней действительно наблюдённой позиции игрока. Сенсор продолжает работать по пути. При достижении этой точки без нового контакта группа возвращается к патрулю; поискового круга, прохода или дополнительного таймера поиска нет. Текущие скрытые координаты/маршрут игрока и debug visibility не используются.
A-35-08: если сенсор вновь обнаружил игрока по пути или на возврате, группа снова переходит в Pursuing; это логическое рабочее допущение, не отдельная дословная команда пользователя.
A-35-09: возвращаться к сохранённой следующей точке прерванного патруля; конкретный return waypoint policy не был отдельно одобрен.
G-35-10 [CLOSED D-35-19]: capture radius = 20 км = 200 world units, одинаковый обоим, distance <= radius между центрами.
Supersession: прежние A-01 escape radius/escapeDuration/max pursuit duration больше не завершают эту погоню. Escaped — наблюдаемый outcome после достижения lastKnown без повторного контакта, а не конкурирующая AI state machine. Старые cooldown параметры не дают иммунитет от нового настоящего контакта; duplicate events предотвращаются stable contact/encounter identity.
Сохранить private lastKnown position/time, state, patrol route/resume waypoint и текущие motion параметры; валидировать ссылки и finite coords. Legacy escape fields не активируют отменённую логику.
Named tests: LoadLostContact_PreservesLastKnownNotCurrentPlayer; LoadReturning_PreservesPatrolWaypoint; PauseAndLoad_DoNotAdvanceRoute; ArrivalAfterLoad_NoSearchCircle; NoDuplicateEscapedOutcome.

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
## D-35-47: восстановление captured боя с начала
Пользователь: «Да, принимаем восстановление того же боя с начала после закрытия приложения или сбоя».
Durable prebattle pending checkpoint ДО tactical start: committed capture, fixed group/member/drone composition, exact player/company prebattle state, stable encounter/battleId и replay seed. Continue после close/crash запускает тот же бой с начала, не navigable world. Ordinary midbattle save/load/menu retreat запрещены; perturn resume не вводится.
Win/loss settlement exactly once, затем durable finished checkpoint до map/end. Failed write удерживает pending result/retry storage, не restart/reapply. Crash при atomic persistence даёт либо old durable pending battle replay, либо durable finished state. Intermediate damage persistence не обещается.
G-35-19 CLOSED. US0016 owns engine checkpoint, local-client durable storage и client recovery. Legacy aborted settlement не используется для ordinary EP35 unfinished battle; debug/tutorial context отдельно.
Tests NOT RUN: checkpoint-before-start; same composition/playerstate/seed replay; no navigable escape; failedwrite retryonly; atomicoldpending-orfinished; exactlyonce rewards/losses; no pending loss via autosave.
## D-35-50: завершённая группа больше не участвует в мире
Пользователь: «После завершённого боя убирать группу с карты».
После завершённого EP35 боя (win/loss, включая Draw и Timeout/SeriesTurnCap) участвовавшая pirate group становится retired: исчезает с карты, не патрулирует, не обнаруживает игрока и не может начать новый encounter. Это логическое завершение участия группы, не искусственное уничтожение: actual carrier hull/ammo и drone/pilot statuses сохраняются в settlement/history. Соседние группы не меняются; для них обычный перехват на resume сохраняется без иммунитета.
Retirement входит в exactly-once settlement receipt и durable finished checkpoint. До завершённого settlement, включая pending battle/recovery или отказ записи результата, navigable world не публикуется; повтор callback/Load не создаёт группу заново и не повторяет последствия. Continue pending по D-35-47 восстанавливает тот же бой с начала; retirement не служит выходом из незавершённого боя. A-35-20 и остаток G-35-15 о surviving world group закрыты этим решением; no-respawn сохраняется.
Acceptance / tests NOT RUN: WinLossDrawTimeout_SelectedGroupRetired; SurvivingHullAndPilotStateNotFabricated; RetiredGroupNeverCapturesAgain; NeighborGroupsRemainActive; RetirementAndReceiptExactlyOnce; PendingRecoveryDoesNotRetireEarly; FinishedCheckpointRestoresRetirement. UI использует authoritative retired state, а не локальное скрытие маркера.
### Current implementation scope
Existing retirement/recovery is supplied by US15/US16. This ticket adds saved private last-contact time and a separate player-observed location/time/velocity/pursuit sample, never the private AI target. Extend allowlist to PirateWorldData, PirateGroupsContracts, PirateComposition, MapEvasion and new engine observation partial plus tests. Validate finite fields and nonfuture times; absent additive fields remain compatible. Route comparison is owned by US5 and validated there. Tests run only after both US4 tickets.

Implementation complete: persisted independent observations and private last-contact timestamps, strict additive validation, last-known projection without hidden motion. Five new phase/atomic-load cases plus US15/16 retirement/replay regressions queued for story boundary. Build and scoped formatting PASS; tests NOT RUN at ticket boundary. Evidence us4-tk1-build.log under designated resume run.

Independent review found snapshot-dependent observation refresh. Fix refreshes established memory only at authoritative shared 250ms controller boundaries; initial acquisition still records immediately. Added one-shot vs 125ms snapshot partition regression after contact loss. Initial US4 Engine 95/95 PASS; this new regression remains queued until fix publication.

Story validation after review correction: Engine 96/96 PASS (all Ep0035), Client 25/25 PASS (all Ep0035). Evidence us4-reviewed-tests.log and us4-Client-tests.log under D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/. Route/cargo/passenger exposure acceptance remains owned by US5; native owner gate NOT RUN.
