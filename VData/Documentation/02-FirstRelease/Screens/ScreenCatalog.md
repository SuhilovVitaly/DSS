# Экраны и переходы

Оболочка настольного клиента использует [Tabletop](../../03-Design/TabletopClient.md). Оформление активного боя выбирается в настройках. К дополнительным поверхностям относятся экран уничтожения корабля, панель F12 и мастерская портретов.

Экраны реализуют общий IScreen. SkiaWindow управляет стеком и модальными
pause/resume; экран не должен добавлять второй независимый контур игровых часов.
Состояние мира поступает через session/snapshot, а не синхронные Engine-запросы.

| Экран | Назначение |
|---|---|
| [MainMenu](MainMenu.md) | Главное меню |
| [ScenarioSelect](ScenarioSelect.md) | Выбор сценария |
| [GameSession](GameSession.md) | Карта игровой сессии |
| [GameMenu](GameMenu.md) | Меню игровой сессии |
| [Settings](Settings.md) | Настройки |
| [Save](Save.md) | Сохранение игры |
| [Load](Load.md) | Загрузка игры |
| [TacticalBattleSetup](TacticalBattleSetup.md) | Подготовка тактического боя с карты |
| [TacticalBattle](TacticalBattle.md) | Полноэкранная карта, карточка выбранного пилота с командами (отдельная панель в Tabletop), панель фазы и итог |
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
