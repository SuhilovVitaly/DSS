---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0004-battle-ai
ticket: EP-0015-US-0004-TK-0002-striker-fighter-tail
title: striker-fighter-tail
stage: done
layer: engine
depends_on: ["EP-0015-US-0004-TK-0001-steering"]
files_touched: 4
serves: [AC-02]
created: 2026-11-05
revision: 3
---

# striker-fighter-tail

## Why
Ролевые манёвры и поиск хвоста.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/Ai/TacticalRoles.cs` | new | Ударник, истребитель |
| `src/DeepSpaceSaga.Engine/TacticalBattle/Ai/TacticalTail.cs` | new | Выход в хвост |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AiRolesTests.cs` | new | Гистерезис in/out, extend/break/hunt, хвост: 0/1 бросок, равенство score, takesTail |

## Public API after the change
Внутренний API ИИ.

## Implementation steps
1. Скорости хвоста — без повторов по точному равенству, по возрастанию (устойчиво).
2. Пре-фильтр строго `>`.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Engine.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-05

Add `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs`: per-battle ordered typed AI event list for fighter-break/role-change evidence. Event types live alongside TacticalRoles, no localized strings or InterfaceLog dependency. Commander/whole-side planning clears/captures this list in the next slice; Client rendering follows later. Four code positions total in Engine with tests. Role maneuver runs before optional tail replacement, preserving role-side effects and RNG; inactive/jammed/no-candidate tail searches consume zero draws, found candidate consumes exactly one even when chance is zero.
## Execution evidence — 2026-11-05

Implemented strict-break/inclusive-extend striker hysteresis, straight fighter extend/break, persistent hunt with first-nearest replacement and typed AI events. Tail grid uses exact speed de-duplication then stable ascending order, enemy-list/speed/turn iteration and strictly lower score. It predicts enemies from current speed, applies exact reach/rear/cone gates and consumes one draw only after a best candidate exists. Role maneuver runs before optional tail replacement; takesTail gates only effective striker.

`D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-roles-01/`: solution build 0 warnings/errors, Engine TacticalBattle 144/144 (6 new), Motion source guard 19/19, scoped format/diff PASS. Tests cover in/out boundaries, straight break outside leash, extend counters/event, hunt surviving role changes, symmetric equal-score enemy-order witness, duplicate clamped speeds, zero-chance one draw, jammed/no-candidate zero draws and role-before-tail RNG order. Prior unchanged regression 6443 PASS remains recorded; no fresh full-suite claim. Self-review completed; independent review NOT RUN. Whole-policy production wiring is the next commander slice, with no fake interim planner. Own SHA verified after user-authorized GitHub publication.