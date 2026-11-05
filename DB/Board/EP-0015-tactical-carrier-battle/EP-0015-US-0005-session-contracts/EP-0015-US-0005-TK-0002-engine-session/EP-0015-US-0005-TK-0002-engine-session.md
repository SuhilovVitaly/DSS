---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0005-session-contracts
ticket: EP-0015-US-0005-TK-0002-engine-session
title: engine-session
stage: done
layer: engine
depends_on: ["EP-0015-US-0005-TK-0001-contracts"]
files_touched: 4
serves: [AC-02]
created: 2026-11-05
revision: 3
---

# engine-session

## Why
Авторитетная пошаговая сессия.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs` | new | Фазы, предложения, приказы, повторное ограничение, цели, incoming, снимок, отказы |
| `src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs` | new | Владение сессией, доступность, обработка запросов |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SessionTests.cs` | new | Отказы (все коды AC-02); без приказа — ИИ; повторное ограничение предложения; fire без цели; авто-залп |

## Public API after the change
Внутренние методы Engine для соединения.

## Implementation steps
1. Порт `PlayWing` (`play4.py`), но с проверками и отказами AC-02 вместо KeyError/игнорирования.
2. Сторона игрока — `goal.playerSide`.
3. Снимок собирается из состояния, без отдельной логики правил.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Engine.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-05

Add `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs` for pure projection of loaded options and current battle into the complete immutable Contracts model; it owns no RNG or phase transitions. Four code positions total (three Engine files + SessionTests). SimulationEngine facade serializes access with existing _worldStateLock and caches original command responses by ordinal CommandId + exact request signature. Replays return original receipts without effects; conflicting reuse is rejected. BattleId derives from start CommandId, avoiding any battle RNG use for identity. Move/fire check battle/revision/phase and all batch members before mutation. Exit is battle-scoped, revision-independent, as declared by TK-0001. Next-turn AI planning occurs immediately after salvo per Display §1; turn-stamped paths/shots and BeforeSalvoUnits preserve animation facts. World command/save guards remain the dependent TK-0003 slice; transport and UI wiring follow later.
## Execution evidence — 2026-11-05

AC-02 implemented: authoritative phases, both-side AI draws, untouched suggestions, clamp on every move, atomic batch rejection, typed errors, own-only loadout/targets, automatic no-target salvo and no player turn cap. Deep immutable options/state, before-salvo units and turn-stamped animation facts; log deltas and full journal. Receipt replay preserves original response without repeated side effects, identity conflicts rejected. No world defeat/state changes introduced; world guards are TK-0003.

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-session-01/ — build.log PASS (0 warnings/errors), format.log PASS, tests.log and tests/*.trx 274/274 TacticalBattle Engine cases PASS (29 new, including both complete seed-1 session modes). Self-review and git diff --check PASS; not independent review/native acceptance. Publication authorized by D-USER-14/15 and explicit override of D-USER-09/R-06; original restriction above superseded. Separate commit and immediate origin push, SHA recorded in ExecutionStatus.md.
