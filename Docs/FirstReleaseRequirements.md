# Deep Space Saga - требования к первому релизу

Дата начала фиксации: 2026-08-16

Основной источник истины по движку: `deep_space_saga_engine_requirements.md`.

Назначение документа: собрать границы первого релиза DSS как минимального игрового скелета. Этот файл описывает релиз в целом; детализация экранов и механик хранится в `Docs/FirstRelease/`.

## Цель первого релиза

Первый релиз должен дать игроку минимальный, но связный игровой цикл:

1. Игрок видит Солнечную систему на тактической карте.
2. Игрок управляет кораблем Tetrarch Class и маневрирует к объектам.
3. Игрок использует командные панели Navigation, Maneuver, Engine и Space Control.
4. Игрок стыкуется со станцией командой `navigation.dock` навигационного компьютера.
5. Игрок открывает экран станции и с него переходит к торговле.
6. Игрок покупает и продает товары за `Credits`.
7. Игрок перевозит товары в грузовом отсеке.
8. Игрок добывает лед с астероидов через Drilling Unit и продает его.
9. Игрок следит за запасом `Fuel` в баках двигателя и пополняет его.
10. Игрок хранит, покупает и продает `Energy Cells` как товар для реактора/электросистемы.
11. Игрок начинает с `0 Credits`, а станции имеют собственный запас `Credits`.
12. Игрок имеет экипаж как людей на корабле, размещенных в каютах `Living quarters`.
13. Стартовый экипаж состоит из одного персонажа: главного героя.
14. Игрок общается с членами экипажа и торговым агентом станции через линейные диалоги.

## Документы релиза

- `Docs/FirstRelease/Screens/MainMenu.md` - главное меню.
- `Docs/FirstRelease/Screens/ScenarioSelect.md` - экран выбора сценария новой игры.
- `Docs/FirstRelease/Screens/ScreenCatalog.md` - минимальный набор окон первого релиза и связи между ними.
- `Docs/FirstRelease/Screens/GameSession.md` - экран игровой сессии и тактическая карта.
- `Docs/FirstRelease/Screens/Station.md` - экран пристыкованной станции.
- `Docs/FirstRelease/Screens/Trade.md` - экран торговли.
- `Docs/FirstRelease/Screens/GameMenu.md` - игровое меню поверх сессии.
- `Docs/FirstRelease/Screens/Settings.md` - настройки.
- `Docs/FirstRelease/Screens/Save.md` - окно сохранения.
- `Docs/FirstRelease/Screens/Load.md` - окно загрузки.
- `Docs/FirstRelease/Screens/Dialog.md` - универсальное окно диалога.
- `Docs/FirstRelease/Screens/Ship.md` - окно корабля.
- `Docs/FirstRelease/Screens/Cargo.md` - окно грузового отсека.
- `Docs/FirstRelease/Screens/Loot.md` - окно подбора/добычи предметов.
- `Docs/FirstRelease/Screens/CharacterCommunication.md` - окно общения с персонажами.
- `Docs/FirstRelease/Screens/Finance.md` - окно финансов.
- `Docs/FirstRelease/Screens/Hire.md` - окно найма.
- `Docs/FirstRelease/TechnicalTasks/ScreenNamingConventionRefactor.md` - техническое задание на приведение существующих экранов к naming convention.
- `Docs/FirstRelease/Mechanics/TetrarchClass.md` - стартовый корабль игрока Tetrarch Class.
- `Docs/FirstRelease/Mechanics/CommandPanels.md` - смысловые командные панели корабля.
- `Docs/FirstRelease/Mechanics/TacticalMapAndManeuvering.md` - тактическая карта и маневрирование.
- `Docs/FirstRelease/Mechanics/Docking.md` - стыковка.
- `Docs/FirstRelease/Mechanics/Trading.md` - торговля на станциях.
- `Docs/FirstRelease/Mechanics/StationInventory.md` - конечный склад станции.
- `Docs/FirstRelease/Mechanics/Money.md` - `Credits` и стартовые балансы.
- `Docs/FirstRelease/Mechanics/CargoHold.md` - грузовой отсек.
- `Docs/FirstRelease/Mechanics/CrewAndHabitation.md` - экипаж, пассажиры и жилой модуль.
- `Docs/FirstRelease/Mechanics/CrewDialogues.md` - экипаж и диалоги.
- `Docs/FirstRelease/Mechanics/PassengerContracts.md` - пассажирские контракты на станциях.
- `Docs/FirstRelease/Mechanics/StationDialogues.md` - диалоги с представителями станций.
- `Docs/FirstRelease/Mechanics/IceMining.md` - добыча льда.
- `Docs/FirstRelease/Mechanics/Fuel.md` - топливо двигателя.
- `Docs/FirstRelease/Mechanics/ElectricityAndEnergyCells.md` - реактор, электричество и `Energy Cells`.

