# Тактическая карта и маневрирование

> **Контракт EP-0013.** Актуальное управление EP-0013: A/← и D/→ непрерывно поворачивают; W/↑ и S/↓ разгоняют/тормозят в бою и вне боя. Оси независимы, синонимы и противоположные клавиши обрабатываются стеком, Shift защёлкивает разгон. E — прицел ПРО, Z/C — уклонение. [Полный текущий контракт](../../04-Engineering/ManualManeuver.md) заменяет прежнюю keyboard step-схему. [F12-панель](../../04-Engineering/DebugTuningPanel.md) перехватывает мышь, сохраняя боевые клавиши.

## US20 current behavior — 2026-10-21

Selected navigation reaches its point and continues to the edge; ship image/fields are on the right, module details on the left. NPC torpedo map visuals contain only current marker, short rear tail and dark admitted past trail. Torpedo send exits aiming; own reload completion emits one journal/report/readiness cue. Combat offers pause/1×/2×/5×/10× with real multipliers and Save18 continuation. Earlier conflicting summaries below are superseded for these six changes. See [current contract](../../04-Engineering/PointAimCombat.md) and its execution link; native/audio/human limits remain explicit.

## Current EP-0012 contract — 2026-10-21

Current combat uses point aim, manual CM and engine evasion. Own traces/R/core/deadline are public; foreign markers/rear tails/admitted past trails never reveal fuse point/time. NPC trajectory uses display-admitted motion only: two equal physical one-second turns confirm an arc, otherwise straight at current speed/course. This admitted model is now also used by point launch/forecast consumers. Persistent range rings70/30km and session module toggles replace hover-only combat ranges. Q/W aim,1–3/wheel profile, click launch, Esc/right-click cancel; A/D hold preview/release evade, Tab selects threat, arrows hold turn, Shift full speed, F fits combat. Pause freezes physical forecasts/reload. The dated EP8/9 combat subsections below are historical; other navigation/economy rules remain applicable. [Point-aim combat contract](../../04-Engineering/PointAimCombat.md).

## Наблюдаемый прогноз NPC — 2026-10-18

Будущая линия NPC показывает наблюдаемую тенденцию за две физические секунды, без чтения скрытого манёвра. До подтверждения либо при несовпадении — прямая; подтверждённый поворот — дуга при текущей постоянной скорости. Пауза не добавляет наблюдений. [Точный контракт](../../04-Engineering/NpcObservedTrajectories.md). Текущее движение и прогнозы оружия остаются прежними; native acceptance OPEN.

## Актуализация EP-0009 — 2026-10-12

Карта показывает модульное состояние известных кораблей; атаку/защиту и перехват считает Engine по текущим навыкам и режимам. Боевые сценарии задают NPC radius50±10км и flank−35/0/+35. Каталожные дальности и скорости заменяют исторические числа EP-0008 ниже. [Текущие правила](../../04-Engineering/ModuleCombat.md) · [нативные ограничения](../../../Board/EP-0009-battle/evidence/native.md).

## Актуализация EP-0008 — 2026-10-12

Бой теперь входит в реализованный scope. Круг радиуса появляется по hover оружейного модуля; старое правило selected-пирата заменено. Auto100км/manual200км и торпедный200км берутся из каталога. Manual preview использует общую Motion-геометрию, показывает0% или неизвестный ETA явно. NPC pursuit/orbit подтверждается Engine на сетке100мс; UI не управляет исходом. ПР показывает оставшееся физическое время, обычные торпедные trails остаются скрыты.

