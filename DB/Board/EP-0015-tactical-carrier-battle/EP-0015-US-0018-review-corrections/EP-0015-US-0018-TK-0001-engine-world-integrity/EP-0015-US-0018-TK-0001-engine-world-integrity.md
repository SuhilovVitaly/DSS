---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0018-review-corrections
ticket: EP-0015-US-0018-TK-0001-engine-world-integrity
title: engine-world-integrity
stage: done
layer: engine
depends_on: ["EP-0015-US-0017-TK-0004-board-sync"]
files_touched: 8
serves: [AC-01]
created: 2026-11-06
revision: 1
---

# engine-world-integrity

## Why
Находки ревью №2, №5, №6, №7, №8: целостность мира Engine при остановке службой безопасности, во время тактического боя и при миграции.

## Decisions
D-USER-REVIEW-01 (история). Правила боя и механика безопасности станции не меняются.

## Assumptions
Baseline `codex/ep-0015-tactical-carrier-battle @ b3303748`. Код ответа остановки — существующий `player_destroyed`; код отказа боя — существующий `tactical_battle_active`. Новых DTO, кодов или ключей локали не требуется.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | `CaptureSaveStateCore:1082`, `BuildSnapshot:663`, `ReceiveCommand:152` | №2: `ThrowIfStoppedByStationSecurity()` после `UpdateStationSecurity`; №6: повторно применить `_stationSecurityStop` к `clockState`/`gameTimeMs` после `UpdateStationSecurity`; №8: только `_commandGate`, чтение флага боя |
| `src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs` | `_tacticalBattleSession` под `_worldStateLock` | №8: флаг активного боя, записываемый под `_commandGate` при старте и выходе |
| `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs` | `ReceiveDialogueCommand:15` бросает | №5: пометить команду обработанной и выдать событие-отказ `tactical_battle_active` |
| `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs` | `ProjectWorld:33` | №7: только если тест найдёт висячую ссылку |
| `tests/DeepSpaceSaga.Engine.Tests/StationSecurityStopTests.cs` | new | №2, №6 |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/WorldGuardTests.cs` | existing | №5, №8 |
| `tests/DeepSpaceSaga.Engine.Tests/SaveMigration20Tests.cs` | existing | №7 |
| `DB/Documentation/04-Engineering/TacticalBattle.md` | contract | Только если меняется описанное поведение отказов |

## Public API after the change
Без изменений сигнатур.

## Out of scope
Находки №9, №10; клиент; правила боя.

## Invariants
Engine владеет миром и исходами; мир и байты сохранения во время боя не меняются (US-0005 AC-03); порядок замков `_worldStateLock` → `_commandGate`.

## Tests
Owning project: tests/DeepSpaceSaga.Engine.Tests. TDD: каждый тест сначала воспроизводит дефект на baseline. Сборка, scoped format, `git diff --check`. Process-tree cap 300000 ms, результаты — в `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/<unique-run>/`.

## Definition of Done
AC-01 выполнен и проверен; лимит files_touched соблюдён; результаты записаны в карточку; отдельный коммит тикета; push в `origin` (публикация разрешена пользователем 2026-11-06, D-USER-14/15).

## Execution evidence — 2026-11-06

Baseline `82ce2cb5`. Изменены 6 из 8 разрешённых файлов; `SaveMigration20.cs` и `TacticalBattle.md` не менялись (висячих ссылок нет; описанное в контракте поведение отказов не меняется).

| № | Изменение | Тест (красный на baseline → зелёный) |
|---|---|---|
| 2 | `SimulationEngine.cs` `CaptureSaveStateCore`: `ThrowIfStoppedByStationSecurity()` сразу после `UpdateStationSecurity` | `StationSecurityStopTests.Save_that_triggers_the_station_security_stop_is_refused`, `...Quicksave_that_triggers_the_station_security_stop_writes_no_file` (baseline: «No exception was thrown») |
| 6 | `SimulationEngine.cs` `BuildSnapshot`: после `UpdateStationSecurity` при `_stationSecurityStop` — `ApplyStationSecurityStop()`, `clockState`/`gameTimeMs` берутся из часов остановки, проекция объектов идёт по ним | `StationSecurityStopTests.Snapshot_that_stops_inside_station_security_publishes_the_stop_clock` (baseline: `CurrentSpeed` Speed2 вместо Speed0) |
| 5 | `SimulationEngine.Dialogue.cs` `ReceiveDialogueCommand`: во время боя не бросает; `RejectDialogueCommandDuringTacticalBattle` добавляет ID в `ProcessedCommandIds` и выдаёт событие `tactical_battle_active` (повтор — `dialogue_duplicate`) | `WorldGuardTests.DialogueCommandDuringBattleIsProcessedAsARejectionWithoutThrowing`; в `QueuedCommandsLoadTravelAndAccelerationCannotMutateBattleWorld` ожидание исключения заменено проверкой события (baseline: `InvalidOperationException: tactical_battle_active`) |
| 8 | `SimulationEngine.cs` `ReceiveCommand` берёт только `_commandGate`; `SimulationEngine.TacticalBattle.cs`: флаг `_tacticalBattleCommandBlock` и время отказа пишутся под `_commandGate` при старте/выходе боя (под `_worldStateLock`); команды мира во время боя копятся в `_tacticalBattleRefusedCommands` и получают `Rejected`/`tactical_battle_active` в начале следующего `ApplyPendingCommands` под замком мира; очистка списка в `LoadScenario` | `WorldGuardTests.ReceiveCommandDoesNotWaitForTheWorldLockAndStillRefusesWorldCommandsInBattle` (baseline: «ReceiveCommand waited for _worldStateLock»); `WorldCommandsAreRefusedEvenIfBattleExitsBeforeTheirNextTick` остаётся зелёным |
| 7 | Код не менялся | `SaveMigration20Tests.MigratedSave19WithStationDialogueAndIncidentLoadsWithoutReferencesToTheRemovedHostile` — зелёный и на baseline: Save19 с удаляемым врагом `SPC-MUKHA-01`, активным диалогом и инцидентом у станции загружается, сохранение не содержит `MUKHA` и проходит round-trip |

Почему №8 не вызывает `RecordCommandResult` в `ReceiveCommand`: он пишет `_commandResults` (очищается в `BuildSnapshot` под замком мира) и в `RememberResult` перебирает `_objects`; без `_worldStateLock` это гонка. Отказ поэтому откладывается до ближайшего прохода мира; время отказа — `_processedWorldTimeMs` на старте боя (мир на паузе весь бой). Инвариант US-0005 AC-03 сохранён: во время боя сохранение запрещено, после выхода мир и Save20 совпадают байт в байт, кроме журналов команд (`CommandReceipts`, `DialogueState.ProcessedCommandIds`/`NextEventId`) — как и для уже существовавшего отказа ранее поставленных диалоговых команд.

Проверки (артефакты: `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/us0018-tk0001-20261106-red/` и `.../us0018-tk0001-20261106-green/`):
- Red: целевые 23 теста — 6 failed / 17 passed (`red.trx`, `test.log`).
- `dotnet build DeepSpaceSaga.sln` — 0 warnings, 0 errors.
- Scoped `dotnet format whitespace --verify-no-changes --include <6 файлов>` — PASS; `git diff --check` — PASS.
- Целевые StationSecurityStop/WorldGuard/SaveMigration20 — 23/23 PASS.
- Engine.Tests (2157), разбиение с cap 300000 ms: без TacticalBattle и correctness-корпусов 1651/1651 PASS; TacticalBattle без SeriesParity 386/387; SeriesParity 4/5; FullMap facts по точным ID 3/3 PASS.
- Падения вне тикета: `ContentSyncTests.ExportCheckRejectsCorruptionWithoutOverwritingFixture` — на машине нет `pwsh`; `SeriesParityTests.Raid` 856 vs 857 — известное расхождение открытого EP-0015-US-0016-TK-0001.
- Не завершены в cap: классы `ClusterCorrectnessCorpusTests` (50), `SystemCorrectnessCorpusTests` (49) и 12 случаев `FullMapCorrectnessCorpusTests.HundredSeedFullMapMatrix` — каждый класс превышает 300000 ms (`test-p4-*.log`); прогон по точным ID не выполнялся. Затронутые ветки работают только при активном бое или остановке службой безопасности.