## Уже существующая основа в коде

- Архитектурная граница `Client -> Engine.LocalClient -> Engine` уже существует.
- `GameSessionScreen` уже содержит тактическую карту, камеру, масштабирование, панель скоростей, панель команд модулей, выбор объектов, подписи, трейлы и прогноз движения.
- Движение корабля уже основано на module-addressed командах двигателя.
- Стартовый корабль игрока заменен на Tetrarch Class: `Navigation Computer`, `Living quarters`, `Cargo hold`, `Scanner`, `Reactor`, `Engine`.
- В данных уже есть стартовые модули Tetrarch: `module.bridge.navigation.computer.basic`, `living.quarters.mk1`, `module.container.basic`, `module.scanner.mk1`, `module.generator.basic`, `module.engine.basic`.
- В каталоге также остаются будущие/не стартовые модули: `Drilling Unit`, `Battery`, `Combat Laser`, старый `Habitation Module`.
- `module.engine.basic` уже содержит `fuelCapacityKg`, что подходит для решения первого релиза: `Fuel` хранится в баках двигателя.
- В требованиях уже есть hull grid Tetrarch, грузовая вместимость по массе, `Energy Cells` как ресурс реактора/генератора и `Fuel` как ресурс двигателя.
- Экранный стек уже поддерживает `MainMenu`, `ScenarioSelect`, `GameSession`, `GameMenu`, `Settings` и модальную паузу.
- По схеме минимального набора экранов первого релиза частично реализованными считаются `MainMenu`, `Settings`, `Session`/`GameSession` и `GameMenu`.
- В коде также уже существует `SaveScreen`.
- `New Game` из `MainMenu` открывает `ScenarioSelectScreen` (полностью реализован, не заглушка): список сценариев `Scenarios/*/scenario.json` с `Name`/`Description`, `PLAY` стартует сессию из выбранного файла. `scenarioMetadata` получил необязательное поле `description` для этого экрана.
- `navigation.dock` теперь настоящая authoritative команда (`SimulationEngine.TryStartNavigationCommand`), а не catalog-only заглушка: проверяет target/дистанцию (`rangeKm` из command definition)/синхронизацию, при успехе синхронизирует корабль со станцией и выставляет `IsDocked`/`DockedStationObjectId` (персистентны через save/load). `StationScreen` (заглушка, по паттерну `Finance`/`Ship`) открывается автоматически после успешного дока или повторным кликом по станции/кораблю игрока. Отстыковка не реализована. Детали: `Docs/FirstRelease/Mechanics/Docking.md`, `Docs/FirstRelease/Screens/Station.md`.
- Клик по тактической карте теперь выбирает объект по приоритету типа, если в 30 px радиусе несколько объектов сразу: `Station` > корабль игрока > другой корабль > всё остальное; расстояние до курсора — только tie-break внутри одного приоритета (`GameSessionScreen.FindNearestObjectId`).
- Кнопка `TRADE` на `StationScreen` реальная (не placeholder-строка): открывает `TradeScreen` (тоже заглушка) вложенным modal поверх `Station`.
- Кнопка `HIRE` на `StationScreen` тоже реальная: открывает `HireScreen` (заглушка) тем же вложенным modal поверх `Station`.
- `Load` присутствует как кнопка в `MainMenu` и `GameMenu`, но отдельного `LoadScreen` пока нет; название окна в требованиях первого релиза - `Load`.
- Naming convention для игровых окон: каждое окно в коде должно начинаться с `Screen`, например `ScreenMainMenu`, `ScreenGameSession`, `ScreenGameMenu`, `ScreenSettings`, `ScreenSave`.
- Текущие частично/полностью реализованные экранные классы пока не соответствуют новой конвенции, потому что используют суффикс `Screen`; их переименование описано в `Docs/FirstRelease/TechnicalTasks/ScreenNamingConventionRefactor.md`.

