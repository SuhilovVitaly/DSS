---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0009-fire-and-results
ticket: EP-0015-US-0009-TK-0002-results-phase
title: results-phase
stage: done
layer: client
depends_on: ["EP-0015-US-0009-TK-0001-fire-planning"]
files_touched: 5
serves: [AC-02]
created: 2026-11-05
revision: 3
---

# results-phase

## Why
Фаза результатов.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs` | new | Таймлайн снарядов, следы, вспышки, значения до залпа, итог по целям |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs` | new | Кадры таймлайна, смещения промаха, двухшаговый Enter, «Ещё раз», пропуск фазы |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | phase presentation | Play, skip, replay and continue without additional Engine calls |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | immutable map renderer | Draw projectile/flash/miss and target deltas |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs` | deterministic test clock | Finish playback explicitly in existing navigation flows |

## Public API after the change
—

## Implementation steps
1. Таймлайн по реальному времени.
2. Ключи этого тикета — в `Data/Locale/Russian.json` (рабочий язык, D-USER-11) и теми же ключами в `English.json` черновым переводом без вычитки (правило R-07).

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-06

Use immutable admitted shots and before/after units; no replay or skip sends a command. Start automatically after the existing movement timeline for Engine auto-salvos, skip the presentation entirely for no shots. Flight shows pre-salvo values in map and header; the later sides/forecast tabs consume the same presentation mode. Cap entire flight including final arrival at 3000 ms by compressing the launch interval only when required.

## Execution evidence — 2026-11-06

AC-02 core result timeline: actual Engine shots animate in stable order, 300ms first launch, 380ms nominal spacing, 650ms flight, 450ms flash and 3000ms cap; misses alternate 13 logical pixels sideways and extend 28. During flight map/header use pre-salvo values; on arrival they switch to admitted outcomes and target deltas. Skip, replay and continue are local presentation actions; queued tactical orders are blocked. Empty automatic salvo skips presentation; automatic shots start after movement. Sides/forecast tabs will use the same frame in TK-0003/4.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-salvo-results-01/: build/format/diff/self-review PASS; 173 Client +378 Engine +19 Motion source guard PASS. Actual flight/resolved CPU rasters visually inspected. Long automatic salvo cap exercised with repeated captured shot fixture.
