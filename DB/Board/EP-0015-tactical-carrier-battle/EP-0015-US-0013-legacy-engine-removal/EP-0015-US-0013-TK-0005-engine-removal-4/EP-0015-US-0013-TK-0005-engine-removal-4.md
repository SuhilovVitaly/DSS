---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0013-legacy-engine-removal
ticket: EP-0015-US-0013-TK-0005-engine-removal-4
title: engine-removal-4
stage: draft
layer: engine
depends_on: ["EP-0015-US-0013-TK-0004-engine-removal-3"]
files_touched: 12
serves: [AC-02, AC-07]
created: 2026-11-05
revision: 3
---

# engine-removal-4

## Why
Engine: остаток и ядро, часть 4.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `DB/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `DB/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.WeaponOperators.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/Combat/{LaunchScatterRng,ModuleDamageMath,ModuleHitLocationRng,ShipDestructionMath}.cs` | математика старого боя | Удалить |
| `src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs` | боевые потоки | Убрать боевые имена |
| `src/DeepSpaceSaga.Engine/{SimulationEngine,SimulationEngine.Presentation,SimulationEngine.PlayerDefeat,SimulationEngine.Time}.cs` | боевые части | Убрать по инвентаризации |
| `src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj` | `InternalsVisibleTo DeepSpaceSaga.CombatAnalyzer` (`:17`) | Убрать |

## Public API after the change
—

## Implementation steps
1. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
2. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `DB/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Engine.Tests регрессия. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02, AC-07 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `DB/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