## Минимальный набор окон первого релиза

Таблица ниже сверена с текущим кодом клиента и схемой экранов от 2026-08-20. Зеленые узлы на схеме трактуются как частично реализованные.

| Окно | Статус по коду | Документ | Основные переходы первого релиза |
|---|---|---|---|
| `Main Menu` | Частично реализовано: `MainMenuScreen`; `Load` нарисован, но отключен. | `Docs/FirstRelease/Screens/MainMenu.md` | `Load`, `Settings`, `Scenario Select`. |
| `Scenario Select` | Реализовано: `ScenarioSelectScreen`. | `Docs/FirstRelease/Screens/ScenarioSelect.md` | Из `Main Menu` по `New Game`; выбор сценария открывает `Session`; `BACK`/`Escape` возвращают в `Main Menu`. |
| `Load` | Не реализовано отдельным экраном; есть disabled-кнопка `LOAD` в `MainMenu` и `GameMenu`. | `Docs/FirstRelease/Screens/Load.md` | Из `Main Menu` и `Game Menu`; после выбора сохранения открывает `Session`. |
| `Settings` | Частично реализовано: `SettingsScreen`. | `Docs/FirstRelease/Screens/Settings.md` | Из `Main Menu` и `Game Menu`; закрытие возвращает на предыдущий экран. |
| `Session` | Частично реализовано как `GameSessionScreen`. | `Docs/FirstRelease/Screens/GameSession.md` | Открывает `Game Menu`, `Station`, `Ship`, `Loot`, `Character Communication`, `Cargo`, `Dialog`. |
| `Game Menu` | Частично реализовано: `GameMenuScreen`; `Load` и `Settings` нарисованы, но пока отключены. | `Docs/FirstRelease/Screens/GameMenu.md` | `Save`, `Load`, `Settings`, возврат в `Session`, выход в `Main Menu`. |
| `Save` | Реализовано как `SaveScreen`. | `Docs/FirstRelease/Screens/Save.md` | Из `Game Menu`; закрытие возвращает в `Game Menu`. |
| `Dialog` | Не реализовано. | `Docs/FirstRelease/Screens/Dialog.md` | Из `Session` для простых линейных диалогов. |
| `Station` | Реализована заглушка: `StationScreen`. | `Docs/FirstRelease/Screens/Station.md` | Из `Session` автоматически после успешного `navigation.dock`, либо повторным кликом по станции/кораблю игрока; закрытие возвращает в `Session`. Планируется открывать `Trade`, `Hire`, `Finance`. |
| `Ship` | Не реализовано. | `Docs/FirstRelease/Screens/Ship.md` | Из `Session`; открывает `Character Communication`. |
| `Loot` | Не реализовано. | `Docs/FirstRelease/Screens/Loot.md` | Из `Session`; используется для результатов добычи/подбора. |
| `Character Communication` | Не реализовано. | `Docs/FirstRelease/Screens/CharacterCommunication.md` | Из `Session` и `Ship`; показывает общение с членами экипажа и персонажами. |
| `Cargo` | Не реализовано. | `Docs/FirstRelease/Screens/Cargo.md` | Из `Session`; показывает грузовой отсек корабля. |
| `Finance` | Не реализовано. | `Docs/FirstRelease/Screens/Finance.md` | Из `Station`; показывает финансовые станционные операции/сводку. |
| `Hire` | Реализована заглушка: `HireScreen`. | `Docs/FirstRelease/Screens/Hire.md` | Из `Station` (кнопка `HIRE`, вложенный modal); закрытие возвращает в `Station`. Найм/пассажирские контракты не реализованы. |
| `Trade` | Реализована заглушка: `TradeScreen`. | `Docs/FirstRelease/Screens/Trade.md` | Из `Station` (кнопка `TRADE`, вложенный modal); закрытие возвращает в `Station`. Покупка, продажа и заправка `Fuel` не реализованы. |

