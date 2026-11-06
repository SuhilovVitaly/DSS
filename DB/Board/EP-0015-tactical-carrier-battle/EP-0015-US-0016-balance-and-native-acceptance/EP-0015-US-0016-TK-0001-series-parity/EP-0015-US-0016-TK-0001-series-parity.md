---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0016-balance-and-native-acceptance
ticket: EP-0015-US-0016-TK-0001-series-parity
title: series-parity
stage: in-progress
layer: engine
depends_on: ["EP-0015-US-0015-TK-0001-locale-coverage"]
files_touched: 2
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# series-parity

## Why
Баланс игры = баланс V4.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SeriesParityTests.cs` | new | Серия цели = series.json |

## Public API after the change
—

## Implementation steps
1. Категория `LongRunning`, деление по тактикам; обычный прогон её исключает.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Engine.Tests; process-tree cap 300000 ms на запуск. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope confirmed before edits — 2026-11-06

Execute the complete independent published seed range once per tactic. Verify exact recoverable counts and separately compare only published rounding for means and carrier loss rates. Keep normal Linux CI short via the LongRunning filter.

- tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SeriesParityTests.cs
- .github/workflows/tactical-battle.yml

## Cross-platform diagnosis before parity signoff

Windows Release: four tactics match exactly; raid 856/140 vs 857/139 wins/losses with the remaining published counters matching. Retain the independent fixtures and exact assertion. Reopen G-03 numerical validation: the ordinary exact geometry sample does not cover every call. Add a read-only Ubuntu diagnostic on the existing approved runner for the same unchanged core and a Windows per-seed comparison. It reports the actual result even if the exact comparison fails; it is not a PASS for AC-01. The ticket remains in progress.
