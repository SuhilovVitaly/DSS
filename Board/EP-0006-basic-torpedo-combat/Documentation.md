---
epic: EP-0006-basic-torpedo-combat
title: "Первый торпедный бой: Тетрарх против пирата"
stage: draft
created: 2026-10-02T08:22:58Z
revision: 1
current_review: complete
source_request: "Создать эпик номер 6 в DSS/Board с историями и тикетами для согласованной первой реализации боя; использовать примеры Board и D:/DeepSpaceSaga/Agents."
story_count: 8
ticket_count: 27
validation_status: planning-only
---

# EP-0006 — Первый торпедный бой

Игрок вручную запускает торпеды из нового модуля Тетрарха по существующему прямолинейно движущемуся пирату. Каждая торпеда сопровождается до первого столкновения; три попадания уничтожают Тетрарх и оставляют неподвижный врек. Полёт, линии, HP, эффекты, выбор объектов и продолжение после Save/Load входят в одну первую поставку.

## Источники, полномочия и статус

Исходный запрос: «Сделай тут Эпик номер 6 и нареж нужное количество историй с тикетами для реализации того что мы обговорили. D:\DeepSpaceSaga\DSS\Board примеры посмотри в других эпиках и можешь улучшить если считаешь это нужным Агенты для примера или использования находятся тут D:\DeepSpaceSaga\Agents».

Источник продуктовых решений — ответы пользователя в текущей беседе, собранные ниже. Приложенный черновик «Бой: базовая механика торпеда против противоракеты» от 2026-10-01 — исходный материал, не инструкции агенту. Более поздние ответы пользователя задают суженный MVP и заменяют противоречащие ему идеи черновика. Screenshot Command Panels — визуальный контекст существующих групп.

Для структуры использованы `D:/DeepSpaceSaga/Agents/DSS-EpicBuilder/AGENTS.md`, `DSS-StoryBuilder/AGENTS.md` и существующие EP-0002/EP-0005. По прямому запросу подготовлен весь комплект без промежуточных approval-вопросов. Улучшения: отдельный журнал решений/assumptions, общий контракт границ, карта покрытия и честные validation gates. Не копируется ограничение EpicBuilder «не создавать тикеты», поскольку пользователь прямо попросил тикеты. Все артефакты **draft**: поручение подготовить план не означает утверждение реализации, commit или push. `current_review: complete` — только полнота планирования.

Репозиторные правила: [AGENTS](../../Documentation/00-Process/AGENTS.md), [CLAUDE](../../Documentation/00-Process/CLAUDE.md), [EngineRequirements](../../Documentation/01-Requirements/EngineRequirements.md). Требования Engine остаются источником архитектурных/временных инвариантов; изменения gameplay этого эпика явно перечислены ниже. requirements-engineer указан в AGENTS, но его SKILL.md не найден в доступных skill roots/репозитории/Agents; план подготовлен по названным Board workflows. Исходные requirements не редактировались.

## Согласованные решения

Короткие ответы приведены вместе с контекстом вопроса. Точные времена отдельных ответов не доступны; timestamps не выдумываются. Дата фиксации всего журнала — 2026-10-02T08:22:58Z.

