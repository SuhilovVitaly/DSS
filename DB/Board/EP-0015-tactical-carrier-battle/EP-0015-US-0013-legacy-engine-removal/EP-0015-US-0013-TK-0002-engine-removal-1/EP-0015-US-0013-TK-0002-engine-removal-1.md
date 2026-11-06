---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0013-legacy-engine-removal
ticket: EP-0015-US-0013-TK-0002-engine-removal-1
title: engine-removal-1
stage: done
layer: engine
depends_on: ["EP-0015-US-0013-TK-0001-local-client-removal"]
files_touched: 20
serves: [AC-02, AC-07]
created: 2026-11-05
revision: 3
---

# engine-removal-1

## Why
Engine: бой, журнал, отчёты, часть 1.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportIdentity.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportPersistence.cs` | старый бой | Удалить |

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

The combat tick and event journal still supply shared private point/encounter/evasion code. Remove the independent realtime report path and all old runtime save restore/capture first, keeping ScenarioLoader historical validation and the Save20 projection intact. The remaining journal and encounter processing move together to TK-0003; the Combat partial's point and damage execution is removed with its direct owners in TK-0004. The linked report emitters from TK-0003 and PointCombatPersistence from TK-0005 must be removed here to keep the cut compilable.

Delete:
- src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportIdentity.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportPersistence.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportRange.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs

Edit direct consumers:
- src/DeepSpaceSaga.Engine/SimulationEngine.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs

Test scope will record exact retired expectations.
## Additional scope before edits

Compiler identified the unused impact sequence in `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs`; remove only that field. The four report-only suites/helpers are retired:
- tests/DeepSpaceSaga.Engine.Tests/CombatReportOutboxTests.cs
- tests/DeepSpaceSaga.Engine.Tests/CombatReportsSaveLoadTests.cs
- tests/DeepSpaceSaga.Engine.Tests/CombatReportsScenarioFixtures.cs
- tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0017TK0002Tests.cs

In mixed tests remove only the following retired runtime continuation methods:
- `PointCombatUS0009TK0004Tests.cs`: `Save18_midflight_resume_matches_uninterrupted`, `Save18_preserves_evasion_segment_anchor_and_original_base_course`, `Npc_dodge_cursor_roll_and_motion_continue_without_reroll`, `Lowercase_point_owner_launcher_and_projectile_references_normalize_to_existing_identity`, `Won_and_acknowledged_idle_preserve_statistics_and_orphan_enemy_flight`, `Save_during_evasion_restores_suspended_orbit_and_its_command_identity`, `Catalog_edit_changes_future_weapons_without_recomputing_captured_flights`
- `PointCombatUS0020TK0005Tests.cs`: `Reload_newgame_save_load_and_duplicate_delivery`

Keep malformed report migration tests on real Save19 fixtures, and keep live combat tests' non-report assertions until their owning runtime removal. Affected mixed files:
- tests/DeepSpaceSaga.Engine.Tests/CombatReportsSaveSchemaTests.cs
- tests/DeepSpaceSaga.Engine.Tests/CombatPresentationTests.cs
- tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0008TK0003Tests.cs
- tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0004Tests.cs
- tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0020TK0005Tests.cs
Retire `PointCombatUS0008TK0003Tests.Intercept_counter_uses_captured_hostility_after_owner_death_and_new_encounter`: the fixture belongs to the removed report suite and both assertions are for the retired realtime encounter/report.

## Execution evidence — 2026-11-06

Removed the old report producers/projection and runtime combat capture/restore. ScenarioLoader still validates historical envelopes before discarding them into Save20; malformed report cases run on the recorded Save19 fixture. The old realtime tick and encounter remain only through their owning cuts. Windows solution build 0 warnings/errors, scoped format/diff PASS; 877 selected Engine and 223 selected Client PASS in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-engine-reports-regression-02`.
