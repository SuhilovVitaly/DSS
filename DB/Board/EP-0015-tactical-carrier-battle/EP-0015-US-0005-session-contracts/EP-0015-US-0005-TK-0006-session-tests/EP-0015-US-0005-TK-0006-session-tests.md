---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0005-session-contracts
ticket: EP-0015-US-0005-TK-0006-session-tests
title: session-tests
stage: done
layer: engine-local-client
depends_on: ["EP-0015-US-0005-TK-0005-client-session-handle"]
files_touched: 1
serves: [AC-03, AC-06]
created: 2026-11-05
revision: 3
---

# session-tests

## Why
Граница системы, а не только Engine.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/LocalSessionBattleTests.cs` | new | 90 боёв через соединение в двух режимах; блок сохранения |

## Public API after the change
—

## Implementation steps
1. Режим 1: пустые приказы, цели `default`.
2. Режим 2: приказы = `suggest`, цели = `default`.
3. Расхождение режимов между собой — дефект, даже если один совпал.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Engine.Tests; process-tree cap 300000 ms. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-03, AC-06 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Validation plan before implementation — 2026-11-05

One owning test file only. Each independent row runs through real LocalGameSessionConnection twice (empty orders vs exact Suggestions); both complete immutable final snapshots must agree, and every published summary field is checked against source schema2. No new simulator or expected values. Additional real-world save refusal at Move/Fire/Over and queued save capture verify integration before/after Exit. Game world stays Speed0 through existing modal-equivalent setup. Linux workflow already selects all TacticalBattle tests, so new cases join its next push automatically.

## Execution evidence — 2026-11-05

AC-06 PASS: all90 independent battle rows each run in both modes through real LocalGameSessionConnection (180 completed battles). Complete final snapshots equal between modes; all published outcomes/turns/losses/carrier hull/shots/hits/rearHits/systems/disabled/reasons/RNG counts equal independent schema2. Queries consume no RNG. No expected data regenerated. Missing source metrics remain explicitly uncovered in reference header. AC-03 integration: real world unchanged and slot bytes preserved at Move/Fire/Over; queued save checks active battle on capture, no directory created; Exit restores saving. Static new-session factories are independent; Client F9/modal guard remains US-0007.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-local-session-01/: build 0 warnings/errors, scoped format/diff PASS, 378/378 TacticalBattle Engine cases PASS (94 new cases, including 90 paired battles and four save integration cases). US-0005 self-review PASS; automated boundary implemented, native/product/independent review not claimed. Latest published prior Linux run42106449393 at9eec6113 PASS; new tests join next workflow push. Separate commit/immediate origin push authorized.
