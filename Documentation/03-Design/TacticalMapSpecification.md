# Tactical Map Specification

## Сверка реализации — 2026-10-10

Обновление после исправлений: F01–F05 закрыты кодом и регрессиями; EP-0004 включён в текущий рабочий набор. Баланс, 80 FPS и человеческая приёмка остаются открытыми. Текущий статус — `Documentation/04-Engineering/EpicReview20261010/Fixes.md`.

EP-0002–0003 реализованы в base-fight: seeded система и общая орбитальная математика, кластеры/ресурсы/торговые направления, текущие ETA и materialized Save/Load. EP-0004 интегрирован в текущий незакоммиченный рабочий набор; native80FPS FAILED. F02–F04 исправлены и проверены новыми регрессиями. Географическая известность отличается от свежести рыночных наблюдений; текущий UI не должен представлять старое remote observation действующей котировкой.

[Текущее устройство](../04-Engineering/TradingAndSolarSystem.md) · [Ревью, дефекты и остаток](../04-Engineering/EpicReview20261010/README.md). Датированные записи ниже сохраняют историческое значение; они не являются новым подтверждением готовности.

Дата фиксации: 2026-08-13

Назначение файла: единая спецификация client-side tactical map для `GameSessionScreen`: палитра маркеров, правила видимости/знания игрока и пользовательские взаимодействия мышью и клавиатурой.

Этот документ является источником истины для client-side tactical map rendering palette и interaction rules. Engine не должен зависеть от `System.Drawing.Color`, `SKColor`, Silk.NET, SkiaSharp или других graphics/input-specific типов. Через `Contracts` допустимо передавать только dependency-free данные: стабильные object ids, serializable render metadata и session-control состояние.

## Цвета

### Общие правила

- Цвета применяются только на стороне Client при отрисовке игровой карты.
- Mapping цвета должен быть сосредоточен в одном resolver-е, а не размазан по screen rendering code.
- Размеры маркеров задаются в `Documentation/01-Requirements/EngineRequirements.md` и не переопределяются этим документом.
- Если объект не имеет утверждённого цвета или render metadata отсутствует, используется fallback color.

### Палитра

| Объект / состояние | Исходный цвет .NET | RGB | Hex |
| --- | --- | --- | --- |
| PlayerShip / SpaceshipPlayer | `DarkOliveGreen` | `85, 107, 47` | `#556B2F` |
| NpcShip neutral / SpaceshipNpcNeutral | `DarkGray` | `169, 169, 169` | `#A9A9A9` |
| NpcShip enemy / SpaceshipNpcEnemy | `DarkRed` | `139, 0, 0` | `#8B0000` |
| NpcShip friend / SpaceshipNpcFriend | `SeaGreen` | `46, 139, 87` | `#2E8B57` |
| Asteroid | `WhiteSmoke` | `245, 245, 245` | `#F5F5F5` |
| Container | `Gray` | `128, 128, 128` | `#808080` |
| Station | `Orange` | `255, 165, 0` | `#FFA500` |
| Planet | `WhiteSmoke` | `245, 245, 245` | `#F5F5F5` |
| Sun | `Orange` | `255, 165, 0` | `#FFA500` |
| Missile | fallback | `30, 45, 65` | `#1E2D41` |
| Explosion | fallback | `30, 45, 65` | `#1E2D41` |
| UnknownSpaceObject | fallback | `30, 45, 65` | `#1E2D41` |
| Unsupported / missing metadata | fallback | `30, 45, 65` | `#1E2D41` |

### Fallback Color

Fallback color соответствует legacy-коду:

```csharp
Color.FromArgb(30, 45, 65)
```

Эквивалент:

```text
RGB: 30, 45, 65
Hex: #1E2D41
```

### NPC Ship Relation Rules

- `NpcShip neutral` используется для нейтральных NPC-кораблей.
- `NpcShip enemy` используется для враждебных NPC-кораблей.
- `NpcShip friend` используется для дружественных NPC-кораблей.
- Если отношение NPC-корабля к игроку неизвестно, не загружено или пока не передаётся в render snapshot, Client использует `NpcShip neutral`.

### Scan / Knowledge Rules

- До успешного GeneralScan объект может отображаться как `UnknownSpaceObject` и использовать fallback color.
- После успешного GeneralScan маркер должен немедленно перейти на цвет раскрытого render type.
- Player knowledge не должен подменять authoritative domain `objectType`; цвет выбирается по той client-visible проекции, которую разрешено показать игроку.
- Если будущий дизайн добавит отдельные цвета для `Missile` или `Explosion`, этот документ должен быть обновлён одновременно с требованиями и тестами resolver-а.

## Прогноз Approach

