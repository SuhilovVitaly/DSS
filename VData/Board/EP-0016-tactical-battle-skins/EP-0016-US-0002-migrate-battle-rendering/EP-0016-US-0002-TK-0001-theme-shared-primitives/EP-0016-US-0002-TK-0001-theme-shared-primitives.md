---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0002-migrate-battle-rendering
ticket: EP-0016-US-0002-TK-0001-theme-shared-primitives
title: Оформление общих элементов боя
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0001-TK-0003-ship-reference-pack]
files_touched: 5
serves: [US2-AC1, US2-AC2, US2-AC4]
created: 2026-11-06T08:02:55Z
revision: 2
---

# Оформление общих элементов боя

## Why

Общие виджеты должны получать оформление боя только на время его рендера. Это позволяет использовать пакет в настоящем UI.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#подтверждённые-требования) и [утверждение истории](../EP-0016-US-0002-migrate-battle-rendering.md#решения-и-согласование). Разрешение касается планирования. Статус тикета присвоен по workflow epic-writer.

## Assumptions

A-07. Синхронная область оформления ограничена вызовом Render на UI-потоке и автоматически восстанавливает предыдущее значение.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinRenderScope.cs` | Новый файл. TacticalBattleScreen.Render синхронен (TacticalBattleScreen.cs:110). | Вход в область стилей, чтение свойств, рисование границы/текста/ресурса из роли. |
| `src/DeepSpaceSaga.Client/UI/Controls/TabStrip.cs` | TabStrip.cs:57 и :70. Здесь рисуются вкладки и определены ControlVisuals.Frame/Label. | Подключить стили вкладок, общих рамок и подписей с сохранением прежнего API. |
| `src/DeepSpaceSaga.Client/UI/Controls/DropDown.cs` | DropDown.cs:166, :176, :192, :199, :222, :239, :265. Здесь выпадающий список, ControlText и подсказки. | Оформить закрытый и открытый список, варианты и подсказки. Использовать один шрифт для измерения и рисования. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/WidgetsListsTests.cs` | WidgetsListsTests.cs:13, :61, :104, :173 проверяет списки, перенос и raster. | Тесты состояния стиля и отображения двух скинов. Сохранить действующие проверки ввода. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkin.cs` | Модель v1 и частичное наследование. | Проверка наличия типизированного значения для выбора между штатным PNG виджета и оформлением роли. |

## Public API after the change

Ввести internal `TacticalBattleSkinRenderScope.Enter(TacticalBattleSkin skin)` с `IDisposable`, nullable `Current` и методы чтения ролей. Добавить `EnterRole(string role)` с `IDisposable` и nullable `CurrentRole` для локального оформления вызова общего виджета. Сохранить публичные сигнатуры виджетов. Для выбора локального стиля разрешены необязательные internal overloads у ControlVisuals и ControlText. По умолчанию используются общие роли из карты US-0002.

## Implementation steps

1. Реализовать вложенные области на UI-потоке через сохранение предыдущего скина или локальной роли и восстановление при Dispose. Проверить восстановление обоих значений после вложенного Render и исключения. Если активного скина нет, выполняется штатная ветка общего UI.
2. Заменить прямые стили TabStrip и ControlVisuals, а также closed/popup/options/tooltip DropDown значениями ролей US-0002. Сохранить существующие изображения Xenon, цвета и размеры в штатной ветке; скин может заменить фон или картинку, контур, текст, шрифт и индикатор. Не изменять статические общие paints или bitmap.
3. Подключить чтение шрифта и размера к тем же местам, где выполняются перенос, ellipsis и Draw. Тестом сравнить растры и проверку очистки области после исключения.
4. Снять исходные растры по матрице US-0002 на [проверенной базе](../../Documentation.md#grounding) до выполнения шага 1, включая исправленный шеврон подготовки как контроль изоляции. Шеврон рисует `XenonComboBox.DrawField`; он используется в штатной ветке без изменений.

## Out of scope

Состояние выбора, клавиатура, scrolling, hit-testing, подключение production Render.

## Invariants

Popup рисуется последним и использует те же строки и границы, что ввод (DropDown.cs:162–192, WidgetsListsTests.cs:13–76). Внешние экраны получают прежний вид.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `WidgetsListsTests.Skin_scope_is_restored_after_nested_render_and_exception`
- `WidgetsListsTests.Styled_tabs_popup_and_tooltip_use_overrides_without_changing_unscoped_controls`
- `WidgetsListsTests.RasterPopupFits1280By720AndLabelsAndWrappedJournalStayClipped`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~WidgetsListsTests.Skin_scope_is_restored_after_nested_render_and_exception|FullyQualifiedName~WidgetsListsTests.Styled_tabs_popup_and_tooltip_use_overrides_without_changing_unscoped_controls|FullyQualifiedName~WidgetsListsTests.RasterPopupFits1280By720AndLabelsAndWrappedJournalStayClipped" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us2-tk1" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinRenderScope.cs src/DeepSpaceSaga.Client/UI/Controls/TabStrip.cs src/DeepSpaceSaga.Client/UI/Controls/DropDown.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/WidgetsListsTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US2-AC1, US2-AC2, US2-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Scope, общие роли и исходные виджеты определены. Render подключается следующим тикетом.

## Исполнение

Синхронная область восстанавливает скин и роль при вложенном вызове и исключении. Общие рамки, подписи, вкладки, список и подсказки принимают переопределения. Штатные Xenon PNG сохраняются при отсутствии визуальных переопределений. Для стилизованных строк одна метрика используется для измерения и вывода. При отсутствии глифов берётся штатный шрифт.

- Исходная отрисовка из 81e6d87183504b1e6f2e4977c112d57cd40c7923, снятая на 3842ca16 до изменения рендера: 186/186 тактических тестов PASS. Windows, Skia CPU raster, входы и масштаб фиксируют существующие тестовые сцены. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/baseline/`.
- `dotnet test --no-restore --filter "FullyQualifiedName~WidgetsListsTests|FullyQualifiedName~SetupScreenTests"`: 26/26 PASS. Восемь PNG списков и подготовки совпали побайтно с исходными. Тест сравнил Classic, альтернативную роль и выход из области. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us2-tk1/focused.trx`.
- Сборка, scoped whitespace и staged diff check прошли. Проверены US2-AC1/2/4 в части общих виджетов. Production Render подключается следующим тикетом. Нативная проверка здесь не проводилась.
