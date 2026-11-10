---
epic: EP-0022-tactical-battle-xenon-star
story: EP-0022-US-0006-read-salvo-results
ticket: EP-0022-US-0006-TK-0001-style-salvo-results
title: Оформить полёт залпа и итог
stage: implemented
layer: client
depends_on: [EP-0022-US-0001-TK-0001, EP-0022-US-0002-TK-0001, EP-0022-US-0005-TK-0001]
files_touched: 5
serves: [AC-01]
created: 2026-11-10T16:52:26Z
revision: 1
---

# Оформить полёт залпа и итог

[История](../EP-0022-US-0006-read-salvo-results.md) · [Эпик](../../Documentation.md)

## Why

Список и карта показывают те же шаги, исходы, урон и действия. Повтор, пропуск, выход и новый бой проходят прежние переходы.

## Decisions

EP-0022 D-01/D-02 задают визуальное направление и геометрию. Запрос исполнения разрешает отдельный commit и push каждого тикета.

## Assumptions

Локально лицензированные срезы подключаются явным свойством сборки `XenonStarSource`. PNG из набора отсутствуют в Git. Общая поставка этих ресурсов остаётся G-01. Используется прежний загрузчик, сохранение выбора и шрифт.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Skins/xenon-star/skin.json` | Роли локального пакета | Добавить роли результатов и модали. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Salvo.cs` | DrawSalvo | Оформить очередь и отчёт. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Outcome.cs` | DrawOutcome | Оформить итог. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs` | DrawSalvoReport | Оформить строки отчёта. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0022SkinScenesTests.cs` | Парные сцены | Проверить полёт, результаты, итог и выход. |

## Public API after the change

Схема сохранения, команды и публичные API прежние.

## Implementation steps

1. Добавить роли результатов и модали.
2. Оформить очередь и отчёт.
3. Оформить итог.
4. Оформить строки отчёта.
5. Проверить полёт, результаты, итог и выход.

## Out of scope

Другие экраны, правила боя, сохранение мира, замена шрифтов и лицензирование от имени владельца.

## Invariants

Макет, координаты ввода, расчёты и авторитет Engine остаются общими для всех скинов. Срезы применяются только к отрисовке.

## Tests

`dotnet test tests/DeepSpaceSaga.Client.Tests -c Release --no-restore --results-directory <absolute-run-dir> --filter "Ep0022SkinScenesTests|PhaseSalvoTests|PhaseOutcomeTests|ResultsPhaseTests|BattleNavigationTests"` с ограничением дерева процессов 300000 ms. Сборка Client, scoped format и `git diff --check`.

## Definition of Done

AC-01 подтверждён целевыми проверками. Реальные результаты, ограничения и проверка публикации фиксируются в Board. Отдельный commit и немедленный push.

## Self-containment check

Production seam и проверка перечислены в Code context. Существующие команды и fixtures сохраняются.

## Execution

- Release Client — 46/46 целевых тестов. Парные кадры проверили полёт, завершение, поглощение и итог. В окне 1280×720 проверена та же команда подтверждения выхода.
- Очередь, применение урона, прокрутка, переходы, повтор и отчёт используют прежние проекции и зоны. Дополнительные метки попадания, промаха и поглощения находятся внутри строки справа от подписи.
- Scoped форматирование и git diff --check прошли.
