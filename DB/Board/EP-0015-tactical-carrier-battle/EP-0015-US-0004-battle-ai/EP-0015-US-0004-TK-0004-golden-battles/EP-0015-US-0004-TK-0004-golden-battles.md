---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0004-battle-ai
ticket: EP-0015-US-0004-TK-0004-golden-battles
title: golden-battles
stage: in-progress
layer: engine
depends_on: ["EP-0015-US-0004-TK-0003-interceptor-commander"]
files_touched: 2
serves: [AC-04]
created: 2026-11-05
revision: 3
---

# golden-battles

## Why
Доказать бит-точный порт.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/GoldenBattleTests.cs` | new | 90 боёв battles.json с бросками по этапам и trace-seed-1.json поэтапно |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ReferenceReader.cs` | new | Чтение эталонов из `VData/Planning/V4.0/Reference`, IEEE-hex, заголовок |

## Public API after the change
—

## Implementation steps
1. Читать эталоны из `VData/Planning/V4.0/Reference`.
2. Тест обязательный, не длинный (90 боёв).

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Engine.Tests на Windows и Linux; process-tree cap 300000 ms. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-04 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Source/reader assumption before implementation — 2026-11-05

Reuse existing ReferenceFixtureTests.Open/ReadDoubleHex rather than duplicate a ReferenceReader. New code position is GoldenBattleTests.cs; existing source-fingerprint/schema tests remain mandatory in the TacticalBattle suite. D-USER-14/15 limits expectations to published data: all 90 summaries with exact integer outcomes/counters/RNG; full seed-one trace at original six-decimal precision and exact stage counts/states. No source contains stage calls for other 89 battles, original trace IEEE bits, per-weapon/role counters for all battles or all original trig arguments; those are not fabricated. Full trace test runs one uninterrupted Engine battle without setting source positions/orders/states/RNG into it. Any confirmed production defect expands the owning file scope before its fix.
## Windows validation / Linux CI publication — 2026-11-05

`D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-golden-01/`: solution build 0 warnings/errors; 91 golden cases PASS (90 independent battle summaries + uninterrupted seed-one trace), full TacticalBattle 245/245, source guard 19/19, scoped format/diff PASS. Every summary matches exact outcome/reason/turns/losses/carrier hull/published statistics/RNG. The continuous trace matches every planning/movement/firing stage and final state, without source-state/order/RNG injection. Fractional trace fields use only original 6-decimal precision; no bit-level trace claim is made.

This implementation commit is published to execute the required Linux CI gate; ticket remains in-progress until the actual Linux result is observed and recorded. No next ticket begins before successful publication and Linux verification. Existing workflow runs the identical TacticalBattle suite on Ubuntu/glibc with tracked schema-2 references. Self-review completed; independent review NOT RUN. No production defect or expected-outcome change was required by the Windows golden suite.