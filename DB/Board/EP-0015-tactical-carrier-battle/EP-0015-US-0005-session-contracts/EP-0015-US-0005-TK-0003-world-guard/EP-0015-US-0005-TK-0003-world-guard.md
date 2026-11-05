---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0005-session-contracts
ticket: EP-0015-US-0005-TK-0003-world-guard
title: world-guard
stage: done
layer: engine
depends_on: ["EP-0015-US-0005-TK-0002-engine-session"]
files_touched: 5
serves: [AC-03]
created: 2026-11-05
revision: 3
---

# world-guard

## Why
Песочница не трогает мир.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs` | из TK-0002 | Флаг; отказ команд мира, save, quick-save/load |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | команды мира и снимок | Проверка флага; флаг в снимке |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/WorldGuardTests.cs` | new | Мир до и после боя идентичен; отказ сохранения; поражение мира не выставляется |

## Public API after the change
Флаг в `AuthoritativeSnapshot`.

## Implementation steps
1. Скорость мира и тик-цикл не трогать: мир стоит на модальной паузе Client.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Engine.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-03 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-05

Five code files in Engine + owning tests: existing three positions plus SimulationEngine.Time.cs (station travel directly advances calendar time) and Dialogue/SimulationEngine.Dialogue.cs (direct dialogue queue bypasses PlayerCommand). Main engine locks world before command gate for atomic refusal; pending world commands are also checked on processing. Speed0 remains accepted for modal pause; acceleration refused while active. Save capture and in-place LoadScenario throw tactical_battle_active before mutation; static factories create independent sessions and cannot know another engine's active battle, so Client quick-load entry guard remains US-0007. No tick-loop changes. Tactical state never persists into saves or world defeat.

## Execution evidence — 2026-11-05

AC-03 Engine implemented: active flag through world snapshots; immediate, deduplicated world-command refusal also survives Exit before next tick; queued commands/dialogues refused; direct station travel/acceleration refused; save capture/in-place load rejected before mutation. Speed0/modal Client owns pausing, tick loop unchanged. New factories are independent sessions; UI quick-load prevention remains US-0007. Active includes Over until Exit. Complete saved-world equality verified after normal battle and tactical defeat; world outcome remains Running. Save/quick-save writes neither new directories nor modified slot bytes; Exit restores saving/loading.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-world-guard-03/: build PASS 0 warnings/errors, verify-format PASS, tests 436/436 PASS including all280 TacticalBattle plus command/dialogue/travel/save continuity/defeated-save regressions. Earlier run01 had test-only invalid enum identifier; corrected; no stale-binary run counted. Self-review/diff PASS; independent review/native NOT RUN. User override authorizes origin publication.