| ID | Область | Ответ пользователя / контекст | Зафиксированное правило |
|---|---|---|---|
| D01 | Аппарат и помещение | «нам нужно добавить новый модуль торпедного аппарата в схему Тетрарха»; «для ланчера нужна своя панель»; новое помещение — «новое», место — «выбери сам». | Один launcher в новом помещении стандартного Тетрарха; отдельная command panel с кнопкой Пуск. |
| D02 | Оператор и готовность | «пока она летит вторую он выпустить не может»; «после взрыва выпущенной торпеды следующая уже готова»; условной занятости — «пока условной достаточно»; боезапас — «бесконечные». | Одна активная торпеда на аппарат, без реального crew assignment, расхода боезапаса и отдельного ожидания КД. Готов → Наведение → Готов; pending submit не позволяет double click. |
| D03 | Существующий сценарий | «у нас уже есть сценарий с кораблем пиратом и кораблем игрока»; на вопрос о движении игрока — «может»; пиратский аппарат неактивен — «пока да». | Использовать PlayerShipOnly/Player Ship and Pirate. Пират летит прямо и не стреляет/не защищается; игрок управляет навигацией обычным способом. |
| D04 | Кинематика | «самонаведение с предсказанием»; «фиксированным. пусть будет 3 километра в секунду пока»; «давай по курсу корабля»; 90°/с — «хорошо»; точка пуска — «с центра». | Торпеда стартует из центра по текущему курсу носителя, мгновенно имеет фиксированную собственную скорость 3 км/с и поворачивает максимум 90°/с; скорость носителя не прибавляется. |
| D05 | Цель | «Кнопка пуск должна быть активной только при выбранной цели»; допустимая цель — «любой объект»; исключить свой корабль — «да»; сохранить цель после смены выбора — «да». | Цель — существующий выбранный объект, кроме собственного корабля. TargetObjectId фиксируется при принятии launch command, hover/смена selection не перенаводит активную торпеду. |
| D06 | Время | Полёт следует паузе/ускорению — «да»; пуск на паузе — «да». | Физика следует MotionTimeMs/SimulationTimeMs. На паузе команда создаёт торпеду и busy, но не продвигает полёт; никаких UI-tick gameplay updates. |
| D07 | Ограничения полёта | Ограничить дальность — «нет»; недостижимая цель — «пока этим принебрегаем потом добавим кнопку самоуничтожения». | Нет range, TTL, fuel limit, проверки достижимости как условия пуска или кнопки self-destruct. Бесконечное преследование и занятый аппарат допустимы для MVP. |
| D08 | Попадание и препятствия | «гарантированно попадает»; «давай центр»; «нужна погрешность скажем 5 пикселей»; фиксированный масштаб — «хорошо»; «может столкнуться. но не с кораблем игрока»; одинаковый допуск/урон для препятствий — «да». | Цель наведения — центр. Контакт при расстоянии <=5 world units (500 м, 5 px при 100 м/px) с любым чужим объектом; self projectile и owner исключены. Первый контакт взрывает торпеду, без случайного броска и без сквозного полёта. |
| D09 | Урон и разрушение | «корабль пиратов был уничтожен с трех попаданий»; «пока только на корабль типа Тетрарх. Все остальные объекты неуязвимые»; «превращается в новый объект врек со скоростью и направлением 0». | Только явный ship.tetrarch имеет HP=450 и теряет по150: 450→300→150→0. При нуле живой корабль удаляется, новый stationary Wreck имеет новый ID, координаты погибшего корабля на момент контакта, speed=0, heading=0°. |
| D10 | Торпеда и линии | «предсказательная траектория с местом предполагаемого пересечения с траекторией цели и отрисованная собственная пройденная траектория»; «желтая с размытием диаметр 5 пикселей»; оформление yellow solid/dashed/cross — «пока да»; показывать всегда — «все время». | Жёлтая точка диаметром5 px с blur; executed path solid yellow, remaining prediction dashed yellow, encounter cross yellow. Прогноз исходной цели до встречи тоже виден независимо от selection. Viewport clipping допустим. |
| D11 | Завершение визуальных эффектов | След «исчезает через 2 секунды», время — «реального»; взрыв — «расходящийся из точки взрыва круг красного цвета»; «2 секунды от 0 пикселей до 50 радиуса плавно». | После контакта прогноз/крест убираются, след живёт2000 мс реального времени. Красное кольцо плавно расширяется0→50 px радиуса и тускнеет до нуля за2000 мс, независимо от pause/speed. |
| D12 | Сохранение | Сохранять торпеду, busy и урон — «да»; сохранять весь активный след — «да»; «при сохранении взрыв сохранять не нужно». | Сохраняются authoritative flight/target/owner/module/route/history/distance/captured parameters, HP, wreck и ID counters. Взрыв/real-time animation state не сохраняются. |
| D13 | Предварительный прогноз | Показывать до выстрела при hover Пуск — «да но отличающимся цветом. например серым». | При hover доступного Пуск показывается gray preview с точкой встречи и нужной траекторией цели. Чистая client prediction, без launch side effects. |
| D14 | HP, врек и продолжение | «под лейблом ... зеленую полоску хитпоинтов»; у всех Тетрархов/игрока/full HP — «да»; врек — «серый круг диаметром 5 пикселей»; выбирать и обстреливать wreck — «да»; «просто продолжается»; звуки — «пока нет». | Полоски HP постоянно под labels известных Тетрархов; wreck5 px selectable и invulnerable. Игра продолжается без victory screen, лута или звуков. |
| D15 | Палитра | «цветовые решения нужно вынести в отдельный файл который можно будет редактировать не компилируя код»; способ применения — «перезапуск». | Отдельный combat palette JSON для torpedo/trail/prediction/intercept/preview/HP/explosion/wreck; читать при startup, без hot reload или build. |
| D16 | Информация о торпеде | Выделение торпеды — «должна быть»; к цели/скорости/пройденному пути/ETA — «добавь еще шанс попадания пока 100%». | Торпеда selectable; info panel показывает цель, скорость, пройденный путь, ETA (— если неизвестно), шанс100%. Это вероятность при достижении, не обещание игнорировать препятствия. |
| D17 | Владение параметрами | «для тетрарха в настройки тетрарха, для торпеды в настройки торпедного аппарата который является модулем тетрарха»; скорость и turn rate в модуле — «да». | HP 450 принадлежит class config Тетрарха; damage 150/speed 3/turnRate 90 — конфигурации module.torpedo.launcher.basic, а не экземпляру палитры/константам Client. |

