---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0002-deterministic-core
ticket: EP-0015-US-0002-TK-0006-platform-determinism
title: platform-determinism
stage: done
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

## Scope correction before validation — 2026-11-05

Existing GeometryTests already evaluates every geometry.json argument and exact IEEE image; LimitsAndChanceTests covers all published seed-one chances. Reuse these production-path tests rather than add duplicate PlatformDeterminismTests or unused DeterministicMath. Code context for this ticket is evidence-only: existing tests (read/run), new Board `PlatformValidation.md`, and current Board Documentation.md/ExecutionStatus/coverage. No code or golden expectation changes are planned unless the platform run finds a mismatch. D-USER-15 means absent original battle trig arguments are not invented; measured coverage is the 1088 independent vectors, all source shot chances and later golden-battle results.
## Execution evidence — 2026-11-05

PlatformValidation.md records Windows and Linux/glibc runs. Linux run 42089031642 at fc29503a812f6793779d3564f22884a8cd0eecdb succeeded: solution build, 1376 Motion, 96 Engine TacticalBattle and 2 Client content tests. Windows same Motion set 1376/1376; all 1088 exact geometry images match on both platforms, plus all 34 source shot chances at source precision. No production changes required: no observed trig discrepancy, no speculative DeterministicMath, no regenerated expected outcomes. The original all-battle trig argument set is absent and remains explicit coverage debt under D-USER-15; golden battle integration must still pass in US-0004.

Evidence root `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-platform-01/` contains Windows SDK info and Linux job log. Windows build/tests/format use the unchanged-code evidence in geometry-01 and limits-01. This ticket changes only Board evidence, so content/links/diff checks are appropriate; no redundant duplicate tests were added. Self-review completed; independent review NOT RUN. Own SHA is verified after publication using explicit user GitHub authorization.

Story review: six deterministic-core tickets complete under D-USER-14/15 available-source contract. Motion remains pure, RNG remains Engine-owned; no world simulation/save format changes. End-to-end battle/UI integration and broader actual-argument coverage are dependent work. US-0001 future Client availability presentation also remains open in US-0007.