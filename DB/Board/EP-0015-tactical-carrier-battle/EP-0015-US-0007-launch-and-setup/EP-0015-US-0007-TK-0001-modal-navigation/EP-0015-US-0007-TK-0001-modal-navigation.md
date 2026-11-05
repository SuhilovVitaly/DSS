---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0007-launch-and-setup
ticket: EP-0015-US-0007-TK-0001-modal-navigation
title: modal-navigation
stage: draft
layer: client
depends_on: ["EP-0015-US-0006-TK-0006-text-and-numbers"]
files_touched: 4
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# modal-navigation

## Why
Навигация между картой, подготовкой и боем.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `DB/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `DB/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | PushModalAsync/PopModalAsync, отрисовка стека | События боя, замена внутри модального уровня, непрозрачный модальный экран |
| `src/DeepSpaceSaga.Client/UI/ScreenStack.cs` | Push/Pop/overlay | Признак непрозрачности, Replace |
| `src/DeepSpaceSaga.Client/UI/Screens/IScreen.cs` | `enum ScreenEvent` (`:21`) | `OpenTacticalBattle`, `StartTacticalBattle`, `NewTacticalBattle`, `CloseTacticalBattle` |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs` | new | Пауза один раз, замена, восстановление скорости, нижние экраны не рисуются |

## Public API after the change
События и `ScreenStack.Replace`.

## Implementation steps
1. Существующие модальные экраны ведут себя как раньше.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `DB/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `DB/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
