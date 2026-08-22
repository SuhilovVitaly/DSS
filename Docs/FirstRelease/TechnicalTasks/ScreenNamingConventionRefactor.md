# Screen Naming Convention Refactor

Статус: техническое задание, без реализации в рамках этого документа.

## Цель

Привести имена всех игровых окон клиента к единой конвенции: каждое конкретное окно начинается с префикса `Screen`.

Целевой формат: `Screen{Name}`.

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

## Область применения

Конвенция применяется к конкретным игровым окнам:

- screen class names;
- папкам экранов;
- namespaces экранов;
- test class names, если они явно называются по экрану;
- references в `SkiaWindow`, `Program`, `ScreenStack` usage и других client entry points;
- документации экранов.

Конвенция не применяется к `IScreen`, потому что это общий интерфейс/контракт экранов, а не конкретное окно.

## Уже существующие экраны, требующие переименования

| Текущее имя | Целевое имя | Текущий путь | Целевой путь |
|---|---|---|---|
| `MainMenuScreen` | `ScreenMainMenu` | `src/DeepSpaceSaga.Client/UI/Screens/MainMenu/MainMenuScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/ScreenMainMenu/ScreenMainMenu.cs` |
| `ScenarioSelectScreen` | `ScreenScenarioSelect` | `src/DeepSpaceSaga.Client/UI/Screens/ScenarioSelect/ScenarioSelectScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/ScreenScenarioSelect/ScreenScenarioSelect.cs` |
| `GameSessionScreen` | `ScreenGameSession` | `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/ScreenGameSession/ScreenGameSession.cs` |
| `GameMenuScreen` | `ScreenGameMenu` | `src/DeepSpaceSaga.Client/UI/Screens/GameMenu/GameMenuScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/ScreenGameMenu/ScreenGameMenu.cs` |
| `SettingsScreen` | `ScreenSettings` | `src/DeepSpaceSaga.Client/UI/Screens/Settings/SettingsScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/ScreenSettings/ScreenSettings.cs` |
| `SaveScreen` | `ScreenSave` | `src/DeepSpaceSaga.Client/UI/Screens/Save/SaveScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/ScreenSave/ScreenSave.cs` |
| `StationScreen` | `ScreenStation` | `src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/ScreenStation/ScreenStation.cs` |
| `TradeScreen` | `ScreenTrade` | `src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/ScreenTrade/ScreenTrade.cs` |

## Layout/helper classes

Связанные helper-классы должны переехать вместе с экраном в новую папку/namespace, но не обязаны начинаться с `Screen`, если они не являются окнами.

Примеры:

- `MenuLayout` может остаться `MenuLayout` внутри namespace `...Screens.ScreenMainMenu`.
- `ScenarioSelectLayout` может остаться `ScenarioSelectLayout` внутри namespace `...Screens.ScreenScenarioSelect`.
- `GameMenuLayout` может остаться `GameMenuLayout` внутри namespace `...Screens.ScreenGameMenu`.
- `SaveLayout` может остаться `SaveLayout` внутри namespace `...Screens.ScreenSave`.
- `StationLayout` может остаться `StationLayout` внутри namespace `...Screens.ScreenStation`.
- `TradeLayout` может остаться `TradeLayout` внутри namespace `...Screens.ScreenTrade`.

Если helper-класс является общим для нескольких экранов, его можно оставить вне конкретной папки экрана.

## Будущие экраны первого релиза

Новые экраны должны сразу создаваться в целевой конвенции:

| Окно из ТЗ | Целевой class name |
|---|---|
| `Load` | `ScreenLoad` |
| `Dialog` | `ScreenDialog` |
| `Ship` | `ScreenShip` |
| `Cargo` | `ScreenCargo` |
| `Loot` | `ScreenLoot` |
| `Character Communication` | `ScreenCharacterCommunication` |
| `Finance` | `ScreenFinance` |
| `Hire` | `ScreenHire` |

## Требования к выполнению

1. Переименовать файлы и папки экранов по таблице.
2. Обновить namespaces в переименованных файлах.
3. Обновить все `using` и references в клиентском коде.
4. Обновить test namespaces и test references.
5. Обновить документацию, где упоминаются старые class names или пути.
6. Не менять поведение экранов в рамках этой задачи.
7. Не смешивать это переименование с реализацией новых окон.

## Проверка

- `dotnet test DeepSpaceSaga.sln` проходит.
- `rg "MainMenuScreen|ScenarioSelectScreen|GameSessionScreen|GameMenuScreen|SettingsScreen|SaveScreen|StationScreen|TradeScreen" src tests Docs` не находит старых имен, кроме changelog/исторических заметок, если они будут явно заведены.
- `rg "namespace DeepSpaceSaga.Client.UI.Screens.(MainMenu|ScenarioSelect|GameSession|GameMenu|Settings|Save|Station|Trade)" src tests` не находит старых namespaces.
- Все существующие UI tests проходят без изменения ожидаемого поведения.
