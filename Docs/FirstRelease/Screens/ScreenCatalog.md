# Screen Catalog

Статус: документ первого релиза, сверено с текущим кодом клиента.

Источник схемы: пользовательская схема минимального набора экранов от 2026-08-20. Зеленые узлы на схеме считаются частично реализованными.

## Правило именования

Окно загрузки в требованиях называется `Load`.

Все игровые окна в коде должны начинаться с префикса `Screen`.

Целевой формат имени экранного класса: `Screen{Name}`.

Примеры:

- `ScreenMainMenu`
- `ScreenGameSession`
- `ScreenGameMenu`
- `ScreenSettings`
- `ScreenSave`
- `ScreenLoad`
- `ScreenStation`
- `ScreenTrade`

Интерфейс `IScreen` не попадает под переименование, потому что это общий контракт, а не конкретное окно.

## Таблица окон

| Окно | Статус по коду | Существующий код | Назначение | Переходы |
|---|---|---|---|---|
| `Main Menu` | Частично реализовано; требует переименования в `ScreenMainMenu` | `src/DeepSpaceSaga.Client/UI/Screens/MainMenu/MainMenuScreen.cs` | Вход в игру. | `Load`, `Settings`, `Session`. |
| `Load` | Не реализовано отдельным экраном | Disabled-кнопка `LOAD` в `MainMenuScreen` и `GameMenuScreen` | Выбор сохранения для загрузки. | Из `Main Menu` и `Game Menu`; после загрузки открывает `Session`. |
| `Settings` | Частично реализовано; требует переименования в `ScreenSettings` | `src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsScreen.cs` | Клиентские настройки. | Из `Main Menu` и `Game Menu`; закрытие возвращает назад. |
| `Session` | Частично реализовано; требует переименования в `ScreenGameSession` | `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Основной игровой экран с тактической картой. | `Game Menu`, `Station`, `Ship`, `Loot`, `Character Communication`, `Cargo`, `Dialog`. |
| `Game Menu` | Частично реализовано; требует переименования в `ScreenGameMenu` | `src/DeepSpaceSaga.Client/UI/Screens/GameMenu/GameMenuScreen.cs` | Модальное меню поверх сессии. | `Save`, `Load`, `Settings`, возврат в `Session`, выход в `Main Menu`. |
| `Save` | Реализовано; требует переименования в `ScreenSave` | `src/DeepSpaceSaga.Client/UI/Screens/Save/SaveScreen.cs` | Сохранение в слот, overwrite, delete. | Из `Game Menu`; закрытие возвращает в `Game Menu`. |
| `Dialog` | Не реализовано | - | Универсальный линейный диалог. | Из `Session`; закрытие возвращает в предыдущий экран. |
| `Station` | Не реализовано | - | Hub пристыкованного состояния. | Из `Session`; открывает `Trade`, `Hire`, `Finance`. |
| `Ship` | Не реализовано | - | Обзор корабля и доступ к внутренним корабельным действиям. | Из `Session`; открывает `Character Communication`. |
| `Loot` | Не реализовано | - | Подбор/получение добычи, включая результаты добычи льда. | Из `Session`; закрытие возвращает в `Session`. |
| `Character Communication` | Не реализовано | - | Общение с членами экипажа и другими персонажами. | Из `Session` и `Ship`; закрытие возвращает назад. |
| `Cargo` | Не реализовано | - | Просмотр грузового отсека корабля. | Из `Session`; закрытие возвращает в `Session`. |
| `Finance` | Не реализовано | - | Финансовая сводка и станционные финансовые операции первого релиза. | Из `Station`; закрытие возвращает в `Station`. |
| `Hire` | Не реализовано | - | Найм/пассажирские контракты на станции. | Из `Station`; закрытие возвращает в `Station`. |
| `Trade` | Не реализовано | - | Покупка, продажа и заправка `Fuel`. | Из `Station`; закрытие возвращает в `Station`. |

## Минимальный flow

1. `Main Menu` открывает новую `Session` или будущий `Load`.
2. В `Session` игрок управляет кораблем, открывает `Game Menu`, корабельные окна и станционные окна после стыковки.
3. `Game Menu` открывает `Save`, будущий `Load` и `Settings`.
4. `Station` является hub-экраном для `Trade`, `Hire` и `Finance`.
5. Диалоги персонажей идут через `Dialog` или специализированное окно `Character Communication`, если нужен список/выбор персонажа.
