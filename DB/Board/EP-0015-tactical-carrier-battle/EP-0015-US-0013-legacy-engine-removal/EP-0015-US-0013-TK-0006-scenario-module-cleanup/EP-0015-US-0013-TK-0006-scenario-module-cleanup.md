---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0013-legacy-engine-removal
ticket: EP-0015-US-0013-TK-0006-scenario-module-cleanup
title: scenario-module-cleanup
stage: done
layer: content-data
depends_on: ["EP-0015-US-0013-TK-0005-engine-removal-4"]
files_touched: 26
serves: [AC-08, AC-07]
created: 2026-11-05
revision: 3
---

# scenario-module-cleanup

## Why
Модули старого боя в сценариях после удаления их кода — инертные данные; убрать их до удаления определений типов модулей.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Scenarios/*/scenario.json` | боевые модули на кораблях | Убрать модули старого боя |
| `src/DeepSpaceSaga.Client/Scenarios/{CombatMukha,CombatMukhaSingle}/` | сценарии старого боя | Удалить |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ScenarioModuleCleanupTests.cs` | new | Все сценарии загружаются; боевых модулей нет; New Game без них |

## Public API after the change
—

## Implementation steps
1. Проверить загрузкой каждый сценарий.
2. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests content discovery. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-08, AC-07 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

Strip authored real-time weapons, armor and hostile AI. Remove both dedicated combat starts. Preserve ordinary hull layout, loadout, crew/cabins/rations, station data, motion and a neutral contact in PlayerShipOnly. Test the actual content of source/deployed scenario lists and the New Game snapshot, and restore exact world corpus comparisons now that the authored combat actor is a normal neutral world contact. Historical Save18/19 fixtures remain unmodified.

- Edit data: src/DeepSpaceSaga.Client/Scenarios/Default/scenario.json
- Edit data: src/DeepSpaceSaga.Client/Scenarios/Default_500/scenario.json
- Edit data: src/DeepSpaceSaga.Client/Scenarios/Docked/scenario.json
- Edit data: src/DeepSpaceSaga.Client/Scenarios/PlayerShipOnly/scenario.json
- Edit data: src/DeepSpaceSaga.Client/Scenarios/Undocked/scenario.json
- Delete data: src/DeepSpaceSaga.Client/Scenarios/CombatMukha/scenario.json
- Delete data: src/DeepSpaceSaga.Client/Scenarios/CombatMukhaSingle/scenario.json
- Edit Engine check: tests/DeepSpaceSaga.Engine.Tests/ClusterCorrectnessCorpusTests.cs
- Edit Engine check: tests/DeepSpaceSaga.Engine.Tests/SystemCorrectnessCorpusTests.cs
- Edit Engine check: tests/DeepSpaceSaga.Engine.Tests/FullMapCorrectnessCorpusTests.cs
- Edit Engine check: tests/DeepSpaceSaga.Engine.Tests/GeneratedWorldPersistenceTests.cs
- Edit Engine check: tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs
- Edit Engine check: tests/DeepSpaceSaga.Engine.Tests/DebugTuningShipTests.cs
- Edit Engine check: tests/DeepSpaceSaga.Engine.Tests/ModulePanelProjectionTests.cs
- Edit Engine check: tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorSchemaTests.cs
- Edit Client check: tests/DeepSpaceSaga.Client.Tests/TetrarchLoadoutTests.cs
- Edit Client check: tests/DeepSpaceSaga.Client.Tests/PirateScenarioTests.cs
- Edit Client check: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0001Tests.cs
- Edit Client check: tests/DeepSpaceSaga.Client.Tests/CountermeasureContentTests.cs
- Edit Client check: tests/DeepSpaceSaga.Client.Tests/CombatSessionRoundtripTests.cs
- Retire old scenario tests: tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs
- Retire old scenario tests: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0009TK0001Tests.cs
- Retire old scenario tests: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0009TK0002Tests.cs
- Add: tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ScenarioModuleCleanupTests.cs
Default now occupies 6 of the same 12 hull cells; retained world modules and cargo are asserted. Update the old full-file stress-scenario checksum after comparing all non-player rows to the authored baseline:
- tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs
- tests/DeepSpaceSaga.Client.Tests/StationResourceFieldContentTests.cs

## Execution evidence — 2026-11-06

Removed both dedicated realtime starts, 13 legacy weapon installations from five remaining files, and their authored armor/hostile AI. PlayerShipOnly retains the neutral contact, crew and world motion; it now survives Save20 alongside the player. All six source and deployed entries and the actual Default New Game stream load without old weapons; crew, scanner, ordinary maneuver and hull placement checked. The 501 non-player stress rows are byte-equivalent after JSON canonicalization. Windows: clean build/format/diff; broad Client 1987 and Engine 2172 passes before known obsolete assertions were corrected; rerun 39 Client and 101 Engine including every initially failed case, 44 economy and 2 focused cargo checks passed. Evidence under `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-world-scenarios-regression-01` and `20261106-world-scenarios-regression-03`. Long corpus final gate remains pending.