Уточнение от 2026-09-20. Геометрический контракт задаётся [EngineRequirements, раздел 60](../01-Requirements/EngineRequirements.md#approach-shortest-route): кратчайший перехват будущей задней точки, а при его отсутствии — кратчайший путь к зафиксированной задней точке цели. Client не выбирает другую точку на кормовой линии и не рассчитывает собственное преследование при наличии `ApproachRoute`.

- Рисуется оставшаяся часть подтверждённого маршрута с точной конечной позой `PredictPose(route, route.DurationMs)`.
- Подтверждённая встреча определяется `IsRendezvous(route)`, не сравнением скоростей; равная или более быстрая встречная цель может быть достижима.
- Запасной путь завершается у зафиксированной при планировании задней точки и не обозначается гарантированным перехватом. Маркер завершения/выравнивания курса не означает достижения фактической движущейся цели.
- Прямое продолжение после манёвра через прогнозное положение цели на момент завершения и до края viewport — отображение дальнейшего курса, не продолжение команды и не обещание догоняния. Для неподвижной цели продолжение заканчивается у неё.
- `maneuverPointCount` отделяет конечный манёвр от продолжения; «Весь маршрут» кадрирует манёвр. Камера и масштаб не меняют физическую геометрию.
- Действующие сплошная золотистая линия Approach, пунктир прогноза цели и маркеры сохраняются. Красная линия на пользовательском скриншоте была нарисована пользователем и не является требованием красной линии в игре.
- Снимки без `ApproachRoute` сохраняют legacy-предпросмотр; render loop не запрашивает Engine.

Детали исполнения и допусков: [ApproachRoutes](../04-Engineering/ApproachRoutes.md).

## Мышь

### Общие правила

- Координаты карты обрабатываются в raw screen pixels viewport.
- UI панели hit-testятся в logical UI coordinates с учётом `uiScale`.
- Клик по UI панели не считается кликом по карте.
- При клике overlap кандидаты упорядочены: станции, player, NPC, другие реальные объекты, POI, поля; затем дистанция и ordinal ID. Повторный клик в пределах3 raw px перебирает тот же список. Hover выбирает реальный объект по тем же группам Station/player/NPC/other, затем distance/ordinal ID; hover не циклический.
- Для реальных маркеров используется фиксированный радиус `30 px` от центра видимого маркера объекта, без зависимости от zoom и `uiScale`; hull/plaque также участвуют в выборе. POI:15px, fields:геометрия.

| Действие | Условие | Результат |
| --- | --- | --- |
| Движение мыши над картой | Курсор находится в радиусе `30 px` от видимого объекта | `ActiveObjectId` получает `ObjectId` реального объекта по priority/distance/ordinal ID. Полная пара `(ActiveObjectId, SelectedObjectId)` отправляется в Engine через session-control. |
| Движение мыши над картой | Курсор покинул радиус `30 px` от всех видимых объектов | `ActiveObjectId` сбрасывается в `null`; изменение отправляется в Engine. |
| Левая кнопка по объекту | Клик в радиусе `30 px` от видимого объекта | Выбирается следующий стабильный кандидат overlap cycle; навигация не отправляется. Одиночный player включает Follow, overlap cycle сохраняет камеру. |
| `Ctrl` + левая кнопка по объекту | Клик в радиусе `30 px` от видимого объекта | Работает как выбор объекта: обновляет `SelectedObjectId`; navigation command не отправляется. |
| Левая кнопка по свободной карте | Клик не попал в UI и не попал в объект | Одиночный клик не перемещает камеру; drag панорамирует и отключает Follow. |
| `Ctrl` + левая кнопка по свободной карте | Клик не попал в UI и не попал в объект | Отправляется `engine.orbit` с world coordinates клика; камера не двигается. `Ctrl` действует только на текущий клик. |
| Правая кнопка по карте | Любой клик по карте, включая объект или пустое место | Real и client-local field/POI selection сбрасываются. `ActiveObjectId`, камера и navigation не меняются. |
| Правая кнопка по UI панели | Клик попал в speed/scale/command/info/player panel | Клик не считается map click и не сбрасывает `SelectedObjectId`. |
| Колесо мыши вверх | Tactical map active | Zoom in вокруг курсора, до максимума `2.0 px/unit`. |
| Колесо мыши вниз | Tactical map active | Zoom out вокруг курсора, до минимального масштаба текущей карты (SolarSystem допускает стратегический System fit ниже legacy0.001). |
| Средняя и другие кнопки мыши | Любое место | Игнорируются. |

### UI-кнопки мышью

| Панель / кнопка | Результат |
| --- | --- |
| Info panel `X` | Закрывает нижнюю левую информационную панель. |
| Speed `II` | Устанавливает `Speed0`. |
| Speed `1x` | Устанавливает `Speed1`. |
| Speed `5x` | Устанавливает `Speed2`. |
| Speed `20x` | Устанавливает `Speed3`. |
| Speed `100x` | Устанавливает `Speed4`. |
| Scale `M0.5` | Устанавливает `2.0 px/unit`. |
| Scale `M1` | Устанавливает `1.0 px/unit`. |
| Scale `M10` | Устанавливает `0.1 px/unit`. |
| Scale `M100` | Устанавливает `0.01 px/unit`. |
| Scale `M1000` | Устанавливает `0.001 px/unit`. |
| Commands panel hide/show | Сворачивает или раскрывает верхнюю левую панель модулей. |
| Commands panel module caption | Сворачивает или раскрывает тело конкретного module row. |
| Engine button Accelerate | Отправляет `engine.accelerate`, если команда доступна. |
| Engine button Brake | Отправляет `engine.brake`, если команда доступна. |
| Engine button Maintain Speed | Отправляет `engine.maintainSpeed`, если команда доступна. |
| Engine button Turn Right Step | Отправляет `engine.turnRightStep`, если команда доступна. |
| Engine button Turn Left Step | Отправляет `engine.turnLeftStep`, если команда доступна. |
| Engine button Turn Right Until Cancel | Отправляет `engine.turnRightUntilCancel`, если команда доступна. |
| Engine button Turn Left Until Cancel | Отправляет `engine.turnLeftUntilCancel`, если команда доступна. |
| Engine button Maintain Course | Отправляет `engine.maintainCourse`, если команда доступна. |
| Engine button Cancel All | Legacy/current UI entry: отправляет `engine.cancelAll`, если эта кнопка присутствует и команда доступна. Не считать новой канонической hotkey-командой без отдельного требования. |

## Клавиатура

### Общие правила

- Клавиатура обрабатывается edge-based: действие происходит на press edge, удержание не повторяет команду каждый frame.
- `ControlLeft` и `ControlRight` используются как модификатор для текущего клика и сбрасываются на key up или при деактивации экрана.
- Быстрые клавиши работают внутри `GameSessionScreen`, если поверх него не активен modal screen.

| Клавиша / сочетание | Результат |
| --- | --- |
| `Ctrl` удерживается | Включает modifier для текущего mouse click. Само по себе действие не запускает. |
| `Ctrl` + левая кнопка мыши по свободной карте | См. блок мыши: `engine.orbit`. |
| `Ctrl+C` | Возвращает camera follow/focus на player ship. |
| `Ctrl+I` | Открывает нижнюю левую information panel. Если панель уже открыта, действие является no-op. |
| `Escape` | Открывает `GameMenu` как modal screen. Modal pause rule останавливает authoritative simulation через `Speed0`, пока modal открыт. |
| `Space` | Toggle pause: при текущем `Speed0` возвращает последнюю non-pause speed; при любой другой speed устанавливает `Speed0`. |
| `F5` | Quick Save в `Saves/quicksave.json` без отдельного modal окна. |
| `F9` | Quick Load из `Saves/quicksave.json` без отдельного modal окна. |
| `1` | Устанавливает `Speed0`. |
| `2` | Устанавливает `Speed1`. |
| `3` | Устанавливает `Speed2`. |
| `4` | Устанавливает `Speed3`. |
| `5` | Устанавливает `Speed4`. |
| `Up` | Отправляет `engine.accelerate`, если команда доступна. |
| `Down` | Отправляет `engine.brake`, если команда доступна. |
| `Left` | Отправляет `engine.turnLeftStep`, если команда доступна. |
| `Right` | Отправляет `engine.turnRightStep`, если команда доступна. |

### Команды без текущей keyboard hotkey

- `engine.maintainSpeed` доступна через UI-кнопку Engine panel.
- `engine.maintainCourse` доступна через UI-кнопку Engine panel.
- `engine.turnLeftUntilCancel` доступна через UI-кнопку Engine panel.
- `engine.turnRightUntilCancel` доступна через UI-кнопку Engine panel.
- `engine.match-target-speed` и `engine.match-target-course` требуют явный `targetObjectId`; `SelectedObjectId` не является implicit authoritative target.
- `engine.cancelAll` является legacy/current UI entry, если кнопка присутствует; не имеет отдельной keyboard hotkey.


## EP-0007 — актуальное дополнение от 2026-10-04

ПР: голубой core5 canvas px с glow, solid подтверждённый trail, dashed forecast и marker встречи. Шанс показан у ПР и marker; tooltip/info содержит frozen base/skill/effective ratings обоих операторов. Под пиратом показан статус защиты, при выборе круг100км зависит от zoom. Перехват/Промах и SelfDestruct ring/terminal trail исчезают за2 реальные секунды независимо от паузы. Ordinary torpedo trails скрыты. Палитра загружается из Data/UI/combat-visuals.json. [Технический контракт и приёмка](../04-Engineering/CountermeasureCombat.md).

## Актуализация EP-0004 — 2026-10-08

При AiMap toolbar включает независимые слои Orbits/Territories/Fields/POI, по умолчанию All. Выключенные descriptors не выбираются. SelectedFieldId/SelectedPoiId локальны Client и имеют приоритет в панели; target commands недоступны. POI/Sun/Planet markers не перехватываются cluster aggregate. Компактная Object Info резервирует место toolbar; обе строки прокручиваются колесом без map zoom. Diagnostic X рисуется последним и hit-testится первым среди левых UI clicks. Native проверены UI1/1.2/1.5 и1280/1920; FPS criterion OPEN.

[Текущий технический контракт и evidence](../04-Engineering/AiMapEnvironment.md).
