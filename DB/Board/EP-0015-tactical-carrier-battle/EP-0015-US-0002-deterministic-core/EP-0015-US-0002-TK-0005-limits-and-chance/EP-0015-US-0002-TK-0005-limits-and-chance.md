---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0002-deterministic-core
ticket: EP-0015-US-0002-TK-0005-limits-and-chance
title: limits-and-chance
stage: draft
layer: motion
depends_on: ["EP-0015-US-0002-TK-0004-geometry"]
files_touched: 3
serves: [AC-05]
created: 2026-11-05
revision: 3
---

# limits-and-chance

## Why
Единый источник правил движения и шанса для Engine и Client (прогноз).

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Motion/TacticalBattle/TacticalLimits.cs` | new | Масса, k, vmin/vmax/change/turn, clamp_speed, предел поворота |
| `src/DeepSpaceSaga.Motion/TacticalBattle/TacticalHitChance.cs` | new | Слагаемые шанса, rangeBonus и clamp/100; признаки кормы и хвоста |
| `tests/DeepSpaceSaga.Motion.Tests/TacticalBattle/LimitsAndChanceTests.cs` | new | Примеры Combat §3 и §6.2; слагаемые выстрелов trace-seed-1.json |

## Public API after the change
`TacticalLimits.Of(frame, modules, factors)`, `TacticalHitChance.Terms(...)`, `.Chance(...)`.

## Implementation steps
1. Суммы по двигателям в порядке списка, деление только double; масса двигателей прибавляется в конце.
2. Слагаемые в порядке Combat §6.2, начиная с 0.
3. Без бросков и без состояния.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Motion.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
