---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0018-review-corrections
ticket: EP-0015-US-0018-TK-0003-battle-exit-recovery
title: battle-exit-recovery
stage: todo
layer: client
depends_on: ["EP-0015-US-0018-TK-0002-ship-destroyed-screen"]
files_touched: 4
serves: [AC-03]
created: 2026-11-06
revision: 1
---

# battle-exit-recovery

## Why
Находки ревью №3 и №4: окно боя не закрывается при `NoBattle`/`WrongBattle`/`Unavailable`; отказ `AlreadyActive` с живым снимком отбрасывается, осиротевший бой движка блокирует мир.

## Decisions
D-USER-REVIEW-01 (история). Протокол и Engine не меняются: отказы `AlreadyActive`/`WrongBattle` уже несут живой снимок, `GetTacticalBattleStateAsync` уже есть.

## Assumptions
Baseline после TK-0002.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` | `ExitAsync:58`, `Submit:72` | №3: сверка с живым боем, очистка при отсутствии боя, один повтор при `WrongBattle`; №4: принимать живое состояние из отказов `AlreadyActive`/`WrongBattle` |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | `:819` выход только при `Flow.State is not null` | Всегда вызывать `ExitAsync` |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleEndTests.cs` | existing | №3 |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs` | existing | №4, закрытие подготовки с осиротевшим боем |

## Public API after the change
Без изменений сигнатур.

## Out of scope
Engine, протокол, правила боя.

## Invariants
Engine владеет состоянием боя; клиент принимает только снимки, пришедшие через сессию; мир после выхода идентичен миру до F8.

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. TDD. Сборка, scoped format, `git diff --check`. Process-tree cap 300000 ms, результаты — в `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/<unique-run>/`.

## Definition of Done
AC-03 выполнен и проверен; лимит files_touched соблюдён; результаты записаны в карточку; отдельный коммит тикета; push в `origin` (D-USER-14/15).