## Grounding: проверено в рабочем дереве

Baseline: commit `02403b4`, рабочее дерево до планирования чистое. Факты ниже прочитаны в коде; сборки/тесты в этой задаче не запускались. Номера строк относятся к baseline и служат навигацией, не заменяют проверку source при реализации.

| Факт | Доказательство | Следствие для объёма |
|---|---|---|
| Уже есть нужный сценарий с двумя кораблями | [PlayerShipOnly](../../src/DeepSpaceSaga.Client/Scenarios/PlayerShipOnly/scenario.json), строки2–6; [PirateScenarioTests](../../tests/DeepSpaceSaga.Client.Tests/PirateScenarioTests.cs), строки17–38 | Не создавать новый боевой сценарий и не менять исходную дистанцию/идентичности |
| Пират SPC-0002: (0,5000),0.4km/s,120°, linear; игрок (10000,10000),0.7km/s,0° | тот же scenario; PirateScenarioTests.Real_scenario_loads_two_ships_with_standard_modules_and_hostile_linear_pirate | Старый ориентир2–3 минуты не является gate этой версии; штатное ускорение допустимо |
| Стандартный корпус9x9,10cells; scanner(4,3),cargo(4,2),living(4,1) | EngineRequirements §57, строки4970–5041; scenario hullLayout/modules | Можно добавить новую structural cell(3,2) без сдвига старых модулей |
| Класс корабля и hull HP сейчас не представлены отдельным registry | ScenarioData.SpaceObjectData, строка102; GameDataRegistry properties; SimulationEngine.ResolveObjectImage, строка1345 | Явно добавить class identity и class config; общий Tetrarch sprite не доказывает класс |
| Есть постоянный-speed перехват | [ApproachPursuitMath.SolveInterceptFlyThroughPlan](../../src/DeepSpaceSaga.Motion/ApproachPursuitMath.cs), строка342 | Переиспользовать shared math, не писать UI-only физику |
| Approach планирует trailing point и конечный курс цели | [ApproachLineCaptureMath.Plan](../../src/DeepSpaceSaga.Motion/ApproachLineCaptureMath.cs), строки16–48 | Торпеде нужен отдельный endpoint contract на центр; не менять Approach поведение |
| 1world unit=100m, Combat100m/px | EngineRequirements §2; ObjectMotionSnapshot, строки8–12; Settings.gameSettings.tacticalMap.metersPerPixel | Допуск попадания5 wu=0.5km, не текущие5 экранных px |
| Модули/команды загружаются рекурсивно из данных | EngineContentLoader.LoadRegistryFromSettingsFile, строка101; ModuleImplementationDto, строка776; Settings.typeData | Параметры оружия принадлежат module implementation; новый class path должен быть подключён явно |
| Команда уже несёт явную цель и адрес модуля | PlayerCommand, строка7; GameSessionScreen.SendCommandFromPanel, строка750 | Переиспользовать session API, не вводить прямой Client→Engine вызов |
| Панелей четыре и enablement для object command недостаточно для busy | CommandsPanel.Panels, строка55; GameSessionScreen.IsModuleCommandEnabled, строка706 | Нужна новая группа и отдельные pending/busy/self guards с тестом существующих панелей |
| Два временных домена уже разделены | SimulationClock.Update, строки48–60; AuthoritativeSnapshot.MotionTimeMs; SimulationEngine.EconomyTime.AdvanceWorldTo, строка12 | Полёт на physical MotionTimeMs, UI эффекты на monotonic real time; не календарный multiplier |
| AdvanceMotionTo имеет early return без других активных систем | SimulationEngine.AdvanceMotionTo, строка3120 | Combat must participate in fast-path guard и motion boundaries |
| Save version9, строгий object whitelist | ScenarioData.SaveFormat, строка26; ScenarioLoader.KnownObjectTypes, строка12 | Missile/Wreck константы сами по себе не дают поддержку Save/Load |
| Missile уже есть среди Contracts type strings | SpaceObjectType, строка12; Wreck отсутствует | Использовать Missile для projectile и добавить Wreck, не duplicate Torpedo type |
| Полный экран схемы корабля пока placeholder | ShipScreen summary, строки8–16, SectionLines строка46 | «Новое помещение» здесь — изменение canonical hullLayout/loadout; не включать redesign ShipScreen |
| Объекты выбираются среди render states с отдельным hit radius | GameSessionScreen.FindNearestObjectId, строка885; ObjectInfoPanel.BuildLines, строка163 | Mouse picking и physical collision — разные радиусы |
| JSON под Data уже копируются рекурсивно | DeepSpaceSaga.Client.csproj, строка264 | Новые ship/launcher/palette данные включаются без произвольного csproj расширения |
| MarketProfiles имеет специализированный трёхклеточный корабль | Scenarios/MarketProfiles/scenario.json hullLayout; остальные перечисленные5 сценариев имеют standard10-cell ships | Не назначать класс по общей картинке/роли и не расширять demo hull без основания |