## Naming convention экранов

Все игровые окна в коде должны начинаться с префикса `Screen`.

Правильный формат имени экранного класса: `Screen{Name}`.

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
- `ScreenHire`

Папки, namespaces, layout/helper-классы и тесты должны быть приведены к этой модели отдельной технической задачей. Интерфейс `IScreen` не переименовывается, потому что это контракт типа, а не конкретное окно.

## Решения первого релиза

- Стартовый корабль первого релиза - Tetrarch Class.
- `Living quarters MK I` имеет `cabines = 2`.
- Стартовый экипаж - `1` персонаж, главный герой.
- Командные панели первого релиза: Navigation, Maneuver, Engine, Space Control.
- `scanner.nearbySignatures` показывается на панели Space Control.
- Mining capability реализуется через Drilling Unit и команды `mining.extractIce`, `mining.stopExtraction`.
- `Energy Cells` остаются в игре как отдельный товар.
- `Energy Cells` можно складировать в грузовом контейнере, покупать и продавать.
- `Energy Cells` нужны для реактора/генератора и электричества, питающего active modules.
- Электрическая модель MVP ограничивается расходом `Energy Cells` реактором/генератором: `1 Energy Cell` за `1` игровой час. Все active modules требуют работающий реактор/генератор.
- `Fuel` тоже является покупаемым товаром/ресурсом, но не хранится в cargo/container module.
- Единица измерения `Fuel` в первом релизе: kg.
- `Fuel` хранится только во внутренних баках двигателя.
- Команды двигателя/навигации в MVP первого релиза не расходуют `Fuel`.
- Вместимость `Fuel` задается параметром двигателя, например существующим `fuelCapacityKg`.
- Пополнение `Fuel` происходит через станционные сервисы/торговлю в пристыкованном состоянии и заполняет баки двигателя, а не грузовой отсек.
- У игрока на старте новой игры `0 Credits`.
- У каждой станции есть `Credits`; если они не заданы явно, Engine генерирует сумму от `10 000` до `50 000` включительно.
- Баланс `Credits` станции скрыт от игрока.
- Если станции не хватает `Credits` на покупку всего выбранного товара игрока, разрешается частичная продажа на доступную сумму.
- Склад станции конечный.
- Все товары станции в первом релизе генерируются в случайном количестве от `20` до `500` единиц включительно.
- Стартовые товары станции первого релиза: `Energy Cells`, `Fuel`, `Ice`.
- Экран торговли открывается кнопкой с экрана станции.
- `Dock` является командой навигационного компьютера.
- Доступная дистанция `Dock`: `< 200 km`; значение задается параметром команды `Dock` навигационного компьютера.
- `Dock` требует выбранную станцию, синхронизированную скорость и синхронизированное направление.
- Отстыковка является станционным authoritative-действием, а не отдельной module command первого релиза.
- При успешном `Dock` корабль синхронизирует позицию/скорость/движение со станцией.
- Local offset корабля после `Dock` использует старую модель синхронизации: `(1, 1)` world unit.
- Экран станции открывается автоматически после подтверждения `Dock`.
- Экраны станции, торговли и диалогов являются modal screens и ставят authoritative simulation на паузу.
- Цены товаров фиксированные по базовой цене с коэффициентом станции от `0.5` до `2.0`.
- Базовые цены: `Energy Cells = 200 Credits`, `Fuel = 200 Credits`, `Ice = 30 Credits`.
- Итоговая цена после коэффициента станции округляется до ближайшего целого `Credits`.
- В будущих версиях цены будут зависеть от событий на станции и от того, что станция производит.
- Экипаж - это люди на корабле, размещенные в каютах жилого модуля.
- Пассажиры являются active gameplay entity первого релиза и используют ту же модель кают, что и экипаж.
- На станции можно получить контракт на перевозку пассажира.
- Станционный представитель - персонаж; в MVP реализуется торговый агент для операций покупки и продажи товаров.
- Диалоги первого релиза линейные.

