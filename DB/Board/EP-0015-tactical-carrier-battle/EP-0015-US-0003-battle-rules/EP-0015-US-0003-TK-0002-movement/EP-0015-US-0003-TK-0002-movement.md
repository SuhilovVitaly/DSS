---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0003-battle-rules
ticket: EP-0015-US-0003-TK-0002-movement
title: movement
stage: done
layer: engine
depends_on: ["EP-0015-US-0003-TK-0001-battle-assembly"]
files_touched: 2
serves: [AC-02]
created: 2026-11-05
revision: 3
---

# movement

## Why
Движение Combat §5.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleMovement.cs` | new | Движение по подшагам через Motion |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/MovementTests.cs` | new | Позиции trace-seed-1 |

## Public API after the change
`Move(battle)`.

## Implementation steps
1. Курс нормализуется только в конце.
2. Скорость дрона = скорость приказа.

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

## Precision assumption before validation — 2026-11-05

Independent trace orders and positions are rounded to six decimals (D-USER-15). Movement-slice tests reset to each source endpoint, avoiding cumulative uncertainty; input half-micro-unit speed/course/turn errors propagate to at most 3 micro-units XY and 1.5 micro-degrees at source speeds. This slice check is separate from the later full unrounded Engine trace/golden-battle integration. Move computes all active paths before mutating state, clamps orders through current Motion limits and consumes no RNG.
## Execution evidence — 2026-11-05

Move returns immutable start/substep paths, applies current Motion speed/turn limits, normalizes course only at the final substep and records speed/absolute turn. All paths validate before any state update. Disabled/destroyed drones and carriers stay stationary; no RNG consumed.

`D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-movement-01/`: solution build 0 warnings/errors; Engine TacticalBattle 104/104 (3 new), full Motion 1376/1376 including source guard; scoped format/diff PASS. All 19 independent trace slices match the documented rounding propagation budget; this is distinct from the later full Engine trace comparison. Atomic invalid-late-order refusal and stationary units tested. Self-review completed; independent review NOT RUN. Publication uses user GitHub override, own SHA checked after commit.

Spec review for next salvo ticket identified a pre-existing export schema defect: battles.md column «в корму» is rearHits (Combat §7.1), while TK-0002 named it rearKills. Fix the schema through a follow-up reference repair before golden battle comparison, preserving all original numeric values and Markdown.