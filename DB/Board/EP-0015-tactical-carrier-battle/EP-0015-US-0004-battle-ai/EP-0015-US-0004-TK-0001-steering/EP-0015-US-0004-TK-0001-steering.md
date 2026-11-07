---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0004-battle-ai
ticket: EP-0015-US-0004-TK-0001-steering
title: steering
stage: done
layer: engine
depends_on: ["EP-0015-US-0003-TK-0004-turn-and-outcome"]
files_touched: 2
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# steering

## Why
Руление и погоня ИИ.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/Ai/TacticalSteering.cs` | new | steer, привязь, slowForTurn, predict, pursue, действующая роль |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AiSteeringTests.cs` | new | Привязь не меняет скорость; середина привязи; slowForTurn рекурсивен; vmax ≤ vmin |

## Public API after the change
Внутренний API ИИ.

## Implementation steps
1. vₖ = vₖ₋₁ + (L − vₖ₋₁)·k/n.
2. Скорость погони — от точки с ошибкой прицеливания.

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

Implemented Spec steer/leash/recursive slowForTurn, current-speed prediction, two-draw pursue and effective role. All tuning comes from captured content; shared Motion owns limits/geometry. Leash redirects only the point to the two-carrier midpoint; pursue speed uses the noisy aim point; effective role leaves stored role/hunt unchanged.

`D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-steering-01/`: solution build 0 warnings/errors, Engine TacticalBattle 138/138 (8 new), Motion source guard 19/19, scoped format/diff PASS. Independent first trace pursuit order matches within original half-micro precision and consumes X then Y draws (Calls42→44). Tests cover midpoint vs own carrier, preserved passed speed, recursive second-step speed and early stop, vmax=min, prediction above damaged limits and inactive enemies. Full unchanged regression from prior rules story remains 6443 PASS; no redundant fresh full-suite claim. Temp/fixtures are redirected into this absolute run root.

Self-review completed; independent review NOT RUN. Real whole-side planning is wired after the dependent roles/commander pieces, not replaced by a fallback policy. Own SHA verified after user-authorized GitHub publication.