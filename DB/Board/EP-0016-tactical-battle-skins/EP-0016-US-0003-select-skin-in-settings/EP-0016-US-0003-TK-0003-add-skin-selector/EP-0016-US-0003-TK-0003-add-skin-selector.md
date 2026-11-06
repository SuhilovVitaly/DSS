---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0003-select-skin-in-settings
ticket: EP-0016-US-0003-TK-0003-add-skin-selector
title: Переключатель скина в настройках
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0016-US-0003-TK-0002-localize-skin-selector]
files_touched: 3
serves: [US3-AC1, US3-AC3]
created: 2026-11-06T08:02:55Z
revision: 1
---

# Переключатель скина в настройках

## Why

Игрок выбирает пакет по имени, а экран явно показывает фактическое состояние.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#подтверждённые-требования) и [утверждение истории](../EP-0016-US-0003-select-skin-in-settings.md#решения-и-согласование). Разрешение касается планирования. Статус тикета присвоен по workflow epic-writer.

## Assumptions

A-09/A-10. Переключатель использует список ID + названий и обратный вызов с результатом записи. Текущий общий экран настроек получает одну новую строку.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsScreen.cs` | SettingsScreen.cs:58, :94–227, :229–258. Рисует три Xenon списка и принимает ввод. | Добавить переключатель с прокруткой, отображением выбора, откатом при ошибке и корректным overlay routing. |
| `src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsLayout.cs` | SettingsLayout.cs:24–50, :62–75. Bounds и hit-test строк. | Разместить строку скина перед выходом и сохранить размеры существующих Xenon контролов. |
| `tests/DeepSpaceSaga.Client.Tests/SettingsScreenTests.cs` | SettingsScreenTests.cs:18, :29, :46 проверяет геометрию и растры. | Уточнить ожидаемую позицию выхода, покрыть выбор, отмену и ошибку. |

## Public API after the change

Сохранить текущий публичный конструктор. Добавить internal overload с IReadOnlyList<(string Id, string DisplayName)> skins, string effectiveSkinId, Func<string,bool> trySelectSkin. Callback вызывается для выбранного валидного ID. Для старого конструктора скрывать новую строку или передавать безопасный одноэлементный источник Classic с сохранением API. SkiaWindow всегда использует новый overload после TK-0004.

## Implementation steps

1. Использовать существующий Xenon dropdown или DropDown<T> с теми же визуальными активами. Панель поддерживает длинный список и узкий viewport. Рисовать options поверх соседних строк, ограничивать высоту и прокручивать список. Контролировать единственный открытый список и закрытие по Esc/клику снаружи. Клик по option не передаётся лежащей ниже строке.
2. Подписать локализованной строкой, использовать стабильные ID как значения. Текущий выбор задаёт caller после разрешения fallback. При успехе обновить текущее отображение, при отказе сохранить прежнее и показать локализованную ошибку.
3. Сохранить работу старых комбобоксов и кнопки выхода. Синхронизировать рисование, границы и hit-test, включая минимальное поддерживаемое окно. В тесте прогнать список больше высоты popup, одинаковые имена разных ID, отмену, успешный и отклонённый выбор.

## Out of scope

Сканирование каталогов, запись на диск, изменение скина текущего боя.

## Invariants

Существующая панель рисует раскрытый список последним (SettingsScreen.cs:249–257). Рендер в Settings проходит вне боевой области стилей.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `SettingsScreenTests.Skin_selector_routes_only_visible_options_and_preserves_selection_on_cancel_or_failure`
- `SettingsScreenTests.Skin_selector_layout_fits_and_does_not_cover_exit_hit_target_when_closed`
- `SettingsScreenTests.Settings_render_smoke_test_draws_the_Type_A_shell`

Будущие команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms и сохранять его вывод рядом с TRX.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~SettingsScreenTests.Skin_selector_routes_only_visible_options_and_preserves_selection_on_cancel_or_failure|FullyQualifiedName~SettingsScreenTests.Skin_selector_layout_fits_and_does_not_cover_exit_hit_target_when_closed|FullyQualifiedName~SettingsScreenTests.Settings_render_smoke_test_draws_the_Type_A_shell" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0016/<run>/us3-tk3" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsScreen.cs src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsLayout.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/SettingsScreenTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги внутри allowlist.
- Для US3-AC1, US3-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены с фактическими результатами. Исходное падение при наличии записано отдельно.
- Соблюдены инварианты и границы изменений. Реальные команды и результат проверок записаны в evidence исполнения.

## Self-containment check

Границы, способ интеграции и источник локализации определены. Тестовый callback позволяет проверять реальное UI без записи пользовательского Settings.json.

## Исполнение

В Settings добавлена строка с каталогом скинов. Список прокручивается и рисуется поверх соседних строк. Отмена и неудачная запись сохраняют прежний ID, отказ показывает локализованную подпись. Выбор различает разные ID с одинаковыми именами. Старый конструктор оставляет прежнюю компоновку.

Сборка и `dotnet test --no-restore --filter "FullyQualifiedName~SettingsScreenTests|FullyQualifiedName~LocalizationCoverageTests"` — 15/15 PASS. Проверены 640×480 и 1280×720, порядок обработки выпадающего списка и прежние активы. Scoped whitespace Client и staged diff check прошли, в тестовом файле formatter отметил только существующий цикл PixelSignature. Самопроверка выполнена. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us3-tk3/focused.trx`.