## Границы и отношения с прежними требованиями

- Заменяется для этого MVP: вероятностная точность, рейтинги операторов, дальность, длительные КД, урон модулям и противоракетный перехват из черновика. Шанс100%, условный оператор, infinite ammo, busy только на время полёта.
- EngineRequirements §45 содержит старую платформенную модель damage; §57.4 уже помечает её как требующую пересмотра. EP-0006 задаёт hull-only HP для explicit Tetrarch; распределение урона по клеткам не реализуется.
- §57 standard loadout получает одну новую cell и module. Не переносить корпусную прочность в StructurePoints модуля.
- Статические string-константы Missile/Explosion не означают готового projectile lifecycle. Explosion здесь только клиентский эффект, не persistent space object.
- Не нужны EP-0002/3/4 и полный refactor карты EP-0005. Используется реально существующий pipeline; если к моменту реализации он изменён, сначала актуализировать Code context.
- Включено: все согласованные D01–D17. Не включено: NPC attacks/defence, real operators/skills, RNG hit chance, ammo economy, cooldown balancing, range/fuel/TTL, self-destruct, sounds, area damage, salvage/loot, victory screen, полноценный ShipScreen, баланс длительности2–3 минуты.

## Технические решения планирования (не выдавать за ответы пользователя)

- **A01 — помещение:** cell(3,2), slotSize1, новое structural помещение рядом с cargo(4,2). Существующие10cells/модули не двигаются. Применяется к стандартным Тетрархам PlayerShipOnly/Default/Default_500/Docked/Undocked; MarketProfiles сохраняет особый минимальный hull.
- **A02 — class identity:** `ship.tetrarch` задаётся явно в scenario/save. Новый ship registry JSON содержит HP; Missile uses TorpedoSnapshot. Непомеченный legacy ship не становится Tetrarch по sprite/name.
- **A03 — collision geometry:** 5 wu — включающий границу радиус вокруг центра каждого чужого объекта, независимо от sprite size. Collision time — первое пересечение; explosion в фактической точке контакта торпеды, wreck в позиции центра погибшего корабля на это время. Нет splash. Объект, уже перекрывающий точку пуска, проверяется при первом physical advance; на паузе время не двигается.
- **A04 — недостижимость/потеря цели:** launch разрешён; при no solution продолжается bounded-turn pursuit, ETA=—, confirmed cross отсутствует. Если target удалён отдельно, торпеда продолжает последний курс, сохраняет исходный target ID и busy до столкновения с другим объектом; не переназначается автоматически на wreck и не взрывается по таймеру. Это минимальное защитное поведение, не новая игровая механика. Self-destruction остаётся backlog.
- **A05 — presentation lifetime:** 2000ms считаются от первого получения нового impact факта Client monotonic clock. После загрузки не восстанавливаются ни ring, ни след уже взорвавшейся торпеды; полный след активной торпеды восстанавливается. Исходный пользователь явно исключил взрыв из save, а правило terminal trail выбирается согласованно с ним.
- **A06 — motion solution:** отдельная shared torpedo geometry с endpoint at target centre; не требуется выход на курс/корму цели и не требуется новый глобальный optimal-control solver. Для обычной прямолинейной цели должен получаться реальный intercept с3km/s/90°/s. На иных типах допустим predictor/replan; физические collisions работают для всех.
- **A07 — rendering:** 5 px диаметры и50 px радиус — экранные/canvas пиксели, не world и не множитель UI scale. Core5 px, halo может выступать за core. HP bar width/height, line widths, dash pattern — локальные presentation параметры implementer; они не меняют игру.
- **A08 — resume/config:** новые игры читают отредактированный content; Save/Load сохраняет captured HP/weapon parameters для продолжающегося боя. Поддерживаемые legacy saves без combat не получают автоматический launcher retrofit/новый класс.
- **A09 — IDs/events:** projectile/wreck/event IDs детерминированы и не расходуют RNG. При контактах одного времени stable ordinal ordering. Уничтожение атомарно, event delivery переживает coalescing snapshots. Для MVP session-local impact журнал не теряет факты; он не сериализуется как анимация. Масштабирование/ack/compaction такого журнала — backlog, не повод молча отбрасывать события.
- **A10 — module boilerplate:** предлагаем massKg=2000, structurePointsMax=60, powerConsumptionW=0, baseCycleTimeMs=0 и neutral command factors с activation cost0; проверить требования loader. Эти поля не вводят HP торпеды, расход энергии/боезапаса или cooldown. Если loader требует положительную cycle metadata, разрешён нейтральный валидный placeholder, который combat executor не использует.
- **A11 — hidden knowledge:** разрешается стрелять по selectable unknown object, но новый UI не раскрывает скрытый класс/имя/HP. Physical collision не зависит от знания игрока; неизвестные payload маскируются как в существующем snapshot.
- **A12 — Wreck cleanup:** остатки минимальны, без crew/cargo/modules, не содержат механики спасения/добычи. Обязательны безопасная очистка ID-ссылок и существующее поведение отвязки фокуса. Корабль игрока исключён из контактов его единственной активной торпеды; сценарий его боевого уничтожения не создаётся NPC, поскольку NPC не атакуют.

