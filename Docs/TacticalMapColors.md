# Tactical Map Colors

Дата фиксации: 2026-08-06

Назначение файла: зафиксировать цветовую гамму объектов на игровой tactical map. Этот документ является источником истины для client-side rendering palette, на которую ссылается раздел 39 `deep_space_saga_engine_requirements.md`.

## Общие правила

- Цвета применяются только на стороне Client при отрисовке игровой карты.
- Engine не должен зависеть от `System.Drawing.Color`, `SKColor`, Silk.NET, SkiaSharp или других rendering-specific типов.
- Через `Contracts` допустимо передавать только dependency-free render metadata: тип объекта, состояние знания игрока и отношение к игроку, если оно требуется для выбора цвета.
- Размеры маркеров задаются в `deep_space_saga_engine_requirements.md` и не переопределяются этим документом.
- Если объект не имеет утверждённого цвета или render metadata отсутствует, используется fallback color.

## Палитра

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

## Fallback Color

Fallback color соответствует legacy-коду:

```csharp
Color.FromArgb(30, 45, 65)
```

Эквивалент:

```text
RGB: 30, 45, 65
Hex: #1E2D41
```

## NPC Ship Relation Rules

- `NpcShip neutral` используется для нейтральных NPC-кораблей.
- `NpcShip enemy` используется для враждебных NPC-кораблей.
- `NpcShip friend` используется для дружественных NPC-кораблей.
- Если отношение NPC-корабля к игроку неизвестно, не загружено или пока не передаётся в render snapshot, Client использует `NpcShip neutral`.

## Scan / Knowledge Rules

- До успешного GeneralScan объект может отображаться как `UnknownSpaceObject` и использовать fallback color.
- После успешного GeneralScan маркер должен немедленно перейти на цвет раскрытого render type.
- Player knowledge не должен подменять authoritative domain `objectType`; цвет выбирается по той client-visible проекции, которую разрешено показать игроку.

## Implementation Notes

- В Client можно использовать `SKColor`, но mapping должен быть сосредоточен в одном resolver-е, а не размазан по screen rendering code.
- В Contracts нельзя добавлять graphics-specific типы. Для выбора цвета предпочтительны enum/string metadata, которые сериализуются и остаются независимыми от Client.
- Если будущий дизайн добавит отдельные цвета для `Missile` или `Explosion`, этот документ должен быть обновлён одновременно с требованиями и тестами resolver-а.