[Текущий контракт](../../01-Requirements/EngineRequirements.md#phase-three-combat) · [Проверки и ограничения](../../04-Engineering/PhaseThreeCombatAcceptance.md).


## Сверка реализации — 2026-10-10

Обновление после исправлений: F01–F05 закрыты кодом и регрессиями; EP-0004 включён в текущий рабочий набор. Баланс, 80 FPS и человеческая приёмка остаются открытыми. Текущий статус — `Documentation/04-Engineering/EpicReview20261010/Fixes.md`.

EP-0002–0003 реализованы в base-fight: seeded система и общая орбитальная математика, кластеры/ресурсы/торговые направления, текущие ETA и materialized Save/Load. EP-0004 интегрирован в текущий незакоммиченный рабочий набор; native80FPS FAILED. F02–F04 исправлены и проверены новыми регрессиями. Approach не является телепортацией или командой стыковки. Числа ETA относятся к прямой на Vmax, без гарантии фактической длительности манёвров.

[Текущее устройство](../../04-Engineering/TradingAndSolarSystem.md) · [Ревью, дефекты и остаток](../../04-Engineering/EpicReview20261010/README.md). Датированные записи ниже сохраняют историческое значение; они не являются новым подтверждением готовности.

Экономическая сеть станции является одним из источников наполнения карты. Правила назначения ролей станций, расстояний, торговых циклов, астероидных полей и рисков описаны в [TradingSystemMvpStories.md](TradingSystemMvpStories.md). Карта должна генерироваться детерминированно из `masterSeed`, но не обязана показывать игроку точные удалённые цены до сканирования или посещения.

Статус: частично реализовано.

Основные источники: `Documentation/01-Requirements/EngineRequirements.md`, `Documentation/03-Design/TacticalMapSpecification.md`, `GameSessionScreen`, `CommandPanels.md`.

## Цель

Игрок должен понимать положение корабля и объектов в системе, масштабировать карту и управлять движением корабля через командные панели Tetrarch Class.

## Уже существующая основа

- Координаты мира: `1 unit = 100 m`, `double`.
- Направление: `0°` вверх, `90°` вправо, по часовой стрелке.
- Клиент получает `AuthoritativeSnapshot` и рисует карту из `SnapshotBuffer`.
- Client-side prediction использует `DeepSpaceSaga.Motion`.
- Доступны команды двигателя: ускорение, торможение, повороты, удержание скорости/курса, синхронизация скорости/направления и orbit.
- Масштаб карты меняется колесом мыши и кнопками scale panel.

## Требования первого релиза

- Карта должна оставаться главным игровым экраном.
- Игрок должен уметь выбрать станцию, астероид, корабль или планету.
- Игрок должен уметь маневрировать к станции, астероиду или выбранному celestial object через командные панели.
- Игрок должен видеть текущую скорость симуляции и менять ее.
- Игрок должен видеть хотя бы минимальное состояние своего корабля: скорость, курс, груз, запас `Fuel`.
- Маневрирование должно проходить через `IGameSessionConnection` и `PlayerCommand`, без прямого обращения Client к Engine.
- `GameSessionScreen` должен группировать команды в панели Navigation, Maneuver, Engine и Space Control.

## Команды первого релиза

- Navigation: `navigation.dock`, `engine.orbit`, `engine.speedSynchronization`, `engine.directionSynchronization`, `navigation.approach`.
- Maneuver: `engine.maintainCourse`, `engine.turnLeftStep`, `engine.turnRightStep`, `engine.turnLeftUntilCancel`, `engine.turnRightUntilCancel`.
- Engine: `engine.accelerate`, `engine.brake`, `engine.maintainSpeed`.
- Space Control: `scanner.generalScan`, `scanner.structuralScan`, `scanner.nearbySignatures`, `navigation.stationsList`, `mining.extractIce`, `mining.stopExtraction`.

## Сближение Approach

Уточнение от 2026-09-20: [EngineRequirements, раздел 60](../../01-Requirements/EngineRequirements.md#approach-shortest-route) определяет кратчайший допустимый маршрут с минимальным временем прибытия при постоянной скорости. При достижимой встрече конечная точка — будущая задняя точка цели; при отсутствии перехвата — её задняя точка, зафиксированная в момент планирования. Конечный курс совпадает с курсом цели, ограничения поворота сохраняются. Равная или большая скорость цели сама по себе не исключает встречу.

Карта показывает подтверждённый `ApproachRoute`, одинаковый с исполнением в Engine. Запасной маршрут не обозначается гарантированным перехватом. Продолжение по конечному курсу к краю viewport не входит в конечный манёвр; режим «Весь маршрут» кадрирует манёвр. Красная пользовательская линия — пояснение желаемого направления полёта, не требование нового цвета UI. Детали: [ApproachRoutes](../../04-Engineering/ApproachRoutes.md).

## Границы MVP

- Полная орбитальная физика не требуется сверх уже описанной упрощенной модели.
- Бой, столкновения и hull damage добавлены этапами EP-0006–0008; остальные ограничения раннего MVP сохраняются.


## EP-0007 — актуальное дополнение от 2026-10-04

ПР — самостоятельный selectable объект с полным подтверждённым следом и прогнозом встречи; Fire по ПР запрещён. Круг защиты100км масштабируется камерой, маркер5px сохраняет экранный размер. Шанс и операторские рейтинги приходят из Engine. [Технический контракт и приёмка](../../04-Engineering/CountermeasureCombat.md).

## Актуализация EP-0004 — 2026-10-08

SolarSystem/cluster fit и четыре слоя показывают открытую стратегическую карту; движения объектов вычисляются общей Motion-математикой из authoritative snapshot. Территории и поля информационные, не являются новыми манёврами или collision rules. POI/field selection client-local; overlap cycling сохраняет камеру, Ctrl+free click использует engine.orbit.

[Текущий технический контракт и evidence](../../04-Engineering/AiMapEnvironment.md).
