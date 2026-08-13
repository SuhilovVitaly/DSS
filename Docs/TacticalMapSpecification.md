# Tactical Map Specification

Дата фиксации: 2026-08-13

Назначение файла: единая спецификация client-side tactical map для `GameSessionScreen`: палитра маркеров, правила видимости/знания игрока и пользовательские взаимодействия мышью и клавиатурой.

Этот документ является источником истины для client-side tactical map rendering palette и interaction rules. Engine не должен зависеть от `System.Drawing.Color`, `SKColor`, Silk.NET, SkiaSharp или других graphics/input-specific типов. Через `Contracts` допустимо передавать только dependency-free данные: стабильные object ids, serializable render metadata и session-control состояние.

## Цвета

### Общие правила

- Цвета применяются только на стороне Client при отрисовке игровой карты.
- Mapping цвета должен быть сосредоточен в одном resolver-е, а не размазан по screen rendering code.
- Размеры маркеров задаются в `deep_space_saga_engine_requirements.md` и не переопределяются этим документом.
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

## Мышь

### Общие правила

- Координаты карты обрабатываются в raw screen pixels viewport.
- UI панели hit-testятся в logical UI coordinates с учётом `uiScale`.
- Клик по UI панели не считается кликом по карте.
- Если несколько объектов попадают в hit-test, выбирается ближайший к курсору; при равной дистанции выбирается меньший `ObjectId` по `StringComparison.Ordinal`.
- Для `ActiveObjectId` и `SelectedObjectId` используется фиксированный радиус `30 px` от центра видимого маркера объекта, без зависимости от zoom, marker size и `uiScale`.

| Действие | Условие | Результат |
| --- | --- | --- |
| Движение мыши над картой | Курсор находится в радиусе `30 px` от видимого объекта | `ActiveObjectId` получает `ObjectId` ближайшего объекта. Полная пара `(ActiveObjectId, SelectedObjectId)` отправляется в Engine через session-control. |
| Движение мыши над картой | Курсор покинул радиус `30 px` от всех видимых объектов | `ActiveObjectId` сбрасывается в `null`; изменение отправляется в Engine. |
| Левая кнопка по объекту | Клик в радиусе `30 px` от видимого объекта | `SelectedObjectId` получает `ObjectId` ближайшего объекта; клик поглощается, камера не двигается, navigation command не отправляется. |
| `Ctrl` + левая кнопка по объекту | Клик в радиусе `30 px` от видимого объекта | Работает как выбор объекта: обновляет `SelectedObjectId`; navigation command не отправляется. |
| Левая кнопка по свободной карте | Клик не попал в UI и не попал в объект | Камера переносит focus в world point под курсором, follow player отключается. |
| `Ctrl` + левая кнопка по свободной карте | Клик не попал в UI и не попал в объект | Отправляется `engine.navigate-to-point` с world coordinates клика; камера не двигается. `Ctrl` действует только на текущий клик. |
| Правая кнопка по карте | Любой клик по карте, включая объект или пустое место | `SelectedObjectId` сбрасывается в `null`. `ActiveObjectId`, камера и navigation не меняются. |
| Правая кнопка по UI панели | Клик попал в speed/scale/command/info/player panel | Клик не считается map click и не сбрасывает `SelectedObjectId`. |
| Колесо мыши вверх | Tactical map active | Zoom in вокруг курсора, до максимума `2.0 px/unit`. |
| Колесо мыши вниз | Tactical map active | Zoom out вокруг курсора, до минимума `0.001 px/unit`. |
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
| Engine button Maintain Speed | Отправляет `engine.maintain-speed`, если команда доступна. |
| Engine button Turn Right Step | Отправляет `engine.turn-right-step`, если команда доступна. |
| Engine button Turn Left Step | Отправляет `engine.turn-left-step`, если команда доступна. |
| Engine button Turn Right Until Cancel | Отправляет `engine.turn-right-until-cancel`, если команда доступна. |
| Engine button Turn Left Until Cancel | Отправляет `engine.turn-left-until-cancel`, если команда доступна. |
| Engine button Maintain Course | Отправляет `engine.maintain-course`, если команда доступна. |
| Engine button Cancel All | Legacy/current UI entry: отправляет `engine.cancel-all`, если эта кнопка присутствует и команда доступна. Не считать новой канонической hotkey-командой без отдельного требования. |

## Клавиатура

### Общие правила

- Клавиатура обрабатывается edge-based: действие происходит на press edge, удержание не повторяет команду каждый frame.
- `ControlLeft` и `ControlRight` используются как модификатор для текущего клика и сбрасываются на key up или при деактивации экрана.
- Быстрые клавиши работают внутри `GameSessionScreen`, если поверх него не активен modal screen.

| Клавиша / сочетание | Результат |
| --- | --- |
| `Ctrl` удерживается | Включает modifier для текущего mouse click. Само по себе действие не запускает. |
| `Ctrl` + левая кнопка мыши по свободной карте | См. блок мыши: `engine.navigate-to-point`. |
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
| `Left` | Отправляет `engine.turn-left-step`, если команда доступна. |
| `Right` | Отправляет `engine.turn-right-step`, если команда доступна. |

### Команды без текущей keyboard hotkey

- `engine.maintain-speed` доступна через UI-кнопку Engine panel.
- `engine.maintain-course` доступна через UI-кнопку Engine panel.
- `engine.turn-left-until-cancel` доступна через UI-кнопку Engine panel.
- `engine.turn-right-until-cancel` доступна через UI-кнопку Engine panel.
- `engine.match-target-speed` и `engine.match-target-course` требуют явный `targetObjectId`; `SelectedObjectId` не является implicit authoritative target.
- `engine.cancel-all` является legacy/current UI entry, если кнопка присутствует; не имеет отдельной keyboard hotkey.
