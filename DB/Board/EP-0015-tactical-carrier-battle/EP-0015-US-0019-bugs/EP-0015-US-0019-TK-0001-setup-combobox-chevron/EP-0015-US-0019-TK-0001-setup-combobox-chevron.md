---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0001-setup-combobox-chevron
stage: done
layer: client
depends_on: []
files_touched:
  - src/DeepSpaceSaga.Client/UI/Controls/XenonComboBox.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/WidgetsListsTests.cs
---

# Пропорции шеврона в выпадающих списках

## Контракт и критерий AC-01

В широких полях подготовки шеврон сохраняет пропорции и не перекрывает подпись. Узкие поля остаются пригодными к отрисовке. Ввод и выбор продолжают работать.

## Code context и реализация

XenonComboBox.DrawClosed растягивает изображение поля целиком; DropDown размещает текст после фиксированного отступа. Сохранить края изображения и растягивать центральный фон.

## Tests

Фильтры `WidgetsListsTests|SetupScreenTests` в соответствующих DeepSpaceSaga.Client.Tests. Проверить изменённые файлы через `git diff --check`.

## Definition of Done

Критерий покрыт проверкой в Dss-Main. Результат проверок записан отдельно от ручной приёмки. Отдельный коммит опубликован в ветке эпика.

## Исполнение

Коммит `85580bab` опубликован в ветке эпика. 24/24 в WidgetsListsTests и SetupScreenTests. Проверены одинаковые размеры шеврона в обычном и сфокусированном поле разной ширины. Ручная приёмка открыта.
