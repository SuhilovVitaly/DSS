---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0003-battle-rules
ticket: EP-0015-US-0003-TK-0001-battle-assembly
title: battle-assembly
stage: done
layer: engine
depends_on: ["EP-0015-US-0002-TK-0006-platform-determinism", "EP-0015-US-0001-TK-0004-content-sync"]
files_touched: 3
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# battle-assembly

## Why
Собрать бой ровно как Generation §2.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs` | new | Дрон, носитель, двигатель с множителем, состояние, роли, счётчики ИИ (dry, extend, hunt, strike, role_since, roleChanges) |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs` | new | Сборка из сценария и загрузки (loadout) |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AssemblyTests.cs` | new | 42 броска; таблица «Сборка» trace-seed-1; переопределение без проверки слотов |

## Public API after the change
`TacticalBattle.Create(catalog, scenarioId, seed, tactics, loadouts)`.

## Implementation steps
1. Дроны A, затем B.
2. Переопределение — поверхностное, без повторной проверки слотов; `shield == ""` → нет щита, число щитов 0.
3. Роли: `OrderBy(-hull)` (устойчиво), `startTactic` для командира.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Engine.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Execution evidence — 2026-11-05

Engine-owned mutable TacticalBattle/unit/engine state uses immutable captured definitions and separate RNG per battle. Create preserves A/B drone and carrier order, computes initial speed before skills, rolls all NPC skills before all scatter, then assigns stable hull-ordered roles. Partial loadout record uses null for omitted fields and empty Shield to clear; retains original shield count when omitted, uses default one when needed and zero without shield. Internal assembly intentionally does not revalidate slots; public command validation belongs to US-0005. Carrier uses frame hull/maneuver, ignoring layout hull. Motion performs limits/math; no world state changes.

`D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-assembly-01/`: solution build 0 warnings/errors; Engine TacticalBattle 101/101 (5 new tests), full Motion 1376/1376 including source guard; scoped format/diff PASS. Every published assembly field and Calls=42 matches independent trace at original precision; tests cover range-equal RNG draw, initial speed clamp, role tie order, out-of-slot internal overrides, carrier hull and independent mutable state/RNG. Self-review completed; independent review NOT RUN. Playback/game-session/UI integration is still future work. Own SHA is verified after publication using user GitHub override.