---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0005-session-contracts
ticket: EP-0015-US-0005-TK-0004-connection-api
title: connection-api
stage: done
layer: engine-local-client
depends_on: ["EP-0015-US-0005-TK-0003-world-guard"]
files_touched: 3
serves: [AC-04]
created: 2026-11-05
revision: 3
---

# connection-api

## Why
Транспорт Client ↔ бой.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs` | сессия мира, default-методы | Методы боя default-методами |
| `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs` | локальное соединение | Реализация, сериализация с тиком Engine |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ConnectionApiTests.cs` | new | Вызовы, отказы, отсутствие исключений |

## Public API after the change
Методы AC-04.

## Implementation steps
1. По образцу `TravelStationAsync`/`GetTradeQuoteAsync`.
2. Снимок боя — прямой ответ, не поток мира.
3. Тесты соединения — в Engine.Tests (соглашение `SaveLoadContinuityTests`, `ClusterLocalResumeTests`).

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

## Pre-implementation boundary — 2026-11-05

The card explicitly owns the Contracts interface addition alongside its Engine.LocalClient adapter. Default methods return unavailable/null/typed Unsupported, preserving existing implementations without synchronous UI exceptions. Local calls use the Engine world-state lock already shared with ticks; no new loop or competing state cache. Cancellation/disposal faults are returned as faulted/cancelled ValueTasks, never thrown while obtaining the task. Invalid gameplay requests retain typed Engine responses.

## Execution evidence — 2026-11-05

AC-04 implemented: six cancellable default interface methods; legacy implementations compile unchanged and return typed Unsupported/unavailable. Local adapter delegates to Engine world-state lock shared with ticks. UI call sites obtain ValueTask without synchronous cancellation/disposal/gameplay exceptions. Typed invalid requests retain codes/phases. Actual running snapshot transport + sixteen concurrent starts and sixteen moves prove one authoritative application and stable RNG/revisions. No Client InterfaceLog dependency introduced.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-connection-02/: build 0 warnings/errors, verify-format PASS, 284/284 TacticalBattle Engine + 283/283 Contracts PASS; diff/self-review PASS. Run01 had test-only reference equality for separately captured ImmutableArray records; assertion corrected to compare complete serialized content; no production weakening. Independent review/native NOT RUN. Immediate origin push authorized by user override.
