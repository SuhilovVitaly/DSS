---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0012-pirate-squadrons
ticket: EP-0035-US-0012-TK-0001-contracts
title: Контракт группы и настройки появления
stage: draft
implementation_status: implemented-tests-pending
layer: contracts
matching_test_project: tests/DeepSpaceSaga.Contracts.Tests
depends_on: [EP-0035-US-0011-TK-0005-generation-engine]
files_touched: 4
serves: [AC-01]
created: 2026-11-17
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Original planning baseline; implementation status and published commits: see current execution ledger.

# Контракт группы и настройки появления

## Why and decisions
[История](../EP-0035-US-0012-pirate-squadrons.md) — bounded вклад AC-01. D-35-21…25 и A/G истории применяются; продуктовые G закрыты итоговыми решениями, техническая готовность требует проверки prerequisites/allowlists по текущим source owners.

## Code context and allowlist
- src/DeepSpaceSaga.Contracts/PirateSquadContracts.cs — owning prerequisite/new planned extension; проверить фактический путь перед исполнением.
- src/DeepSpaceSaga.Contracts/PirateGroupsContracts.cs — owning prerequisite/new planned extension; проверить фактический путь перед исполнением.
- src/DeepSpaceSaga.Contracts/DebugTuning.cs — существующий owning source, только описанный scope.
- tests/DeepSpaceSaga.Contracts.Tests/Demo/Ep0035Us12Tk1Tests.cs — новый matching test.

Grounding: src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs:23 — DemoPirateGroup имеет один LoadoutId. TacticalBattle/Content/TacticalBattleDefinitions.cs:52 — BattleSide имеет один Carrier. TacticalBattle/TacticalBattleAssembly.cs:28 — сборка читает side.Carrier. TacticalBattle/TacticalBattleState.cs:100–102 — Midpoint/Carrier(side) опираются на одиночного носителя. TacticalBattle/TacticalBattle.cs:54–56 — outcome проверяет Carrier(side). TacticalBattle/Ai/TacticalCommander.cs:10 — защита выбирает Carrier(side). Client/UI/DebugConsole/DebugCommandCatalog.cs:6 — регистрация команд.

## Public API / implementation steps
Ввести stable groupId и member carrierId; публичное наблюдение не содержит hidden drone loadout. Контракт настройки вероятности 0..100 и текущего значения. Одна группа имеет один token и один discovery flag.

Сначала сверить prerequisite DTO/schema; затем реализовать описанную часть только в allowlist. Если слой требует шестой файл, обновить Board decomposition до изменения production. Новые имена — проект, не утверждение существующего API.

## Invariants
Один layer, Engine authoritative; stable group/member/drone IDs. Сенсор200/240/264км, capture20км и один automatic report/group сохраняются. Fog off не discovery; нет reroll. Не считать side равным carrier. Два enemy Логова плюс player carrier. Открытые G не заменять случайным решением.

## Tests and validation plan
NOT RUN. Добавить focused Ep0035Us12Tk1Tests: positive two-Lair path; invalid/duplicate identity rejected; legacy singleton compatibility; repeat/Load idempotence для изменяемой части. Проверить конкретный AC-01 и отсутствие утечки hidden loadout. Для spawn — fixed seed,0/100% boundaries,10% default, rejected NaN/out-of-range без mutation; для battle — уничтожение первого carrier при живом втором не завершает сторону; для client — выбор каждого ID и3 carrier snapshot.
Запуск после реализации из DSS: dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj -c Release --filter FullyQualifiedName~Ep0035Us12Tk1Tests --results-directory <absolute-run>/tests. Output root D:/DeepSpaceSaga/artifacts/validation/ep0035-squadrons/<unique-run>; timeout300000ms, не PASS. Build owning project и format изменённых C#; JSON loader tests. Native UI/product отдельно, не заменяются unit tests.

## Out of scope and Definition of Done
Другие layers,3/mixed enemy squad и самостоятельный выбор orphan policy вне scope. Для завершения реализации требуется evidence: AC проверены, stable save/identity и regression singleton соблюдены. Сейчас tests/native/product NOT RUN/OPEN; закрытые продуктовые G не переоткрывать. US0006 документирует фактическую реализацию последней. Никакого commit/push/build при текущем grooming.


