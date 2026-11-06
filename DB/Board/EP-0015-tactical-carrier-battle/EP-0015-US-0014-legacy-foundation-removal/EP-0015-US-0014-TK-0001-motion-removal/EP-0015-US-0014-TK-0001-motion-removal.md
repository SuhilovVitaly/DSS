---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0014-legacy-foundation-removal
ticket: EP-0015-US-0014-TK-0001-motion-removal
title: motion-removal
stage: done
layer: motion
depends_on: ["EP-0015-US-0013-TK-0010-legacy-save-dto-removal"]
files_touched: 21
serves: [AC-01, AC-05]
created: 2026-11-05
revision: 3
---

# motion-removal

## Why
Motion без старого боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Motion/{BlastMath,BlastThreatMath,CombatForecast,CombatRangeMath,EvasionMotionMath,TorpedoCollisionMath,HostileOrbitMath}.cs` | старый бой | Удалить |

## Public API after the change
—

## Implementation steps
1. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
2. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Motion.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01, AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

Remove all seven obsolete realtime Motion files. Retain the ordinary turn/speed analytical arc as a local world-motion operation with plain inputs, and keep CurrentManeuverForecast for real engine cycles, manual axes and the map trajectory. Contracts and TacticalBattle math are unchanged.

- Delete motion: src/DeepSpaceSaga.Motion/BlastMath.cs
- Delete motion: src/DeepSpaceSaga.Motion/BlastThreatMath.cs
- Delete motion: src/DeepSpaceSaga.Motion/CombatForecast.cs
- Delete motion: src/DeepSpaceSaga.Motion/CombatRangeMath.cs
- Delete motion: src/DeepSpaceSaga.Motion/EvasionMotionMath.cs
- Delete motion: src/DeepSpaceSaga.Motion/TorpedoCollisionMath.cs
- Delete motion: src/DeepSpaceSaga.Motion/HostileOrbitMath.cs
- Delete tests of removed rules: tests/DeepSpaceSaga.Motion.Tests/CombatRangeMathTests.cs
- Delete tests of removed rules: tests/DeepSpaceSaga.Motion.Tests/TorpedoCollisionTests.cs
- Delete tests of removed rules: tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0018TK0004Tests.cs
- Delete tests of removed rules: tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0007TK0001Tests.cs
- Delete tests of removed rules: tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0006TK0002Tests.cs
- Delete tests of removed rules: tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0006TK0001Tests.cs
- Delete tests of removed rules: tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0003TK0001Tests.cs
- Delete tests of removed rules: tests/DeepSpaceSaga.Motion.Tests/HostileOrbitMathTests.cs
- Delete tests of removed rules: tests/DeepSpaceSaga.Motion.Tests/FlankApproachTests.cs
- Delete tests of removed rules: tests/DeepSpaceSaga.Motion.Tests/EvasionSteeringTests.cs
- Edit: src/DeepSpaceSaga.Motion/CurrentManeuverForecast.cs
- Edit: src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs
- Edit: src/DeepSpaceSaga.Motion/HoldMotion.cs
- Edit: tests/DeepSpaceSaga.Motion.Tests/PointCombatLegacyRemovalKinematicsTests.cs
- Edit: tests/DeepSpaceSaga.Motion.Tests/HoldMotionTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/SpeedHoldEngineTests.cs
The initial build identified both suites' use of the retired orbit sampler as a movement oracle. Preserve their checks using fixed positions from independent numerical integration of thrust and heading, including the split at the speed limit.

## Execution evidence — 2026-11-06

Removed seven realtime motion rule implementations and their obsolete tests. World turn/acceleration arcs, ordinary trajectories and inertial cycles retain independent numeric and step-partition checks. Scoped formatting and zero-warning solution build; 1,267 Motion, 764 Engine and 265 Client tests passed in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-world-motion-regression-03/`.