## Крупные механики первого релиза

### Tetrarch Class

Tetrarch Class является стартовым кораблем игрока. В стартовую комплектацию входят Navigation Computer, Living quarters, Cargo hold, Scanner, Reactor и Engine. Старые стартовые модули `Battery`, `Drilling Unit`, `Combat Laser` и старый `Habitation Module` не входят в новую стартовую комплектацию.

Детали: `Docs/FirstRelease/Mechanics/TetrarchClass.md`.

### Командные панели

Команды корабля группируются по смыслу в четыре панели: Navigation, Maneuver, Engine и Space Control. Панели отображают module-addressed команды Navigation Computer, Engine, Scanner и Drilling Unit. `scanner.nearbySignatures` показывается на Space Control panel.

Детали: `Docs/FirstRelease/Mechanics/CommandPanels.md`.

### Тактическая карта и маневрирование

Игрок управляет кораблем на `GameSessionScreen`: масштабирует карту, выбирает объекты, отдает команды через панели Navigation, Maneuver, Engine и Space Control.

Детали: `Docs/FirstRelease/Mechanics/TacticalMapAndManeuvering.md`.

### Стыковка

Станция доступна через явную стыковку. `Dock` является командой Navigation Computer. `Dock` доступен при выбранной станции, дистанции `< 200 km`, синхронизированной скорости и синхронизированном направлении. Успешная стыковка синхронизирует корабль со станцией с local offset `(1, 1)` world unit и автоматически открывает экран станции. Отстыковка выполняется как станционное действие.

Реализовано как MVP: сама команда `navigation.dock` (валидация + физическая синхронизация + authoritative `IsDocked`/`DockedStationObjectId`, персистентные через save/load) и автоматическое открытие `StationScreen`-заглушки. Не реализовано: отстыковка и блокировка обычных engine-команд корабля во время дока.

Детали: `Docs/FirstRelease/Mechanics/Docking.md`.

### Экран станции

Экран станции является modal hub-экраном пристыкованного состояния. С него игрок открывает торговлю, диалоги представителей станции, покупку/установку Drilling Unit, пассажирские контракты и отстыковку; пока экран открыт, симуляция на паузе.

Реализована заглушка (`StationScreen`, открытие/закрытие/пауза): открывается автоматически после успешного `Dock` или повторным кликом по станции/кораблю игрока, закрывается `×`/`Escape`/кликом по фону без отмены стыковки. Показывает placeholder-строки вместо реальных Trade/Finance/Representatives/Install Drilling Unit/Hire/Undock — ни один из них ещё не реализован.

Детали: `Docs/FirstRelease/Screens/Station.md`.

### Торговая система

В пристыкованном состоянии на станциях игрок может покупать и продавать товары, включая `Energy Cells` и лед, а также пополнять `Fuel` в баках двигателя. Торговля учитывает cargo capacity, конечный склад станции, `Credits` игрока, скрытый баланс `Credits` станции и итоговые цены `basePrice * stationPriceCoefficient`.

Реализована пока только заглушка экрана (`TradeScreen`: открытие кнопкой `TRADE` со `Station`, закрытие, пауза) — сама торговая механика (баланс, товары, покупка/продажа, заправка) не реализована.

