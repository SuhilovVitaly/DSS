---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0016-balance-and-native-acceptance
ticket: EP-0015-US-0016-TK-0002-performance
title: performance
stage: done
layer: engine
depends_on: ["EP-0015-US-0016-TK-0001-series-parity"]
files_touched: 4
serves: [AC-02]
created: 2026-11-05
revision: 3
---

# performance

## Why
Скорость боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tools/DeepSpaceSaga.Performance/TacticalBattlePerformance.cs` | new | Замеры AC-02 |
| `tools/DeepSpaceSaga.Performance/Program.cs` | точка входа | Режим замера боя |

## Public API after the change
—

## Implementation steps
1. Пороги AC-02; при превышении — профилирование и исправление.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Performance tool; значения в ExecutionStatus. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope confirmed before edits — 2026-11-06

Measure independent Release gates without changing balance or the open Raid reference assertion. Use real Engine steps and CPU Skia frames. Add an internal draw-and-flush observation callback for the subsequent real-window confirmation; it is inert without the evidence consumer.

- tools/DeepSpaceSaga.Performance/TacticalBattlePerformance.cs
- tools/DeepSpaceSaga.Performance/Program.cs
- src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
- DB/Board/EP-0015-tactical-carrier-battle/FinalValidation.md

## Execution evidence — 2026-11-06

Release benchmark on the execution Windows VDI: 2,029 complete authoritative turns p95 0.4454 ms, 1,000 warmed aim calls max 0.2442 ms, 1,400 real screen Skia raster frames 1920x1080 worst phase p95 4.4156 ms / max 9.4828 ms, 1,000 full default battles 1.1292 s. No monitor presentation claim; native draw/flush will be measured separately. Scoped format and zero-warning Release build, 167 Tactical Client tests, diff check. Evidence `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-tactical-release-performance-01/performance.json`.
