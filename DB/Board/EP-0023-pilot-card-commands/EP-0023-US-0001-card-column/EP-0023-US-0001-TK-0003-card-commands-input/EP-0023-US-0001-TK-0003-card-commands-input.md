---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0001-card-column
ticket: EP-0023-US-0001-TK-0003-card-commands-input
title: Команды карточки: реестр, клики, перетаскивание, колесо
stage: done
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0023-US-0001-TK-0001-column-layout]
files_touched: 4
serves: [US1-AC3]
created: 2026-11-11T14:30:00Z
revision: 1
---

# Команды карточки: реестр, клики, перетаскивание, колесо

## Why

Команды выбранного дрона переезжают из правой панели в его карточку (D-02); им нужен общий механизм попаданий, который знает дрона, действие и блокировки ввода.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в EP-0019, EP-0020 и EP-0022. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-05, A-10. Команда существует только на раскрытой карточке выбранного дрона: на остальных карточках прямоугольников команд нет, клик по ним только выбирает дрона.

## Code context

Строки — `main` @ `8b0bf6ce`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | — | Реестр команд кадра: `(SKRect Bounds, string UnitId, string Action)`; регистрируется при отрисовке блока раскрытия. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs` | :36-63 `OverlayClick` (выбор дрона, `_phasePanel.BeginDrag`, кнопки панели), `:64-65` `OverlayDrag`/`EndOverlayDrag` | Перед выбором дрона проверять команду карточки; действия `speed±`, `turn±`, `straight`, `ai`, `target:<id>`, `hold`, `decoy`, `evade` выполнять теми же вызовами `_orders`/`_fireOrders`, что и правая панель; начало и продолжение перетаскивания ползунков карточки. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | :272-317 обработчики мыши | Маршрутизация перетаскивания и колеса для карточки; блокировка при `Pending`, `TurnBusy`, `ConfirmExit`, презентации залпа. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardCommandsInputTests.cs` | — | Новый файл тестов маршрутизации. |

## Public API after the change

`internal SKRect TacticalBattleScreen.CardCommandBounds(string action)` (экранные координаты команды выбранной карточки, пусто — нет команды); `PilotCardColumn.HitCommand(SKPoint) : (string UnitId, string Action)?`; `PilotCardColumn.BeginSlider/DragSlider/EndSlider`.

## Implementation steps

1. Добавить реестр команд и `HitCommand`.
2. Маршрутизировать клик: команда выбранной карточки → действие; тело карточки → выбор дрона; блокировки ввода как у правой панели.
3. Маршрутизировать перетаскивание ползунков карточки и колесо над списком целей карточки (используют US-0002, US-0003).
4. Тестовый доступ `CardCommandBounds` и тесты на синтетических командах.

## Out of scope

Сами блоки команд (US-0002…US-0004).

## Invariants

Клавиатура (Tab, стрелки, E, D, Enter, Esc) работает как сейчас. Выбор цели кликом по карте (`ChooseAt`) работает как сейчас.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `PilotCardCommandsInputTests`
- `KeysDuringBattleTests`
- `MapFireTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotCardCommandsInputTests|FullyQualifiedName~KeysDuringBattleTests|FullyQualifiedName~MapFireTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us1-tk3" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US1-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Список действий и их соответствие вызовам приказов приведён в Code context; геометрия команд — в блоках UiSpec §5–§7.

## Исполнение

US1-AC3: реестр кадра обрезает прямоугольники по viewport, принимает команды выбранного дрона и сбрасывается при новой раскладке. Маршрутизация проверяет текущий выбор и блокировки. 20/20 тестов (реестр, клавиши, цели карты), build, scoped format и diff check прошли. D:/DeepSpaceSaga/artifacts/validation/ep-0023/20261111-execution-01/us1-tk3-02/. Самопроверка diff выполнена. Проверки реальных команд и колеса списка целей выполняются с соответствующими блоками.
