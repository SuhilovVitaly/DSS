---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0006-client-input-and-controls
ticket: EP-0015-US-0006-TK-0001-keyboard
title: keyboard
stage: draft
layer: client
depends_on: ["EP-0015-US-0005-TK-0006-session-tests"]
files_touched: 4
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# keyboard

## Why
Клавиатура для экранов боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs` | фиксированный список | Новые клавиши, модификаторы, автоповтор, сброс при потере фокуса |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | HandleKeyboardEdge | Передача модификаторов и повторов экрану |
| `src/DeepSpaceSaga.Client/UI/Screens/IScreen.cs` | OnKeyDown(Key) | Перегрузка с модификаторами (по умолчанию вызывает старую) |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/KeyboardRoutingTests.cs` | new | Модификаторы, повтор, сброс |

## Public API after the change
`OnKeyDown(Key key, KeyModifiers mods, bool repeat)`.

## Implementation steps
1. Образец передачи модификаторов — `GameSessionScreen.SynchronizeModifiers`.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
