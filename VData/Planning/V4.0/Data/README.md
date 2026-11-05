# Данные V4

Все числа боя хранятся в таблицах Markdown этой страницы, Frames/README.md и Modules/*/README.md. В текущем checkout JSON игры создаёт tools/tactical-battle/Export-Content.ps1 из этих таблиц. ContentSyncTests независимо проверяет значения и порядок JSON. Справочная Python-реализация не требуется (D-USER-14/15 EP-0015).

## Устройство папки

| Путь | Что | Ключ в данных |
|---|---|---|
| `rules.json` | общие правила боя | `rules` |
| `ai.json` | тактики и параметры ИИ | `ai` |
| `pilots.json` | пилоты | `pilots` |
| `layouts.json` | компоновки: рама + модули | `layouts` |
| `scenarios.json` | сценарии боя | `scenarios` |
| `Frames/<id>.json` | рамы (классы корпусов), по файлу на раму | `classes` |
| `Modules/Weapons/<id>.json` | пушки | `weapons` |
| `Modules/Engines/<id>.json` | двигатели | `engines` |
| `Modules/Shields/<id>.json` | щиты | `shields` |
| `Modules/Armor/<id>.json` | бронеплиты | `armor` |

**Правила файлов:**
- Файл рамы и модуля — один объект с полем `id`, равным имени файла без `.json`. Остальные поля описаны в README папки.
- Каталог собирается в порядке имён файлов (сортировка по байтам UTF-8). На исход боя порядок каталога не влияет.
- Все ссылки проверяются при загрузке. Ошибка останавливает запуск с указанием файла и поля (Spec/Generation.md §2).

## Как дрон собирается из рамы и модулей

**Компоновка** (`layouts.<id>`) — это рама и модули на ней:

| Слот | Сколько | Откуда |
|---|---|---|
| рама | 1 | `class` → `Frames/` |
| пушка | ровно 1 (у носителя нет) | `weapon` → `Modules/Weapons/` |
| бронеплиты | ровно 1 (`armor.none` — без брони) | `armor` → `Modules/Armor/` |
| щиты | 0…`shieldSlots` рамы одинаковых | `shield` (null — нет) × `shieldCount` (по умолчанию 1) → `Modules/Shields/` |
| двигатели | 1…`engineSlots` рамы (у носителя 0) | `engines[]` → `Modules/Engines/` |

- Корпус дрона — `hull` компоновки, если он задан, иначе `hull` рамы.
- Масса дрона — масса рамы + масса всех модулей; от неё зависят скорость, разгон и поворот (Spec/Combat.md §3).
- Перед боем игрок может заменить у своих дронов пушку, плиты и щиты, включая их число, в пределах слотов рамы.

`tier` у пушек, плит и щитов — уровень снаряжения для будущего прогресса. На механику он не влияет.

## rules.json — общие правила

Смысл полей — в Spec/Combat.md. Кратко:
- `substeps` — подшагов пути за ход;
- `turnAtMaxSpeedFraction` — доля поворота на максимальной скорости;
- `deployScatter` — разброс расстановки;
- `hit.*` — формула шанса (§6.2);
- `rear.arcHalfDeg` — кормовой сектор (§6.3);
- `disabled.hullFraction` — порог «выведен из строя»;
- `systems.*` — повреждение систем (§6.4);
- `seriesTurnCap` — технический предел хода в автоматических сериях; в игре лимита нет.

<!-- table:rules -->
| поле | значение |
|---|---|
| `version` | 4.0 |
| `description` | Общие правила V4: плоскость без краёв, движение V2, одновременный залп, целые числа урона. |
| `substeps` | 4 |
| `turnAtMaxSpeedFraction` | 0.5 |
| `deployScatter.xy` | 12 |
| `deployScatter.courseDeg` | 6 |
| `hit.base` | 50 |
| `hit.minChance` | 5 |
| `hit.maxChance` | 95 |
| `hit.rangeBonus` | 15 |
| `hit.anglePenalty` | 12 |
| `hit.rearBonus` | 10 |
| `hit.rearBonusArcDeg` | 30 |
| `rear.arcHalfDeg` | 45 |
| `disabled.hullFraction` | 0.3 |
| `systems.minHullLoss` | 40 |
| `systems.engineDamagedFactor` | 0.5 |
| `systems.table` | [effect: weaponJammed, weight: 1, effect: engineDamaged, weight: 1] |
| `seriesTurnCap` | 100 |
<!-- /table -->

## ai.json — ИИ

Смысл полей — в Spec/Generation.md §3. `tactics.<id>` — раскладка ролей «ударник / перехватчик / истребитель» или `adaptive: true` (командир).

<!-- table:ai -->
| поле | значение |
|---|---|
| `tactics.raid.name` | Налёт |
| `tactics.raid.striker` | 3 |
| `tactics.raid.interceptor` | 0 |
| `tactics.raid.fighter` | 1 |
| `tactics.defence.name` | Защита |
| `tactics.defence.striker` | 0 |
| `tactics.defence.interceptor` | 2 |
| `tactics.defence.fighter` | 2 |
| `tactics.dogfight.name` | Схватка |
| `tactics.dogfight.striker` | 0 |
| `tactics.dogfight.interceptor` | 0 |
| `tactics.dogfight.fighter` | 4 |
| `tactics.balanced.name` | Сбалансированная |
| `tactics.balanced.striker` | 1 |
| `tactics.balanced.interceptor` | 1 |
| `tactics.balanced.fighter` | 2 |
| `tactics.commander.name` | Командир |
| `tactics.commander.adaptive` | true |
| `roles.striker.name` | ударник |
| `roles.interceptor.name` | перехватчик |
| `roles.fighter.name` | истребитель |
| `pursuit.tailOffset` | 35 |
| `pursuit.aimErrorScale` | 14 |
| `pursuit.speedFactor` | 1.15 |
| `pursuit.slowForTurn` | true |
| `pursuit.aimAccuracyRef` | 50 |
| `pursuit.slowForTurnSteps` | 4 |
| `striker.attackSpeed` | 50 |
| `striker.breakDistance` | 40 |
| `striker.extendDistance` | 140 |
| `striker.takesTail` | true |
| `interceptor.patrolRadius` | 120 |
| `interceptor.patrolOrbitRadius` | 70 |
| `interceptor.patrolOrbitStepDeg` | 70 |
| `interceptor.patrolSpeed` | 65 |
| `interceptor.screenDistance` | 90 |
| `interceptor.screenHold` | 40 |
| `interceptor.engageFactor` | 1 |
| `interceptor.orbitTurnFraction` | 0.8 |
| `interceptor.flankOffset` | 0 |
| `commander.threatRadius` | 250 |
| `commander.threatTurns` | 5 |
| `commander.threatHeadingDeg` | 60 |
| `commander.maxInterceptors` | 4 |
| `commander.extraInterceptors` | 1 |
| `commander.minRoleTurns` | 3 |
| `commander.strikeWhenEnemyCarrierBelow` | 0.5 |
| `commander.startTactic` | balanced |
| `leash.radius` | 420 |
| `tail.chance` | 0.85 |
| `tail.rangeFactor` | 0.8 |
| `tail.coneFactor` | 0.8 |
| `tail.speedSteps` | 2 |
| `tail.turnSteps` | 6 |
| `fighter.breakAfterTurns` | 3 |
| `fighter.extendTurns` | 1 |
<!-- /table -->

## pilots.json — пилоты

`accuracy` и `maneuver` — целые навыки. Число — у именного пилота. Пара `[min, max]` — у NPC, бросается в каждом бою (Spec/Generation.md §2).

<!-- table:pilots -->
| id | name | accuracy | maneuver |
|---|---|---|---|
| vera | Вера | 55 | 52 |
| yun | Юн | 53 | 55 |
| marta | Марта | 59 | 54 |
| oleg | Олег | 50 | 51 |
| thug1 | Таг-1 | [40, 55] | [40, 55] |
| thug2 | Таг-2 | [40, 55] | [40, 55] |
| thug3 | Таг-3 | [40, 55] | [40, 55] |
| thug4 | Таг-4 | [40, 55] | [40, 55] |
| thug5 | Таг-5 | [40, 55] | [40, 55] |
| thug6 | Таг-6 | [40, 55] | [40, 55] |
<!-- /table -->

## layouts.json — компоновки

`hull` и `shieldCount` необязательны, прочерк — поле не задано.

<!-- table:layouts -->
| id | name | class | hull | weapon | armor | shield | shieldCount | engines |
|---|---|---|---|---|---|---|---|---|
| heavy.sword.shield | Тяжёлый · меч I · средние плиты · щит I ×2 | heavy | — | gun.sword.t1 | plate.medium | shield.t1 | 2 | drive.standard, drive.standard |
| heavy.spear.shield | Тяжёлый · копьё I · средние плиты · щит I ×2 | heavy | — | gun.spear.t1 | plate.medium | shield.t1 | 2 | drive.standard, drive.standard |
| light.sword.shield | Лёгкий · меч I · средние плиты · щит I | light | — | gun.sword.t1 | plate.medium | shield.t1 | — | drive.standard, drive.standard |
| light.spear | Лёгкий · копьё I · средние плиты · щит I | light | — | gun.spear.t1 | plate.medium | shield.t1 | — | drive.standard, drive.standard |
| thug.scrap | Таг · самопал I · латаные плиты | thug | — | gun.scrap.t1 | plate.patched | — | — | drive.light |
| thug.sawn | Таг · обрез I · обшивка из хлама | thug | 40 | gun.sawn.t1 | plate.rags | — | — | drive.light |
| thug.scrap.shield | Таг · самопал I · лёгкие плиты · щит I | thug | 50 | gun.scrap.t1 | plate.light | shield.t1 | — | drive.light |
| carrier.standard | Носитель · без вооружения | carrier | — | — | armor.none | — | — | — |
<!-- /table -->

## scenarios.json — сценарии

Первый сценарий в файле — сценарий по умолчанию. `sides` — ровно две стороны, A и B, по порядку.

У каждой стороны есть тактика, носитель (`id`, `layout`, `x`, `y`, `course`) и дроны (`id`, `pilot`, `layout`, `x`, `y`, `course`, `speed`). Ось y направлена вниз, курс 0 — вдоль +x, по часовой (Spec/Combat.md §1).

<!-- table:scenarios -->
| сценарий | сторона | тактика | что | id | pilot | layout | x | y | course | speed |
|---|---|---|---|---|---|---|---|---|---|---|
| initial | A | dogfight | носитель | Скиталец | — | carrier.standard | 110 | 250 | 0 | — |
| initial | A | dogfight | дрон | Вера | vera | heavy.sword.shield | 145 | 228 | −12 | 60 |
| initial | A | dogfight | дрон | Юн | yun | heavy.spear.shield | 145 | 275 | 20 | 60 |
| initial | A | dogfight | дрон | Марта | marta | light.sword.shield | 138 | 212 | −12 | 60 |
| initial | A | dogfight | дрон | Олег | oleg | light.spear | 135 | 288 | 0 | 60 |
| initial | B | commander | носитель | Логово | — | carrier.standard | 790 | 150 | −180 | — |
| initial | B | commander | дрон | Таг-1 | thug1 | thug.scrap.shield | 755 | 172 | 168 | 55 |
| initial | B | commander | дрон | Таг-2 | thug2 | thug.scrap | 755 | 125 | −160 | 55 |
| initial | B | commander | дрон | Таг-3 | thug3 | thug.sawn | 762 | 188 | 168 | 55 |
| initial | B | commander | дрон | Таг-4 | thug4 | thug.sawn | 765 | 112 | −180 | 55 |
| initial | B | commander | дрон | Таг-5 | thug5 | thug.scrap | 770 | 205 | 175 | 55 |
| initial | B | commander | дрон | Таг-6 | thug6 | thug.sawn | 772 | 95 | −175 | 55 |
<!-- /table -->

Цель баланса (`goal`) — доли боёв стороны игрока, которые проверяет замер (Spec/Generation.md §4):

<!-- table:goals -->
| сценарий | name | playerSide | winMaxOneLoss | winNoLoss | tolerance | seeds |
|---|---|---|---|---|---|---|
| initial | Начальный бой | A | 0.8 | 0.6 | 0.03 | [1, 1000] |
<!-- /table -->
