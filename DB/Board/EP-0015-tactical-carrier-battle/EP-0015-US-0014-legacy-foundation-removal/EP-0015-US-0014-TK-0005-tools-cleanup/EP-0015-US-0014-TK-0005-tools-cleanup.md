---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0014-legacy-foundation-removal
ticket: EP-0015-US-0014-TK-0005-tools-cleanup
title: tools-cleanup
stage: done
layer: tooling
depends_on: ["EP-0015-US-0014-TK-0004-content-cleanup"]
files_touched: 9
serves: [AC-04, AC-05]
created: 2026-11-05
revision: 3
---

# tools-cleanup

## Why
Инструменты без старого боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.CombatSim/` | старая Python-модель | Удалить |
| `tools/DeepSpaceSaga.CombatAnalyzer/` | старый бой | Удалить |
| `tools/DeepSpaceSaga.Performance/{PointCombatNativeEvidence,CombatReportsNativeEvidence}.cs` | старый бой | Удалить |
| `tools/DeepSpaceSaga.Performance/{DebugTuningNativeEvidence,ModulePanelNativeEvidence,SolarNativeEvidence,Program}.cs, tools/DeepSpaceSaga.Performance.Tests/` | боевые части | Убрать |
| `tools/graphify/rebuild.py` | ссылки на удалённые файлы (`:95-99`) | Обновить |

## Public API after the change
—

## Implementation steps
1. `tools/DeepSpaceSaga.CarrierWingSim` не трогать.
2. Финальный поиск символов старого боя — AC-05.
3. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
4. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Сборка решения; полная регрессия; Performance.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-04, AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before edits — 2026-11-06

Delete the seven tracked files of the retired Python world combat model. The old native evidence and analyzer were removed with their compiled callers. Correct the remaining native debug check to the actual engine-only title and replace stale Graphify runtime anchors with current tactical contracts. Keep map, solar, station, module and debug profiling, as well as standalone tools outside scope.

- Delete: src/DeepSpaceSaga.CombatSim/.gitignore
- Delete: src/DeepSpaceSaga.CombatSim/combatsim/__init__.py
- Delete: src/DeepSpaceSaga.CombatSim/combatsim/__main__.py
- Delete: src/DeepSpaceSaga.CombatSim/combatsim/core.py
- Delete: src/DeepSpaceSaga.CombatSim/combatsim/gamedata.py
- Delete: src/DeepSpaceSaga.CombatSim/README.md
- Delete: src/DeepSpaceSaga.CombatSim/tests/test_core.py
- Edit: tools/graphify/rebuild.py
- Edit: tools/DeepSpaceSaga.Performance/DebugTuningNativeEvidence.cs

## Execution evidence — 2026-11-06

Deleted the tracked realtime Python world combat model. The analyzer and obsolete native tools had already been removed with their dependent code. Graphify now binds to current TacticalBattle owners and the legacy migration boundary; Python syntax, source paths and named tokens checked. World profiling stays in place; native debug now expects the engine-only title. rg found no retired implementation symbols in live code/tool sources; archived save-field strings and fixtures are retained intentionally. Scoped format, zero-warning build, diff check. Complete regression: 175 Contracts, 1,267 Motion, 2,138 Engine (2,024 regular + 114 exact corpus IDs), 1,963 Client, all 61 Economy tests and all 9 Performance tests. Primary matrix: `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-legacy-foundation-full-02/matrix.json`; concurrent Performance process hit the 300 s cap, then its 7 and 2 test partitions both passed in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-legacy-foundation-performance-partitions-01/`.
