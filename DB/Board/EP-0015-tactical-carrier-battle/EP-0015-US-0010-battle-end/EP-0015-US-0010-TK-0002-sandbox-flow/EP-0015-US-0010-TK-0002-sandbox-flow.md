---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0010-battle-end
ticket: EP-0015-US-0010-TK-0002-sandbox-flow
title: sandbox-flow
stage: done
layer: client
depends_on: ["EP-0015-US-0010-TK-0001-battle-end-ui"]
files_touched: 2
serves: [AC-02]
created: 2026-11-05
revision: 3
---

# sandbox-flow

## Why
Доказать песочницу на пути игрока.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleSandboxFlowTests.cs` | new | F8 → бой → выход: мир, скорость, камера, сохранение, меню |

## Public API after the change
—

## Implementation steps
1. Сравнить сериализованный мир.

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

Extend the existing BattleNavigationTests.cs fixture with an optional save directory and initial speed; production code is unchanged. Compare actual LocalGameSessionConnection slot bytes before F8 and after normal F5 on return, starting paused to exclude unrelated world ticks. A separate running-world path proves restoration of nonzero speed. Use a disposable directory under the validation run, delete only the test's exact files.

## Execution evidence — 2026-11-06

AC-02: actual F8 enters the modal, real battle command runs, F5 cannot write a slot in battle, Escape and confirmation exit, F5 again writes actual LocalGameSessionConnection quicksave byte-identical to the paused pre-F8 world. Map camera restored and Escape opens the normal world menu. Separate actual F8 round trip verifies nonzero Speed2 restoration. Fixture save files removed by the test.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-sandbox-flow-01/: build/format/diff/self-review PASS; full Client2479/2479 uninterrupted, focused182 Client, Engine380, Motion guard19 PASS. This is headless actual window routing, not native/physical acceptance.