## Текущее расширение эскадр — D-35-21…26
D-35-22: одна фишка группы, Vmax=min operational Vmax её носителей.
D-35-23: все носители и дроны эскадры одновременно на одном поле.
D-35-24: шанс эскадры10%, меняется debug console. Предлагаемый синтаксис pirate_squad_chance [0..100], без аргумента current; help/валидация обязательны.
D-35-25 supersedes раннее2–3/mixed DEMO: «так по поводу боя. пока эскадры только два логова и оба выводим в бой». Ровно два Логова, по6 дронов =12, плюс носитель игрока (всего3 carriers на поле). Танкред остаётся одиночным типом. Без повреждений squad Vmax=1.10 frozen start player maximum, operational min сохраняется.
D-35-26: «при уничтожении всех носителей дроны получают статус выведены из строя как при стрельбе». Гибель первого из двух не отключает дроны. После последнего носителя стороны все её surviving drones становятся существующим Disabled, speed0; Destroyed остаётся Destroyed, отключение не означает смерть пилота. Симметрично для player и pirate side. Atomic/idempotent transition, затем outcome и skip последующих shots по existing invariants.
Grounding: TacticalBattleState.cs:9 TacticalDroneStatus Air/Disabled/Destroyed; TacticalBattleSalvo.cs:237–239 shot disable задаёт Disabled,Speed0,statistics; :152–153 skip disabled/destroyed shooter.
A-35-11: независимый seeded draw на каждое создание новой группы,90% singleton/10% squad; chance действует только future creation, не sensor/load; RNG/result сохраняются без reroll.
G-35-12 CLOSED D-35-32: одна точка группы, max operational radius без сложения; report layout — A.
G-35-13 lifetime CLOSED D-35-40; ordinary group count4 CLOSED D-35-36; singleton weights CLOSED D-35-35; initial/norespawn CLOSED D-35-34.
G-35-14 CLOSED D-35-26; orphan policy больше не открыта.
G-35-15 normal retreat CLOSED/excluded D-35-46: exit onlywin/loss; crash recovery CLOSED D-35-47.
Ранние2–3/mixed варианты, выбор2vs3 и orphan proposals — superseded; не scope демо и не текущие G.
D-35-26 tests NOT RUN: FirstCarrierDeath_LeavesDronesActive; LastCarrierDeath_DisablesSurvivors; DestroyedDrone_Unchanged; Disable_NoPilotDeath; PlayerAndEnemySymmetric; RepeatedResolution_Idempotent; SubsequentShot_ShooterDisabled. Reuse existing Disabled semantics and statistics; never fabricate Hull damage to trigger disable.
## D-35-32: общая точка и лучший действующий сенсор эскадры
Пользователь: «Да, принимаем одну общую точку эскадры и лучший действующий сенсор без сложения радиусов».
Одна authoritative group position — центр map token. Её используют world range, bearing, ETA по D-35-29 и capture<=20км по D-35-19; tactical позиции отдельных carrier не становятся отдельными map contacts. Все расчёты в world units, zoom/DPI/window не меняют distance.
Squad sensor range=max operational radius её carriers, НЕ сумма. Два исправных Логова=>200км, один неработающий sensor=>200км от другого; оба неработающие=>0. Нулевая дальность означает отсутствие detection coverage, а не detection при coincident point. Не суммировать площадь/радиусы и не создавать дополнительный sensor origin для tactical member positions.
G-35-12 geometry/reference/sensor aggregation CLOSED. A-report-shape: один отчёт группы с двумя точными типами и одним approximate aggregate total; два automatic окна по членам недопустимы вследствие D-35-21 one/group. UI layout списка членов — implementation choice.
G-35-04 simultaneous batching CLOSED D-35-33: одна сводка всех новых групп текущего observation step; сортировка/selection/scroll — A.
Tests NOT RUN: TwoLairs_Max200NotSum400; OneSensorDisabled_UsesOther; AllSensorsDisabled_NoCoverage; GroupReference_SharedByRangeBearingEtaCapture; MemberTacticalPositions_NotMapContacts; CaptureAndSensorIndependentOfZoom.
## D-35-34: все группы на старте, без возрождения
Пользователь: «Да, все группы создаются на старте кампании, без возрождения в демо».
Ordinary pirate world groups материализуются seeded при New Game: stable group/member IDs, типы, position и patrol route существуют и движутся до player observation. Sensor detection не создаёт world group; он отдельно материализует скрытую light/heavy composition по D-35-13/27 и формирует доклад. Не путать эти моменты RNG.
В демо нет respawn. Save/Load/revisit/уничтожение группы не создают новые группы/копии. Initial group draw использует squad chance10% (или допустимый override при старте); уже созданные группы не меняются после console command. G-35-13 initial cadence CLOSED; lifetime/pre-NewGame CLOSED D-35-40: app memory only, fog немедленно и NewGame/Load, chance только следующий NewGame; restart defaults true/10%. Conditional singleton50/50 утверждено D-35-35; default45/45/10.
Region/safe routes исходного US1 — A-baseline. «Два обычных состава» НЕ количество world groups: ordinary group count теперь4 по D-35-36. Проверить safe start station separation/route bypass. Position/route не скрытое знание player или pirate AI; fog off не acquisition.
Deferred EP39 integration note (не blocker EP35): EP39-US0003-TK0003:52 планирует story-gated captain; пользователь вынес сюжет из текущего грумминга. EP39 не изменён; варианты появления не утверждены.
Tests NOT RUN: NewCampaign_AllOrdinaryGroupsExistBeforeObservation; UndetectedGroups_MoveOnPatrol; FirstAcquisitionDoesNotSpawnWorldGroup; FirstAcquisition_MaterializesOnlyHiddenComposition; DestroyedGroup_NoRespawn; LoadRevisit_NoDuplicates; ConsoleChange_DoesNotMutateExistingGroups; SameCampaignSeed_ReproducesInitialGroups.
## D-35-35: распределение45/45/10
Пользователь: «Да, принимаем45/45/10».
Default initial world group draw: singleton Логово45%, singleton Танкред45%, squad2xЛогово10%. При изменении squadChance=p условный выбор singleton остаётся50/50, поэтому вероятности (1-p)/2,(1-p)/2,p. Речь о независимом seeded выборе для каждой группы при старте кампании, не о точной квоте групп.
p=0 =>50/50/0; p=1 =>0/0/100%. Не повторять draw на sensor acquisition, Load/revisit. Сохранять stable chosen group/member identities; separate hidden light/heavy slot draws50/50 при первом acquisition не меняют carrier type.
G-35-13 singleton weights CLOSED. Lifetime console override CLOSED D-35-40; ordinary world group count4 утверждён D-35-36. Фраза старого плана «два обычных состава» означает два типа/набора, не одобренное число spawn groups; не подставлять автоматически две группы.
Tests NOT RUN: Defaults45_45_10; ConditionalSingletonHalfForChangedP; ChanceZero_NoSquads; ChanceHundred_OnlyTwoLairSquads; FixedSeed_KnownChoiceSequence; LoadRevisit_NoTypeReroll; IndependentGroups_NotExactQuota. Без flaky frequency tests.
## D-35-36: четыре ordinary группы на демо-кластер
Пользователь: «Да, берём четыре обычные группы на демо-кластер. Добавляй в тикеты».
Создать ровно4 ordinary world groups при старте кампании. Каждая независимо выбирается по D-35-35 (45/45/10 default); squad2xЛогово считается ОДНОЙ группой, а не двумя. Поэтому четыре группы не означает четыре carrier и не задаёт квоту типов. No respawn, Load/revisit не увеличивают число initial instances.
G-35-13 total ordinary group count CLOSED. A-35-16: named finale captain — отдельная сюжетная встреча EP39, не входит в4 ordinary; его initial existence/story activation deferred EP39, вне EP35 grooming; не утверждать пятую группу или способ появления.
Количество станций/география и ограничение выбора маршрутов пользователем пока обсуждаются; нового D о сокращении станций нет. Safe initial locations/не на стартовой станции/доступный обход остаются прежними A до конкретной балансировки.
Tests NOT RUN: NewCampaign_ExactlyFourOrdinaryGroups; SquadCountsAsOneGroup; FourIndependentTypeDraws_NoQuota; LoadRevisit_NoAdditionalGroups; DefeatedGroup_NotRespawned; SafeInitialPositions_RespectConfiguredRegion.
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


## Execution 2026-11-17
Implemented stable member carrier IDs under one group token, without drone inventory. Debug settings DTO declares normal fog=true and squad chance10%; application/lifetime validation belongs to later engine/client tickets. Dependency is published US1-TK1 and US11-TK1 contracts; group engine follows these schemas. Build/format/diff PASS; evidence D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/us12-tk1-build.log. Tests NOT RUN until US12 boundary.