## Общий контракт реализации

`content -> Engine authoritative state -> immutable Contracts -> session boundary -> shared Motion projection -> Client map/panels`.

1. Производство snapshot/Save идёт из согласованного world state; renderer не применяет HP, spawn/destroy или RNG. Preview не исполняет команды.
2. `torpedo.fire` адресуется `(playerObjectId, launcherModuleId, targetObjectId)`, принимает command identity; Engine повторно проверяет все guards. Отказ имеет нулевые side effects, повтор CommandId не создаёт второй projectile.
3. Ready→InFlight фиксируется атомарно. At first contact: close flight/history → apply hull damage if eligible → create Wreck if lethal → remove projectile → clear launcher reference → publish impact fact. Состояние между этими шагами не наблюдается.
4. Collision sweep охватывает весь прошедший физический интервал и boundaries изменений движения. Equal-time ordering описан в тикетах; большой accelerated step эквивалентен разбитым шагам в заданных numerical tolerances.
5. Сохранение обязательно проходит реальный путь file→loader→new engine/session. Сохраняется полная аналитическая история активного полёта, а не только ограниченный клиентский trail buffer. История не зависит от числа Render/snapshot calls.
6. `GameTimeMs` (календарь), `MotionTimeMs` (полёт) и monotonic real UI time (ring/trail expiry) не смешиваются.
7. Все новые DTO/методы/файлы в тикетах — **предлагаемый API**, не утверждение о наличии готового runtime. Точные overloads можно уточнять внутри указанного allowlist без изменения семантики; дополнительный файл требует обновлённого scope.
8. Совместимость: existing Approach/math, торговля/докинг, surveys, unknown knowledge, supported saves и command routing не регрессируют. При изменении общих validators сначала проверить реальные legacy fixtures; не чинить соседние файлы вне границ.

