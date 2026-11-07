---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0010-battle-end
ticket: EP-0015-US-0010-TK-0001-battle-end-ui
title: battle-end-ui
stage: done
layer: client
depends_on: ["EP-0015-US-0009-TK-0004-forecast-tab"]
files_touched: 3
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# battle-end-ui

## Why
Конец боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | из US-0008 | Итог, новый бой, выход |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleEndTests.cs` | new | Итог по playerSide, ничья с двумя причинами, новый бой посреди боя, выход |

| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | existing serialized navigation | Permit New during active play after exit succeeds |

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

Retain the previously verified typed header outcome. Make New visible in the upper panel throughout the battle, enabled during planning and after arrival; gated during request/movement/flight and by the window before dispatch. The current session must accept Exit before replacing the modal with a fresh setup that prefills LastStart. Return directly when the outcome is already visible, otherwise require the existing confirmation.

## Execution evidence — 2026-11-06

AC-01: New Battle is available in the upper panel during planning and after arrival. Actual window route exits current Engine battle before opening a new setup; full accepted choices, tactics and seed survive; original modal pause stays at depth one. Mid-battle Escape confirms, terminal Escape exits directly. Verified real completed outcome and both typed reasons on a draw presentation fixture.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-battle-end-01/: build/format/diff/self-review PASS; 180 Client +380 Engine +19 Motion guard PASS.
