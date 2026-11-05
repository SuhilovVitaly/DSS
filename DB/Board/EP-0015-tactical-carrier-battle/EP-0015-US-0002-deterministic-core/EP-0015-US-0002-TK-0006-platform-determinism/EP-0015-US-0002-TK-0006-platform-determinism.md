---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0002-deterministic-core
ticket: EP-0015-US-0002-TK-0006-platform-determinism
title: platform-determinism
stage: draft
layer: motion
depends_on: ["EP-0015-US-0002-TK-0005-limits-and-chance"]
files_touched: 5
serves: [AC-06]
created: 2026-11-05
revision: 3
---

# platform-determinism

## Why
Закрыть G-03 замером.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Motion.Tests/TacticalBattle/PlatformDeterminismTests.cs` | new | Все аргументы тригонометрии geometry.json и шанс на текущей ОС |
| `src/DeepSpaceSaga.Motion/TacticalBattle/DeterministicMath.cs` | new, если нужно | Корректно округлённые Sin/Cos/Atan2 |
| `tools/DeepSpaceSaga.CarrierWingSim/carrierwing/geometry.py, VData/Planning/V4.0/Reference/*, VData/Planning/V4.0-archive/Changes/<дата>-trig.md` | только при варианте (2) | Тот же алгоритм в Python, перегенерация, запись |

## Public API after the change
При расхождении `DeterministicMath` заменяет `Math.*` в TacticalGeometry.

## Implementation steps
1. Прогнать на Windows и Linux.
2. При расхождении — вариант (1) AC-06; только если он не сходится — вариант (2) с перегенерацией в Linux/glibc.
3. Записать решение и числа в Documentation.md эпика и ExecutionStatus.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Motion.Tests на обеих ОС; результат в ExecutionStatus. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-06 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