## Истории и безопасный порядок

| История | Тикетов | Зависит от | Наблюдаемый результат |
|---|---:|---|---|
| [US-0001 — Настраиваемые характеристики Тетрарха и торпеды](EP-0006-US-0001-combat-configuration/EP-0006-US-0001-combat-configuration.md) | 4 | none | Тетрарх имеет явный идентификатор класса и настройку 450 HP; другие классы не становятся уязвимыми из-за совпавшей картинки. |
| [US-0002 — Тетрарх получает помещение торпедного аппарата](EP-0006-US-0002-tetrarch-launcher-room/EP-0006-US-0002-tetrarch-launcher-room.md) | 3 | US-0001 | В canonical hull grid появляется новая structural cell (3,2) и один launcher; старые модули остаются на местах. |
| [US-0003 — Ручной пуск торпеды с упреждением](EP-0006-US-0003-manual-guided-launch/EP-0006-US-0003-manual-guided-launch.md) | 3 | US-0002 | Пуск из центра по текущему курсу корабля; скорость ровно 3 км/с независимо от носителя, поворот ограничен 90°/с, движение с упреждением. |
| [US-0004 — Три попадания превращают Тетрарх во врек](EP-0006-US-0004-impact-and-wreck/EP-0006-US-0004-impact-and-wreck.md) | 4 | US-0003 | Каждое столкновение с любым чужим объектом в радиусе 5 world units завершает торпеду ровно один раз; носитель и сама торпеда исключены. |
| [US-0005 — Игрок видит торпеду, прочность, взрыв и врек](EP-0006-US-0005-combat-map-feedback/EP-0006-US-0005-combat-map-feedback.md) | 5 | US-0004 | Жёлтая торпеда диаметром 5 px с blur, серый wreck диаметром 5 px; размеры не масштабируются с zoom. |
| [US-0006 — Пройденный путь, прогноз и предварительное наведение](EP-0006-US-0006-torpedo-trajectories/EP-0006-US-0006-torpedo-trajectories.md) | 3 | US-0005 | Во время полёта видны полный пройденный путь жёлтым сплошным, прогноз жёлтым пунктиром и жёлтый крест встречи, а также траектория исходной цели до встречи. |
| [US-0007 — Продолжение боя после сохранения и загрузки](EP-0006-US-0007-combat-save-load/EP-0006-US-0007-combat-save-load.md) | 3 | US-0006 | Реальная файловая загрузка восстанавливает активную торпеду, captured параметры, owner/module/target, route/history/distance, motion time, HP и IDs. |
| [US-0008 — Полный бой в существующем сценарии](EP-0006-US-0008-basic-combat-proof/EP-0006-US-0008-basic-combat-proof.md) | 2 | US-0007 | Сквозной прогон использует реальный scenario/content, PlayerCommand и session boundary; никаких teleport, direct HP edits или подмены торпед. |

