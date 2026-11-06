---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0018-review-corrections
ticket: EP-0015-US-0018-TK-0001-engine-world-integrity
title: engine-world-integrity
stage: todo
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
