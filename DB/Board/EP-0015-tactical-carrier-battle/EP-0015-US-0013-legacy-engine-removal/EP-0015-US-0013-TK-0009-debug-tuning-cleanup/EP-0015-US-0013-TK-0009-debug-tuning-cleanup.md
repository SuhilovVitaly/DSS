---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0013-legacy-engine-removal
ticket: EP-0015-US-0013-TK-0009-debug-tuning-cleanup
title: debug-tuning-cleanup
stage: done
layer: engine
depends_on: ["EP-0015-US-0013-TK-0008-engine-noncombat-cleanup"]
files_touched: 4
serves: [AC-05, AC-07]
created: 2026-11-05
revision: 3
---

# debug-tuning-cleanup

## Why
EP-0013 без старого боя (Engine-часть).

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.{DebugTuningProjectiles,DebugTuningReloads}.cs` | боевые оверрайды | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.{DebugTuning,DebugTuningPersistence,DebugTuningStartup,DebugTuningShips}.cs` | схема тюнинга | Схема +1 без боевых полей |

## Public API after the change
Схема DebugTuning без боевых полей.

## Implementation steps
1. Отказ старого файла — код и поле в ответе; лог пишет Client.
2. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
3. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Engine.Tests DebugTuning. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-05, AC-07 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

Runtime flight/reload tuning was removed in TK4 and catalog values in TK7. Version the shared file syntax to schema 2, write only world propulsion overrides, keep schema 1 files with only ships (including the old writer's empty launchers object) readable, and reject old combat overrides atomically with an actionable Interface.log message. Historical saved overlays remain under the Save18/19 migration. Test Engine bootstrap and the actual Client warning adapter.

- Edit: src/DeepSpaceSaga.Contracts/DebugTuningFileFormat.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/DebugTuningStartupTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/DebugTuningFileWriterTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/DebugTuningDefaultsFileTests.cs

## Execution evidence — 2026-11-06

Debug tuning now writes schema 2 with six propulsion values under ships. Schema 1 files containing only world overrides (including an empty launchers object) remain readable; nonempty legacy weapon overrides are refused atomically with a concrete edit suggestion. The actual LocalClient startup warning callback writes the path and rejection to Interface.log and applies no partial overlay. Windows solution build and scoped format/diff clean; 284 Contracts, 56 Engine and 185 Client passed in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-world-debug-schema2-01`.
