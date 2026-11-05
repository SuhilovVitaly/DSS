---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0011-legacy-removal-prep
ticket: EP-0015-US-0011-TK-0004-save-format-20
title: save-format-20
stage: done
layer: engine
depends_on: ["EP-0015-US-0011-TK-0003-maneuver-input-move"]
files_touched: 14
serves: [AC-04]
created: 2026-11-05
revision: 3
---

# save-format-20

## Why
Сохранения без старого боя до удаления его persistence.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs` | CurrentSaveFormatVersion = 19 | 20; writer без боевых секций |
| `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs` | new | 18/19 → 20: отбросить бой, вычистить модули, команды, боезапас, враждебные объекты |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs` | UpgradeSave18, боевые проверки `:143-147` | Миграция в 20; боевые проверки — только внутри миграции |
| `tests/DeepSpaceSaga.Engine.Tests/SaveMigration20Tests.cs` | new | 18→20, 19→20 на фикстурах, Save18 с полями 19, повреждённые, fingerprint, продолжение |

## Public API after the change
Save20.

## Implementation steps
1. Миграция атомарна.
2. Небоевые RNG мира сохраняются.
3. Старые save-DTO пока остаются: их читает миграция; удаление — US-0013.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Engine.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-04 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-06

Add `src/DeepSpaceSaga.Engine/SimulationEngine.cs` (authoritative writer and typed direct-load boundary), `tests/DeepSpaceSaga.Engine.Tests/SaveFixturesTests.cs` (verify permanent fixtures under the new contract; the capture method remains in its published pre-removal commit), and `tests/DeepSpaceSaga.Engine.Tests/CatalogCompatibilityTests.cs` (current version assertion). Preserve validation of the intact legacy save before projection, retaining existing catalog/market checks on the surviving state. Save20 strips only the inventoried combat envelope, weapon/defense module instances, obsolete commands and combat objects; ordinary motion, scanner, cargo and trade state remain. The eventual catalog transition must explicitly accept only the recorded old fingerprint against the exact post-cleanup catalog, with the old manifest checked before any identity change. Legacy runtime code stays for New Game until US-0013.

## Regression-audit scope addition — 2026-11-06

Include `tests/DeepSpaceSaga.Engine.Tests/TradingEconomySaveSchemaTests.cs`: the shared historical economy-fixture helper assumed the current writer still returned combat envelopes; preserve its economy assertions and support a world-only source. First audit was 803 passed / 294 failed across 1097 selected cases; findings include both a shared world fixture issue and tests of now-retired legacy combat continuation. Preserve the exact failing list in the audit TRX and distinguish production regressions from expectations intentionally superseded by Save20.

Include `TradingVoyageFixture.cs`, `EconomyTimeContinuityTests.cs` and `GeneratedWorldPersistenceTests.cs` in Engine.Tests. The first two still constructed obsolete combat envelopes from the current writer; the third compares the unchanged noncombat world after explicitly asserting which old hostile object was removed. Existing economic, clock and deterministic continuation assertions are retained.

## Preserve current world assertions; retire superseded legacy save expectations

`CombatBootstrapTests.cs`, `ModuleHealthBootstrapTests.cs` and `WeaponOperatorSchemaTests.cs` retain ship identity, structure and crew/skill assertions under Save20 and assert removal of old weapon/armor fields. Keep mixed geography/voyage, held-turn and economy tests. Legacy-only tests that rely on retired save continuations remain visible in the audit and are carried to the owning code-removal tickets; this change does not skip or bulk-delete them. Historical migration now exercises the authoritative saved fixtures rather than exposing a second test-only production writer.

## Execution evidence — 2026-11-06

The production writer emits Save20 with no legacy combat envelopes. Both file and typed load paths accept 18/19, validate the legacy payload, project the ordinary world and retain strict catalog checks. All four tracked fixtures continue deterministically. Windows build (0 warnings/errors), scoped format and diff check PASS; 889 Engine and 184 Client cases PASS in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-save20-world-regression-03`. The broader intermediate audit (`20261106-save20-regression-audit`) was not green; it found obsolete combat continuation expectations and noncombat test-fixture regressions. The world regressions were fixed and rerun; legacy-only expectations are carried to their owning removal cuts.
