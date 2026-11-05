---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0005-session-contracts
ticket: EP-0015-US-0005-TK-0005-client-session-handle
title: client-session-handle
stage: done
layer: client
depends_on: ["EP-0015-US-0005-TK-0004-connection-api"]
files_touched: 4
serves: [AC-05]
created: 2026-11-05
revision: 3
---

# client-session-handle

## Why
Экраны вызывают сессию через `GameSessionHandle` (`GameSessionHandle.cs:98,310`).

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/GameSessionHandle.cs` | обёртка соединения для экранов | Методы боя, InterfaceLog команд и отказов |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SessionHandleBattleTests.cs` | new | Проброс, записи InterfaceLog |

## Public API after the change
Методы боя `GameSessionHandle`.

## Implementation steps
1. Отказ превращается в локализованное сообщение в Client, в лог — код и фаза.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-05

Four Client code positions: GameSessionHandle.cs becomes partial, GameSessionHandle.TacticalBattle.cs contains forwarding/logging and localized refusal lookup, UI/InterfaceLog.cs honors the existing absolute DSS_VALIDATION_ROOT diagnostic setting, and SessionHandleBattleTests.cs verifies forwarding/logs/locales/failures. Russian/English locale keys are R-07-exempt. InterfaceLog defaults to existing Interface.log for normal gameplay; diagnostic runs use the configured root without changing cwd or writing repository-root logs. Existing readers use InterfaceLog.FileName and follow the same resolved path. No battle simulation or state cache added to Client; immutable Engine replies flow directly to screens. Transport errors become typed Unavailable plus diagnostic details; caller cancellation remains cancellable.

## Execution evidence — 2026-11-05

AC-05 implemented: six forwarding APIs, original request precision and typed responses retained; every start/move/fire/exit submission and disposition logged with identity/code/phase. Transport failures logged and returned as Unavailable; requested cancellation logged and propagated asynchronously. All 20 refusal codes localized in English/Russian; unknown codes use generic unavailable. No Engine rules/cache in Client. InterfaceLog diagnostic root now follows absolute DSS_VALIDATION_ROOT, default gameplay path preserved; old reader tests follow resolved filename.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-handle-01/: build 0 warnings/errors, scoped format PASS; 33/33 Client cases PASS (new forwarding/log/failure/cancellation/locales, content, existing zoom/scale/log readers). Interface.log retained there, no new root log. Self-review/diff PASS; independent/native acceptance NOT RUN. origin push authorized by explicit override.
