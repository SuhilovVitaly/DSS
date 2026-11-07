---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0009-fire-and-results
ticket: EP-0015-US-0009-TK-0001-fire-planning
title: fire-planning
stage: done
layer: client
depends_on: ["EP-0015-US-0008-TK-0005-movement-animation"]
files_touched: 5
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# fire-planning

## Why
Планирование стрельбы.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFirePanel.cs` | new | Цели, выбор, сообщения, отправка |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | из US-0008 | Слои планирования |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePlanningTests.cs` | new | Выбор цели кликом и списком, по умолчанию — ИИ, сообщения заклинивания и пустого сектора |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFireOrders.cs` | new | Capture default targets, edit choices and build immutable view |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | actual battle view | Connect Fire panel, map picking and exact Fire command |

## Public API after the change
—

## Implementation steps
1. Ключи этого тикета — в `Data/Locale/Russian.json` (рабочий язык, D-USER-11) и теми же ключами в `English.json` черновым переводом без вычитки (правило R-07).

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-06

Five Client files. Candidate ID, distance, angle, probabilities and default come from the admitted Engine frame. Player edits are local, encoded as explicit target or null for each own active drone. The dropdown gives the complete candidate caption via tooltip; right panel and map use the same immutable planning view. Enemy rear arcs are presentation, not a recomputation of candidate validity.

## Execution evidence — 2026-11-06

AC-01: default targets come from the Engine snapshot. The panel shows exact candidate distance/angle and chance, carrier/rear marks and full tooltip. Jammed and empty sectors have separate messages. Actual map hit/toggle, panel dropdown and clear-all were exercised on a real LocalGameSessionConnection; Engine fired exactly one own shot at the selected target with the displayed raw chance. Fire input is gated while the command is pending.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-fire-planning-01/: build/format/diff/self-review PASS; 170 Client +378 Engine +19 Motion guard PASS; actual Fire CPU raster visually inspected. Native/physical/human acceptance remains open.
