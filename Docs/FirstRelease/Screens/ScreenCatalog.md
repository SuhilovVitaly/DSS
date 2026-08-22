# Screen Catalog

Статус: документ первого релиза, сверено с текущим кодом клиента.

Источник схемы: пользовательская схема минимального набора экранов от 2026-08-20. Зеленые узлы на схеме считаются частично реализованными.

## Правило именования

Окно загрузки в требованиях называется `Load`.

Все игровые окна в коде должны начинаться с префикса `Screen`.

Целевой формат имени экранного класса: `Screen{Name}`.

Примеры:

- `ScreenMainMenu`
- `ScreenScenarioSelect`
- `ScreenGameSession`
- `ScreenGameMenu`
- `ScreenSettings`
- `ScreenSave`
- `ScreenLoad`
- `ScreenStation`
- `ScreenTrade`

Интерфейс `IScreen` не попадает под переименование, потому что это общий контракт, а не конкретное окно.

## Стандартный размер окна игровых механик

1400×900 px — стандартный размер панели для окон игровых механик: `Station`, `Trade`, `Hire`, `Cargo`, `Finance`, `Loot`, `Ship`, `Character Communication`, `Dialog`. На этот размер переведены `Finance` (`FinanceLayout.PanelWidth/PanelHeight`) и `Ship` (`ShipLayout.PanelWidth/PanelHeight`).

Закрытие окна игровой механики: кнопка `×`, `Escape` или клик по затемнённому фону за пределами панели — единообразно для всех окон этого стандарта (см. `FinanceScreen`/`ShipScreen`).

Меню/системные экраны (`Main Menu`, `Scenario Select`, `Settings`, `Game Menu`, `Save`, `Load`) в этот стандарт не входят и сохраняют свои текущие размеры.

## Таблица окон

| Окно | Статус по коду | Существующий код | Назначение | Переходы |
|---|---|---|---|---|
| `Main Menu` | Частично реализовано; требует переименования в `ScreenMainMenu` | `src/DeepSpaceSaga.Client/UI/Screens/MainMenu/MainMenuScreen.cs` | Вход в игру. | `Load`, `Settings`, `Scenario Select`. |
| `Scenario Select` | Реализовано; требует переименования в `ScreenScenarioSelect` | `src/DeepSpaceSaga.Client/UI/Screens/ScenarioSelect/ScenarioSelectScreen.cs` | Выбор одного из сценариев `Scenarios/*/scenario.json` (Name + Description) для новой игры. | Из `Main Menu` по `New Game`; выбор сценария (`PLAY`) открывает `Session`; `BACK`/`Escape` возвращают в `Main Menu`. |
| `Load` | Не реализовано отдельным экраном | Disabled-кнопка `LOAD` в `MainMenuScreen` и `GameMenuScreen` | Выбор сохранения для загрузки. | Из `Main Menu` и `Game Menu`; после загрузки открывает `Session`. |
| `Settings` | Частично реализовано; требует переименования в `ScreenSettings` | `src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsScreen.cs` | Клиентские настройки. | Из `Main Menu` и `Game Menu`; закрытие возвращает назад. |
| `Session` | Частично реализовано; требует переименования в `ScreenGameSession` | `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Основной игровой экран с тактической картой. | `Game Menu`, `Station`, `Ship`, `Loot`, `Character Communication`, `Cargo`, `Dialog`. |
| `Game Menu` | Частично реализовано; требует переименования в `ScreenGameMenu` | `src/DeepSpaceSaga.Client/UI/Screens/GameMenu/GameMenuScreen.cs` | Модальное меню поверх сессии. | `Save`, `Load`, `Settings`, возврат в `Session`, выход в `Main Menu`. |
| `Save` | Реализовано; требует переименования в `ScreenSave` | `src/DeepSpaceSaga.Client/UI/Screens/Save/SaveScreen.cs` | Сохранение в слот, overwrite, delete. | Из `Game Menu`; закрытие возвращает в `Game Menu`. |
| `Dialog` | Не реализовано | - | Универсальный линейный диалог. | Из `Session`; закрытие возвращает в предыдущий экран. |
| `Station` | Реализована заглушка (открытие/закрытие/пауза, без данных механик); требует переименования в `ScreenStation` | `src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs` | Hub пристыкованного состояния. | Из `Session` автоматически после успешного `navigation.dock`, либо повторным левым кликом по станции/кораблю игрока, пока корабль пристыкован; закрытие возвращает в `Session` без отмены стыковки. Планируется открывать `Trade`, `Hire`, `Finance`. |
| `Ship` | Реализована заглушка (открытие/закрытие/пауза, без данных механик); требует переименования в `ScreenShip` | `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.cs` | Обзор корабля и доступ к внутренним корабельным действиям. | Из `Session` (кнопка `S` / Ctrl+S на панели механик); открывает `Character Communication`. |
| `Loot` | Не реализовано | - | Подбор/получение добычи, включая результаты добычи льда. | Из `Session`; закрытие возвращает в `Session`. |
| `Character Communication` | Не реализовано | - | Общение с членами экипажа и другими персонажами. | Из `Session` и `Ship`; закрытие возвращает назад. |
| `Cargo` | Не реализовано | - | Просмотр грузового отсека корабля. | Из `Session`; закрытие возвращает в `Session`. |
| `Finance` | Реализована заглушка (открытие/закрытие/пауза, без данных механик); требует переименования в `ScreenFinance` | `src/DeepSpaceSaga.Client/UI/Screens/Finance/FinanceScreen.cs` | Финансовая сводка и станционные финансовые операции первого релиза. | Из `Session` (кнопка `F` / Ctrl+F на панели механик); закрытие возвращает в `Session`. |
| `Hire` | Не реализовано | - | Найм/пассажирские контракты на станции. | Из `Station`; закрытие возвращает в `Station`. |
| `Trade` | Не реализовано | - | Покупка, продажа и заправка `Fuel`. | Из `Station`; закрытие возвращает в `Station`. |

## Минимальный flow

1. `Main Menu` по `New Game` открывает `Scenario Select`; выбор сценария там открывает новую `Session`. `Main Menu` также открывает будущий `Load`.
2. В `Session` игрок управляет кораблем, открывает `Game Menu`, корабельные окна и станционные окна после стыковки.
3. `Game Menu` открывает `Save`, будущий `Load` и `Settings`.
4. `Station` открывается автоматически после успешного `navigation.dock` либо повторным кликом по станции/кораблю игрока и является hub-экраном для `Trade`, `Hire` и `Finance` (пока не реализованы — экран сейчас заглушка).
5. Диалоги персонажей идут через `Dialog` или специализированное окно `Character Communication`, если нужен список/выбор персонажа.
