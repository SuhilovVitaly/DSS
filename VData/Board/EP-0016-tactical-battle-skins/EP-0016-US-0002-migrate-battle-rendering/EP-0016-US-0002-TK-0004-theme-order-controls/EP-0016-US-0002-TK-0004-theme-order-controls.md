---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0002-migrate-battle-rendering
ticket: EP-0016-US-0002-TK-0004-theme-order-controls
title: Оформление приказов движения и стрельбы
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0002-TK-0003-theme-chrome-and-outcome]
files_touched: 7
serves: [US2-AC1, US2-AC2, US2-AC4]
created: 2026-11-06T08:02:55Z
revision: 2
---

# Оформление приказов движения и стрельбы

## Why

Окна приказов движения и стрельбы, элементы выбора и прокрутки входят в пользовательский дизайн.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#подтверждённые-требования) и [утверждение истории](../EP-0016-US-0002-migrate-battle-rendering.md#решения-и-согласование). Разрешение касается планирования. Статус тикета присвоен по workflow epic-writer.

## Assumptions

A-07/A-08. Внешний вид рукоятки, карточки и строки меняется в пределах действующей интерактивной области.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMovePanel.cs` | TacticalBattleMovePanel.cs:33–110, :145. Подписи, ползунки, кнопки, карточки и прокрутка. | Подключить роли orders.move, переключатели и подсказки. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFirePanel.cs` | TacticalBattleFirePanel.cs:30–100, :128. Карточки, список целей, сообщения и оверлей. | Подключить роли orders.fire для всех видимых состояний. |
| `src/DeepSpaceSaga.Client/UI/Controls/Slider.cs` | Slider.cs:80–94. Цвета, фокус, толщина и рукоятка. | Подключить роли ползунка, сохранить управление и диапазон. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MovementOrdersTests.cs` | Текущий набор проверяет приказы движения. Рендер использует TacticalBattleMovePanel.cs:33. | Добавить сравнение растров и команд с темой. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePlanningTests.cs` | Текущий набор проверяет выбор цели. Рендер использует TacticalBattleFirePanel.cs:30. | Добавить проверку вида выбранной цели, popup и режима без выстрела. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkin.cs` | Наследование ролей. | Ограничить поиск локального стиля границей роли виджета. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinRenderScope.cs` | Скоп рендера и локальный fallback. | Сохранить стиль общего контрола при отсутствии явного локального значения. |

## Public API after the change

No API change. Карточки и подписи могут передавать локальную роль в стилизованные перегрузки ControlVisuals/ControlText из TK-0001.

## Implementation steps

1. Подключить роли ползунка, карточек, скролла, текста и состояний приказа. Использовать шрифт и размер скина также при вычислении переноса и clipping.
2. Подключить роли активной карточки и сообщения о недоступной цели. Включить отличимый стиль dropdown/tooltip из TK-0001. Рендерить всплывающий слой в прежнем порядке.
3. Проверить одинаковый результат реальных команд движения и огня после одинакового ввода. Проверить отключённые контролы, hover и clipping на 0.8 и 1.5 UI scale.

## Out of scope

Клавиатурные бинды, формулы движения, прогноз попадания и выбор целей.

## Invariants

Приказы формируют `TacticalBattleScreen.BuildMoveRequest` и `BuildFireRequest`. Стили меняют рисунок, события используют прежние Bounds и состояние.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `MovementOrdersTests.Movement_panel_skin_changes_pixels_and_preserves_issued_order`
- `FirePlanningTests.Fire_panel_skin_changes_selection_and_popup_pixels_without_changing_target`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~MovementOrdersTests.Movement_panel_skin_changes_pixels_and_preserves_issued_order|FullyQualifiedName~FirePlanningTests.Fire_panel_skin_changes_selection_and_popup_pixels_without_changing_target" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us2-tk4" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMovePanel.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFirePanel.cs src/DeepSpaceSaga.Client/UI/Controls/Slider.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MovementOrdersTests.cs tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePlanningTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US2-AC1, US2-AC2, US2-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Указаны все вызываемые виджеты. Остальные общие стили подключены в TK-0001/TK-0003.

## Исполнение

Карточки, прокрутка, подписи, кнопки, сообщения, списки и ползунки читают роли приказов. Локальный стиль переопределяет общий стиль виджета только внутри роли этого виджета. Layout и области ввода используют прежние размеры.

Сборка и `dotnet test --no-restore --filter FullyQualifiedName~TacticalBattle` — 233/233 PASS. Растры приказов при 0.8 и 1.5, геометрия ползунка, выбор без выстрела и запросы проверены с двумя темами. 23/23 PNG Classic идентичны. Scoped whitespace, staged diff check и самопроверка прошли. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us2-tk4/focused.trx`, `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us2-tk4/classic-parity.json`.
