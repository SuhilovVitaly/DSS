# Командные панели корабля

Статус: смысловая группировка команд первого релиза обновлена под Tetrarch Class.

Связанные документы: `TetrarchClass.md`, `TacticalMapAndManeuvering.md`, `Docking.md`, `Fuel.md`, `ElectricityAndEnergyCells.md`, `IceMining.md`.

## Цель

Командные панели на `GameSessionScreen` группируют module-addressed команды корабля по смыслу, а не строго по физическому модулю. Команда может быть технически командой Engine, но отображаться в панели Navigation, если игрок воспринимает ее как навигационное действие.

## Navigation

| Команда | Технический модуль | Условие |
| --- | --- | --- |
| `navigation.dock` | Navigation Computer | Выбрана станция; дистанция `< 200 km`; скорость и направление синхронизированы со станцией |
| `engine.orbit` | Engine | Выбран celestial object |
| `engine.speedSynchronization` | Engine | Выбран celestial object |
| `engine.directionSynchronization` | Engine | Выбран celestial object |
| `navigation.approach` | Engine | Выбран celestial object; корабль движется и может поворачивать. Если встреча достижима, строится кратчайший маршрут к будущей задней точке цели; иначе — кратчайший маршрут к её задней точке, зафиксированной при планировании. Конечный курс совпадает с курсом цели, скорость корабля не меняется. |

### Approach: уточнение от 2026-09-20

Действуют [требования раздела 60](../../01-Requirements/EngineRequirements.md#approach-shortest-route) и [описание маршрута](../../04-Engineering/ApproachRoutes.md). При постоянной скорости кратчайший путь даёт минимальное время прибытия внутри выбранного режима и шести допустимых семейств Дубинса. Повороты учитывают ограничение угловой скорости.

Перехват проверяется и для равной/большей скорости цели: сравнения скоростей недостаточно для вывода о достижимости. Только при отсутствии допустимой встречи используется фиксированная задняя точка; полёт к ней не гарантирует сближения с фактической движущейся целью. Она не сдвигается вслед за равномерным движением цели, повторные заходы после завершения не запускаются. Простое нахождение далеко позади на нужном курсе команду не завершает.

Ранее описанный обязательный обход цели без экстраполяции и более поздняя минимизация конечного отставания со штрафом за длину заменены этим правилом. Торможение, синхронизация скорости и стыковка остаются отдельными действиями игрока. Продолжение линии на карте после конца манёвра показывает дальнейший полёт по курсу, а не дополнительное преследование или обещание встречи.

## Maneuver

| Команда | Технический модуль | Параметр |
| --- | --- | --- |
| `engine.maintainCourse` | Engine | Курс `0..360` |
| `engine.turnLeftStep` | Engine | - |
| `engine.turnRightStep` | Engine | - |
| `engine.turnLeftUntilCancel` | Engine | Останавливается отменой/следующей командой |
| `engine.turnRightUntilCancel` | Engine | Останавливается отменой/следующей командой |

## Engine

| Команда | Технический модуль | Параметр |
| --- | --- | --- |
| `engine.accelerate` | Engine | - |
| `engine.brake` | Engine | - |
| `engine.maintainSpeed` | Engine | Скорость `0..max` |

## Space Control

| Команда | Технический модуль | Условие |
| --- | --- | --- |
| `scanner.generalScan` | Scanner | Выбран celestial object |
| `scanner.structuralScan` | Scanner | Выбран celestial object |
| `scanner.nearbySignatures` | Scanner | Без цели |
| `navigation.stationsList` | Navigation Computer | Без цели |
| `mining.extractIce` | Drilling Unit | Выбран asteroid; `scanner.structuralScan` подтвердил лед; дистанция `<= 100 km`; скорость и направление синхронизированы; есть место в cargo |
| `mining.stopExtraction` | Drilling Unit | Активна добыча льда |

## Требования первого релиза

- `GameSessionScreen` должен показывать команды через шесть панелей: Navigation, Maneuver, Engine, Space Control, Torpedo Launcher, Countermeasure Launcher.
- Панели должны работать поверх существующей module-addressed command model.
- Команды Navigation Computer: `navigation.dock`, `navigation.stationsList`.
- Команды Engine: `engine.accelerate`, `engine.brake`, `engine.maintainCourse`, `engine.maintainSpeed`, `engine.turnLeftStep`, `engine.turnRightStep`, `engine.turnLeftUntilCancel`, `engine.turnRightUntilCancel`, `engine.speedSynchronization`, `engine.directionSynchronization`, `engine.orbit`, `navigation.approach` (физически команда Engine, отображается в панели Navigation).
- Команды Scanner: `scanner.generalScan`, `scanner.structuralScan`, `scanner.nearbySignatures`.
- Команды Drilling Unit: `mining.extractIce`, `mining.stopExtraction`.
- UI должен показывать недоступность команды через понятную причину: нет цели, цель неверного типа, не выполнена синхронизация, нет топлива, нет `Energy Cells`, нет mining module, нет места в cargo, корабль не в нужном состоянии.


## Weapon panels — EP-0007

Torpedo Launcher показывает назначенного оператора, навык и рейтинг, состояние Ready/Guiding/NoOperator. `torpedo.fire` захватывает выбранную разрешённую цель; `torpedo.selfDestruct` захватывает ID своей активной торпеды и работает на паузе. Countermeasure Launcher показывает отдельного оператора, auto on/off, Ready/Guiding/Reloading/NoOperator и физический countdown. Кнопки `defense.enable`/`defense.disable` меняют только будущие пуски; ручной пуск ПР отсутствует. Шесть заголовков доступны при адаптивном сворачивании на малой высоте экрана. [Контракт и доказательства](../../04-Engineering/CountermeasureCombat.md).

## EP-0005 — актуализация 2026-10-09

Layout command/info panels известен до подготовки карты и участвует в её obstacles с учётом uiScale. Внутри кадра панели используют captured snapshot/speed; действия между кадрами проверяются по текущему authoritative snapshot. Высокие масштабы не имеют завершённой human manual acceptance.

[Контракт карты](../../03-Design/TacticalMapSpecification.md); [фактическая приёмка](../../../Board/EP-0005-optimization/PerformanceEvidence.md). Client 1827/1827; native scripted actions 80/80. Performance 80 FPS FAILED/OPEN; human manual smoke NOT RUN.
