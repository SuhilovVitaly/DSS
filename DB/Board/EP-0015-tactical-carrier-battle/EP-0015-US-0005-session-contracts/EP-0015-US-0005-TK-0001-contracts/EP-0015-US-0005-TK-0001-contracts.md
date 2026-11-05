---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0005-session-contracts
ticket: EP-0015-US-0005-TK-0001-contracts
title: contracts
stage: done
layer: contracts
depends_on: ["EP-0015-US-0004-TK-0004-golden-battles"]
files_touched: 5
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# contracts

## Why
Неизменяемые DTO боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | new | Снимок и вложенные DTO, типизированные причины и события |
| `src/DeepSpaceSaga.Contracts/TacticalBattleOptions.cs` | new | Options DTO, доступность |
| `src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs` | new | Start/Move/Fire/Exit, ответ, коды отказа |
| `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs` | снимок мира | `IsTacticalBattleActive` |
| `tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs` | new | Roundtrip, неизменяемость, полнота полей |

## Public API after the change
Records в Contracts.

## Implementation steps
1. Поля AC-01 полностью; роли, тактики, рамы — id, не русские строки.
2. Журнал — enum события + параметры + ход + фаза, без готовых строк.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Contracts.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Contract assumptions before implementation — 2026-11-05

Commands carry CommandId; move/fire also carry BattleId + ExpectedRevision to reject duplicate/stale cross-turn or cross-battle effects before RNG mutation. Exit carries BattleId but no revision so cancellation of the current battle can finish after an in-flight phase. Engine ownership/cache behavior follows in TK-0002. No gameplay rule changes. Snapshot includes turn on paths/shots and immutable BeforeSalvoUnits for auto-fire animation, where Client has no intermediate fire snapshot. This supplies the existing Display §3 requirement without reconstructing authoritative pre-damage state in Client. Events/reasons are enum+typed fields, not localized text. Requests use immutable arrays instead of mutable dictionaries; Engine detects duplicate drone IDs. Default array serializes empty; malformed explicit array values must fail JSON parsing. World active flag defaults false for legacy producers. Five code positions stay unchanged.
## Execution evidence — 2026-11-05

Added immutable request/options/frame DTOs with typed phases, units, reasons, events, modules, skills, motion limits, unrounded proposals/targets/incoming, ordered paths/shots, pre-salvo units, losses, statistics and full/delta journals. Command identity + battle/revision are explicit; default request arrays serialize empty and malformed/null arrays fail. Added optional IsTacticalBattleActive=false to world snapshot without save-schema changes. Contracts carry data only and do not reference Engine/Motion/Client.

`D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-contracts-01/`: solution build 0 warnings/errors; full Contracts 283/283 (10 added), Engine TacticalBattle 245/245 including golden suite; scoped format/diff PASS. Tests cover nested roundtrip, signed-zero/exact double, long seeds, request identity/revision, null target, default/invalid arrays, setup ordering/ranges/slots, immutable source copies, journal delta vs full history and legacy false world flag.

Client regression: broad run 2323 passed / 1 failed (15-second refuel wait in Every_demo_station_docks_and_trades_via_session for SPC-MARKET-INDUSTRIAL). Unchanged isolated run of all five market cases passed; no timer/assertion or production trade code changed. TRX inventory comparison in client-coverage.json proves 2324/2324 distinct cases have passing results, zero missing. This is combined case coverage, not an uninterrupted full-green run; original timeout log is retained. The failure did not reproduce in isolation. Earlier broad 6443 regression remains historical evidence, not a replacement claim.

Self-review completed for ownership/serialization/identity and UI-required captured data. Independent review NOT RUN. DTOs become production behavior in dependent session/transport tickets; adding the flag alone does not activate a battle. Golden evidence completion SHA 682a9b758724922b6b91c49c2d523e078afd9020 is published and verified. Own SHA verified after user-authorized publication.