Детали: `Docs/FirstRelease/Mechanics/Trading.md` и `Docs/FirstRelease/Screens/Trade.md`.

### Credits

`Credits` используются для торговли и заправки. Игрок начинает с `0 Credits`; каждая станция получает стартовый запас `Credits` от `10 000` до `50 000`, если он не задан явно. Баланс станции игроку не показывается.

Детали: `Docs/FirstRelease/Mechanics/Money.md`.

### Склад станции

Склад станции конечный. Стартовые товары первого релиза: `Energy Cells`, `Fuel`, `Ice`. Все товары станции имеют случайное количество от `20` до `500` единиц включительно. Цены используют фиксированную базовую цену и коэффициент станции `0.5..2.0`; базовые цены: `Energy Cells = 200`, `Fuel = 200`, `Ice = 30`, итоговая цена округляется до ближайшего целого `Credits`.

Детали: `Docs/FirstRelease/Mechanics/StationInventory.md`.

### Грузовой отсек

Корабль игрока перевозит cargo-товары в container module. Вместимость первого релиза считается по массе, в соответствии с разделом 46 основного requirements-документа. `Energy Cells` хранятся в грузовом отсеке; `Fuel` грузовой отсек не занимает.

Детали: `Docs/FirstRelease/Mechanics/CargoHold.md`.

### Электричество и Energy Cells

`Energy Cells` питают реактор/генератор, который обеспечивает электричеством active modules. В MVP первого релиза реактор/генератор расходует `1 Energy Cell/hour`; все active modules требуют работающий реактор/генератор.

Детали: `Docs/FirstRelease/Mechanics/ElectricityAndEnergyCells.md`.

### Экипаж, пассажиры и жилой модуль

Экипаж и пассажиры являются людьми на корабле. Для их размещения нужен `Living quarters` с каютами; стартовый `living.quarters.mk1` имеет `cabines = 2`. На старте новой игры на корабле один персонаж - главный герой; пассажира можно взять через станционный контракт при наличии свободной каюты.

Реализована пока только заглушка экрана контрактов (`HireScreen`: открытие кнопкой `HIRE` со `Station`, закрытие, пауза) — сам список контрактов и их принятие не реализованы.

Детали: `Docs/FirstRelease/Mechanics/CrewAndHabitation.md`.

### Экипаж корабля

Игрок может открывать линейные modal-диалоги с членами команды, чтобы получать информацию, атмосферу, подсказки и будущие сюжетные/игровые развилки.

Детали: `Docs/FirstRelease/Mechanics/CrewDialogues.md`.

### Представители станций

В пристыкованном состоянии игрок может говорить с торговым агентом станции. Торговый агент - персонаж, который обслуживает операции покупки и продажи товаров. В первом релизе диалоги линейные.

Детали: `Docs/FirstRelease/Mechanics/StationDialogues.md`.

### Добыча льда

Игрок добывает лед с астероидов через купленный и установленный на станции Drilling Unit командами `mining.extractIce` и `mining.stopExtraction`. Для добычи нужен `scanner.structuralScan`, дистанция `<= 100 km`, синхронизация скорости/направления и свободное место в cargo. Запас льда asteroid конечный: `100..5000` единиц; добыча за цикл `10000 ms`: `100..200` единиц.

Детали: `Docs/FirstRelease/Mechanics/IceMining.md`.

### Заправка корабля

Корабль хранит `Fuel` в kg только в баках двигателя и пополняет его на станциях. В MVP команды двигателя/навигации не расходуют `Fuel`; fuel cost остается за рамками первого релиза.

Детали: `Docs/FirstRelease/Mechanics/Fuel.md`.

## Открытые вопросы

На текущем уровне требований первого релиза открытых вопросов нет. Балансировочные детали, явно вынесенные за рамки первого релиза, перечислены ниже.

## За рамками первого релиза

- Fuel cost для команд двигателя/навигации.
- Детальная цена и условия установки `module.drilling.unit.basic` на станции.
- Сложная экономика пассажирских перевозок.
