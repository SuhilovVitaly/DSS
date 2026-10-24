# Экраны и переходы

Экраны реализуют общий IScreen. SkiaWindow управляет стеком и модальными
pause/resume; экран не должен добавлять второй независимый контур игровых часов.
Состояние мира поступает через session/snapshot, а не синхронные Engine-запросы.

| Экран | Назначение |
|---|---|
| [MainMenu](MainMenu.md) | Главное меню |
| [ScenarioSelect](ScenarioSelect.md) | Выбор сценария |
| [GameSession](GameSession.md) | Тактический экран |
| [GameMenu](GameMenu.md) | Меню игровой сессии |
| [Settings](Settings.md) | Настройки |
| [Save](Save.md) | Сохранение игры |
| [Load](Load.md) | Загрузка игры |
| [Defeat](Defeat.md) | Поражение |
| [Station](Station.md) | Окно станции |
| [Trade](Trade.md) | Торговля |
| [Finance](Finance.md) | Финансы |
| [Hire](Hire.md) | Найм экипажа |
| [Contracts](Contracts.md) | Пассажирские контракты |
| [Ship](Ship.md) | Обзор корабля |
| [Dialog](Dialog.md) | Диалог |

Hire, Contracts и отдельный Ship — оболочки. Cargo, Loot и отдельное общение с
персонажем не перечисляются как готовые экраны без соответствующего runtime.
Наличие plans/иконки не является реализацией. Диалог используется общим экраном.

Источники: [SkiaWindow.cs](../../../../src/DeepSpaceSaga.Client/UI/SkiaWindow.cs), [IScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/IScreen.cs).
