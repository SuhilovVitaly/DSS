---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0013-legacy-engine-removal
ticket: EP-0015-US-0013-TK-0003-engine-removal-2
title: engine-removal-2
stage: done
layer: engine
depends_on: ["EP-0015-US-0013-TK-0002-engine-removal-1"]
files_touched: 23
serves: [AC-02, AC-07]
created: 2026-11-05
revision: 3
---

# engine-removal-2

## Why
Engine: отчёты, контрмеры, encounter, уклонение, hostile, часть 2.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportRange.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Evasion.cs` | старый бой | Удалить |

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
Engine.Tests регрессия. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02, AC-07 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

Report range/projection moved into the preceding independent report cut. This cut removes realtime encounter lifecycle, speed lock, clock subdivision and acknowledgement; preserve normal world clocks, dialogue, pause and tactical modal clock guard. Legacy journal is still used for the last realtime defeat fact and is removed with damage/projectile execution. Evasion and countermeasures are coupled to that same execution and move to TK-0004. Do not use dummy runtime types to bridge the cut.

Delete:
- src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs

Edit direct consumers:
- src/DeepSpaceSaga.Engine/SimulationEngine.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.Evasion.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.PlayerDefeat.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs
- src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs

Test scope: retire old speed lock/encounter and runtime continuation assertions; retain live world, command, and independent legacy schema checks. Cargo upgrade uses Docked and asserts configured capacity through Save20. Exact test files:
- tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0008TK0001Tests.cs
- tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0008TK0002Tests.cs
- tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0008TK0003Tests.cs
- tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0020TK0006Tests.cs
- tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0005Tests.cs
- tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0003Tests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0009TK0001Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0009TK0002Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0001Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0001Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0005Tests.cs
- tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0019TK0002Tests.cs
- tests/DeepSpaceSaga.EconomyBalance.Tests/LongVoyageRunnerTests.cs

## Execution evidence — 2026-11-06

Removed the realtime encounter, range admission, acknowledgements, speed lock and special clock subdivision. World advancement uses the ordinary clock. The legacy wire definitions remain for historical validation until their owning cut. Retained high-speed clock, hold, dialogue and compatibility checks. Solution build clean; scoped format and diff check passed. Windows: 2,012 Client (`20261106-encounter-regression-02`), 919 Engine and 2 EconomyBalance (`20261106-encounter-regression-03`), and 226 focused Client (`20261106-encounter-regression-04`) passed; artifacts under `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/`.
