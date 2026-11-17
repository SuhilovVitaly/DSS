---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0013-squad-battle-assembly
ticket: EP-0035-US-0013-TK-0003-assembly-engine
title: Сборка всех участников эскадры
stage: draft
implementation_status: implemented-tests-pending
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0035-US-0013-TK-0002-schema-engine]
files_touched: 5
serves: [AC-03]
created: 2026-11-17
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Сборка всех участников эскадры

## Why and decisions
[История](../EP-0035-US-0013-squad-battle-assembly.md) — bounded вклад AC-03. D-35-21…25 и A/G истории применяются; продуктовые G закрыты итоговыми решениями, техническая готовность требует проверки prerequisites/allowlists по текущим source owners.

## Code context and allowlist
- src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs — существующий owning source, только описанный scope.
- src/DeepSpaceSaga.Engine/TacticalBattle/OwnedBattleAssembly.cs — существующий owning source, только описанный scope.
- src/DeepSpaceSaga.Engine/SimulationEngine.WorldEncounterBattle.cs — owning prerequisite/new planned extension; проверить фактический путь перед исполнением.
- src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs — существующий owning source, только описанный scope.
- tests/DeepSpaceSaga.Engine.Tests/Demo/Ep0035Us13Tk3Tests.cs — новый matching test.

Grounding: src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs:23 — DemoPirateGroup имеет один LoadoutId. TacticalBattle/Content/TacticalBattleDefinitions.cs:52 — BattleSide имеет один Carrier. TacticalBattle/TacticalBattleAssembly.cs:28 — сборка читает side.Carrier. TacticalBattle/TacticalBattleState.cs:100–102 — Midpoint/Carrier(side) опираются на одиночного носителя. TacticalBattle/TacticalBattle.cs:54–56 — outcome проверяет Carrier(side). TacticalBattle/Ai/TacticalCommander.cs:10 — защита выбирает Carrier(side). Client/UI/DebugConsole/DebugCommandCatalog.cs:6 — регистрация команд.

## Public API / implementation steps
Создать одно поле: один player carrier плюс два Логова и все их12 дронов из persisted состава US11. Дать независимые hull/identity/ownership, collision-free starts. Не копировать один loadout дважды и не генерировать заново на входе. Размещение предложить как A с bounds validation, не новое решение пользователя.

Сначала сверить prerequisite DTO/schema; затем реализовать описанную часть только в allowlist. Если слой требует шестой файл, обновить Board decomposition до изменения production. Новые имена — проект, не утверждение существующего API.

## Invariants
Один layer, Engine authoritative; stable group/member/drone IDs. Сенсор200/240/264км, capture20км и один automatic report/group сохраняются. Fog off не discovery; нет reroll. Не считать side равным carrier. Два enemy Логова плюс player carrier. Открытые G не заменять случайным решением.

## Tests and validation plan
NOT RUN. Добавить focused Ep0035Us13Tk3Tests: positive two-Lair path; invalid/duplicate identity rejected; legacy singleton compatibility; repeat/Load idempotence для изменяемой части. Проверить конкретный AC-03 и отсутствие утечки hidden loadout. Для spawn — fixed seed,0/100% boundaries,10% default, rejected NaN/out-of-range без mutation; для battle — уничтожение первого carrier при живом втором не завершает сторону; для client — выбор каждого ID и3 carrier snapshot.
Запуск после реализации из DSS: dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~Ep0035Us13Tk3Tests --results-directory <absolute-run>/tests. Output root D:/DeepSpaceSaga/artifacts/validation/ep0035-squadrons/<unique-run>; timeout300000ms, не PASS. Build owning project и format изменённых C#; JSON loader tests. Native UI/product отдельно, не заменяются unit tests.

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
## D-35-49: истечение лимита ходов — поражение игрока
Пользователь ответил «да» на вопрос «Истечение лимита ходов тоже считаем поражением?».
EP35 campaign outcome/UI считает existing Timeout/SeriesTurnCap поражением игрока, как Draw по D-35-48. Сохранить actual hull/ammo/disabled/destroyed/pilot state; не фабриковать уничтожение носителя/дронов или смерть пилотов. Применить штатные defeat consequences exactlyonce, без victory rewards, затем durable finished checkpoint до map/end по D-35-47. Сам label defeat не вводит новый gameover при живом carrier.
G-35-20T CLOSED. Продуктовый грумминг EP35 завершён; технические A/allowlist/schema drift проверяет owning implementation, это не новые user blockers. Implementation NOT STARTED, tests/build/native/product acceptance NOT RUN/OPEN.
Tests NOT RUN: SeriesTurnCap_PlayerDefeat; TimeoutPreservesActualDamageAndPilotState; Timeout_NoVictoryRewards; TimeoutSettlement_ExactlyOnce; TimeoutFinishedCheckpoint_DurableBeforeRelease; TimeoutReplayReceipt_NoReapply.

## Execution scope 2026-11-17
Both diagnostic and owned assemblies iterate carrier collections, preserve explicit drone origin and snapshot it (scope expands to TacticalBattleSnapshotBuilder). World scenario builder consumes only persisted composition and stable IDs; actual begin calls it in US3TK2. Technical placement: enemy carriers x850 at y150/450 for squad, drones bounded separated rows x700/760; singleton center250. Player keeps existing deployment. No reroll of equipment, no duplicated loadout. Added actual generated/saved squad through OwnedBattleAssembly test, checking12enemy drones/3carriers/ownedammo and seeded repeatability.
Build PASS us13-tk3-build.log under D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/. Tests deferred to US13 boundary.