Всего 8 историй, 27 тикетов. Подробная карта и покрытие — [Tickets.md](Tickets.md). Выбран последовательный порядок US-0001→…→US-0008; внутри каждой истории TK-0001→… . Каждый тикет зависит от предыдущего тикета, включая границы историй. Это осознанно безопасный порядок, не обещание независимого параллельного merge.

Каждый тикет: один production layer, matching test project, максимум 5 файлов вместе с тестами. Новые partial/helper files перечислены заранее. Новые/старые файлы различаются в Code context. Артефакты Board не являются разрешением реализовывать соседние задачи.

## Матрица приёмки эпика

| Gate | Обязательное доказательство | Владелец |
|---|---|---|
| Данные и схема | Реальные JSON load/copy, явный class ID, launcher room без overlap | US-0001/0002 |
| Пуск | Pause launch, movement independence, fixed target, duplicate/busy/no-target/self отказ | US-0003 |
| Физика | Реальный intercept, turn bound, continuous first contact, stable ordering/time partitions | US-0003/0004 |
| Результат | 450→300→150→0, one wreck/new ID/speed0/heading0, invulnerable obstacle, free launcher | US-0004 |
| UI | 5 px markers, HP, 2real-second ring/trail, selectable torpedo/wreck/info100%, configurable colors | US-0005/0006 |
| Resume | File save midflight, after1/2/3 hits, no duplicate impacts, full active trail, no old effects | US-0007 |
| Полная поставка | Реальные3 пуска из PlayerShipOnly через session; native smoke с pause/speed/zoom/restart | US-0008 |

## Gaps, риски и backlog

Не осталось блокирующих продуктовых вопросов для draft. Предположения A01–A12 доступны для review и не заменяют согласованные ответы.
- Shared intercept математически пригоден как основа, но текущий Approach endpoint нельзя просто переименовать: это отдельный bounded-turn solver ticket с regression gate.
- В исходном сценарии расстояние велико (около1118км на старте); гарантии2–3 минуты нет. Позиции не сокращаются без нового решения.
- Цель на недостижимом курсе может навсегда занять аппарат; намеренно отложенный self-destruct решит это позднее.
- Реальная UI/GPU проверка обязательна и ещё не выполнена. Screenshot из беседы — reference, не proof готового UI.
- Existing ShipScreen — stub. Работа по интерактивной схеме корабля/помещениям как UI-функции вне этого эпика; здесь изменяется canonical hull/loadout и Commands Panel.
- Полный active trail и session impact journal могут расти при очень долгой сессии. Аналитическое объединение неизменных сегментов предусмотрено; unlimited lifetime не превращать в hidden TTL.
- Если future content добавит более одного launcher или NPC fire, потребуется отдельное решение по операторам/target races/самому игроку; MVP content устанавливает один launcher.
- Legacy class identification намеренно не угадывается. Текущие saves открываются как раньше; полный новый combat feature доступен в новых явно classified сценариях.

## Review log / epic review log

- 2026-10-02T08:22:58Z: пользователь поручил создать EP-0006 целиком; изучены Board examples и три файла Agents. Все D01–D17 перенесены из разговора; место помещения выбрано по делегированному решению.
- 2026-10-02T08:22:58Z: выполнен read-only grounding source/data, выбран 8-story/27-ticket split с limit≤5files/≤5tickets. Production, requirements и старые Board-эпики не менялись.
- Planning validation: имена/IDs, frontmatter, локальные links, DAG, layer/allowlist counts и coverage проверяются перед выдачей. Это не runtime validation.
- Нет записи «утверждаю эпик» или индивидуального approval созданных тикетов. `stage: draft` сохранён для всех.


- Итоговая проверка planning package: PASS — 37 Markdown-файлов, 8 историй, 27 тикетов, 236 локальных ссылок; IDs/frontmatter/allowlists/layers/coverage согласованы, dependency cycles отсутствуют. Проверен UTF-8 без replacement characters и trailing whitespace; git status содержит только новый EP-0006. Runtime проверки не выполнялись.
