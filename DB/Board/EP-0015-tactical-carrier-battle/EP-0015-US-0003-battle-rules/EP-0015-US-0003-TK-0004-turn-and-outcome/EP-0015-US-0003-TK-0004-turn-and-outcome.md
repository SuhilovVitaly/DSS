---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0003-battle-rules
ticket: EP-0015-US-0003-TK-0004-turn-and-outcome
title: turn-and-outcome
stage: done
layer: engine
depends_on: ["EP-0015-US-0003-TK-0003-salvo"]
files_touched: 3
serves: [AC-04]
created: 2026-11-05
revision: 3
---

# turn-and-outcome

## Why
Цикл хода и исход Combat §4, §7.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattle.cs` | new | Ход, поэтапный API, исход, потери, статистика, Run(cap) |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleOutcome.cs` | new | Исход A/B/draw/timeout, типизированные причины |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/OutcomeTests.cs` | new | Победа, ничья с двумя причинами, timeout, статистика, roleChanges — ручные ситуации |

## Public API after the change
`Step()`, `Run(int? cap)`, внутренние `Planning()`, `Movement()`, `CheckVictory()`, `Firing()`; `Result`.

## Implementation steps
1. Причина — тип, текст — в Client.
2. Статистика §7.1 и `roleChanges` целиком.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Engine.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-04 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## API sequencing assumption before implementation — 2026-11-05

AI policy is the dependent US-0004 slice and does not exist yet. Core cycle takes an explicit required `Action<TacticalBattle>` planner in Planning/Step/Run; no inert/straight-line AI fallback or fake production planner is added. US-0004 and session integration pass the real policy. This keeps the complete phase/RNG/turn/outcome implementation executable and testable now without an Engine → future-type compile dependency. Movement and Firing guard phase order; result is an immutable captured summary. Public player mode never calls capped Run; cap null/zero falls back to seriesTurnCap only in automated Run, matching Spec cap-or-default.
## Execution evidence — 2026-11-05

Implemented guarded Planning → Movement → Firing cycle with victory checks after movement and firing. Caller must supply a real planning policy; US-0004 provides it next. Player-style manual phases have no turn cap. Automated Run uses explicit cap or series default for null/zero, with typed Timeout. Carrier loss takes precedence, draw reasons preserve A/B order. Result captures losses, carrier hull, all §7.1 counters, ordered per-weapon totals, roleChanges and RNG Calls immutably. New planning clears previous phase artifacts; repeated result reads do not repeat effects.

`D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-outcome-01/`: final solution build 0 warnings/errors; scoped format/diff PASS. Fresh regression: Engine general 2459/2459 (Category!=Balance and not CorpusTests, includes all 130 TacticalBattle tests), Client 2324/2324, Motion 1376/1376, Contracts 273/273, Performance 9/9, Economy cargo continuation 2/2: **6443 PASS** across six projects. This excludes 41 legacy Balance, 114 legacy Corpus and 59 remaining Economy cases, whose unchanged baseline is recorded in ContentValidation.md; it is not a fresh full-suite claim. All processes completed inside 300000 ms. Existing Performance tests use CPU/Skia and do not establish native/GPU acceptance.

Eight new outcome cases cover carrier precedence, ordered draw reasons, disabled losses, cap default/zero/explicit, manual turns beyond cap, phase refusal, movement victory before firing and immutable real-salvo statistics/RNG snapshots. Self-review completed; independent review NOT RUN.

Story review: four rules tickets complete. Every assembly field, 19 movement slices and 19 salvo slices match independent published source; exact integer states and RNG stage calls preserved. Full AI planning and unrounded end-to-end 90-battle comparison are US-0004, not claimed here. Battle remains separate from world simulation; real session/save-load/world-pause integration comes in US-0005. Own SHA is verified after publication using user GitHub override.