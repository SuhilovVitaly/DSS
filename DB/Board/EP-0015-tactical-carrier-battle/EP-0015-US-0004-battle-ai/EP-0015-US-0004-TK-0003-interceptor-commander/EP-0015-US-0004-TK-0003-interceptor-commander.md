---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0004-battle-ai
ticket: EP-0015-US-0004-TK-0003-interceptor-commander
title: interceptor-commander
stage: done
layer: engine
depends_on: ["EP-0015-US-0004-TK-0002-striker-fighter-tail"]
files_touched: 2
serves: [AC-03]
created: 2026-11-05
revision: 3
---

# interceptor-commander

## Why
Командир и заслон под медленный поворот.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/Ai/TacticalCommander.cs` | new | threats, перехватчик, заслон, патруль, командир, переключение, выбор цели |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AiCommanderTests.cs` | new | Угрозы (тест Python `test_threats_by_time_to_carrier`), ветки перехватчика, патруль, заслон, блок смены роли |

## Public API after the change
Внутренний API ИИ.

## Implementation steps
1. Ключ распределения `(не в этой роли, dist)`, первый минимум.
2. Строго `<` для слабого носителя.

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

## Execution evidence — 2026-11-05

Implemented vmax-based stable threats; carrier-near pursuit, engage, screen/hold and parity flank; patrol step at raw patrol speed; commander quotas, current-role-first assignment, minRoleTurns and strict weak-carrier threshold. TacticalBattleAi.Plan composes the real side-role pass and drone-order pass, including forced all-interceptor fallback with typed events and roleChanges. Role maneuver always precedes optional tail replacement. Captured positions remain unchanged during planning; firing selection uses stored role via the existing Salvo default selector.

`D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-commander-01/`: final solution build 0 warnings/errors; Engine TacticalBattle 154/154 (10 new), Motion source guard 19/19; scoped format/diff PASS. All ten first-turn orders and Calls=58 match independent trace. Cases cover vmax vs current-speed threats and inclusive boundaries, stable threat ties, every interceptor branch, active-index flank parity, raw-speed patrol witness, role lock/current-role preference, strict carrier threshold and immediate no-attacker fallback/hunt retention. Initial build found xUnit2031 style error; fixed Assert.Single predicate. Earlier tests/ run used the prior 144-case binary and is excluded; tests-final/ contains the actual 154-case validation. No failed/stale run is reported as coverage of the new code.

Self-review completed; independent review NOT RUN. Prior unchanged broad regression remains 6443 PASS. Whole 90-battle/full trace and platform gates follow in TK-0004. Own SHA verified after explicit user-authorized GitHub publication.