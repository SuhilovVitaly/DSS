---
epic: EP-0008-player-defense-and-hostile-combat
title: "Третья фаза боя: активный противник и ручная защита игрока"
stage: draft
created: 2026-10-07
revision: 1
current_review: complete
story_count: 13
ticket_count: 44
validation_status: planning-only
source_request: "После интервью создать следующий эпик с юзерстори и тикетами по образцу EP-0006/EP-0007."
---

# EP-0008 — Третья фаза боя

Враждебные вооружённые корабли преследуют игрока, выходят на относительную орбиту и стреляют торпедами. Игрок в боевом сценарии управляет двумя установками ПРО: оставляет автоматику включённой, выбирает вражескую торпеду и при необходимости запускает дополнительные противоракеты вручную. Установки определяют точность, дальности и время полёта; гибель игрока завершает бой окном поражения.

## Источник, статус и полномочия

Продуктовый scope согласован в 51 ответе пользователя: [Decisions.md](Decisions.md). Этот документ содержит итоговые правила; Q40 имеет приоритет над ранней постоянной скоростью Q20. Технические A01–A12 ниже — допущения автора плана, не ответы пользователя.

Перед нарезкой прочитаны все70 Markdown-документов Documentation и материалы EP6/EP7, затем выполнен read-only grounding текущего кода. Обязательный в AGENTS навык requirements-engineer не найден в доступных skill roots/репозитории/Agents; для структуры использованы доступные DSS-EpicBuilder/DSS-StoryBuilder и EP6/EP7. Пользователь прямо запросил полный эпик с тикетами после уточнения требований, поэтому дополнительные approval-раунды образца EpicBuilder не вводились. Идеи сверх согласованного scope не добавлены.

Это **план**, не реализованная фаза. Статусы draft; current_review=complete относится к полноте нарезки. Production build/tests/native здесь не запускались. Создание плана не равно выполнению [EpicExecutionPrompt](../EpicExecutionPrompt.md), commit/push реализации или пользовательскому APPROVED.

[EngineRequirements](../../Documentation/01-Requirements/EngineRequirements.md), [CLAUDE](../../Documentation/00-Process/CLAUDE.md) и [правила документации](../../Documentation/00-Process/DocumentationSystem.md) сохраняют архитектурную силу. Gameplay-изменения этой фазы прямо перечислены ниже; canonical требования обновляются по факту исполнения в последней истории.

## Проверенный baseline и необходимые изменения

HEAD: `6907c58bb7e86dc6cc00122567bd4329ba852626`, проверка 2026-10-07. В исходном status присутствовали посторонние untracked `Board/EP-0001-trading-system/ImplementationStatus.md` и `Board/EpicExecutionPrompt.md`; они не изменяются этой задачей. Числа/версии из старых отчётов не подставляются вместо текущего кода.

| Текущий факт | Доказательство относительно корня DSS | Следствие |
|---|---|---|
| Save writer уже15, а не11/12 старых боевых отчётов | src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:32 | Планируем16, перепроверить baseline перед реализацией |
| Loader требует живой PlayerShip при загрузке | src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:231 | Terminal snapshot и запрет post-death saves нужны явно, не fake player save |
| Текущий ChanceTenths добавляет50 к разнице ratings | src/DeepSpaceSaga.Engine/Combat/InterceptionMath.cs:5 | Заменить формулу и все callers, а не только JSON |
| WeaponOperator вычисляет base*skill/50 | src/DeepSpaceSaga.Engine/SimulationEngine.WeaponOperators.cs:8 | Убрать влияние skills, сохранить назначение/identity |
| Fire требует owner=PlayerShip и не проверяет дальность | src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:21 | Общий внутренний executor + сохранённый публичный ownership guard |
| Autodefense обходит модули, reservation общая на target | src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs:29 | Добавить explicit module arbitration и manual exception |
| MissedCoast expiry есть, общего lifetime нет | src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs:41; SimulationEngine.Combat.cs:137 в Engine | Новый абсолютный deadline и равновременный приоритет |
| UI список содержит единственную группу Countermeasure Launcher | src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs:57 | Панели должны адресовать экземпляр, не только имя типа |
| Старый defense circle привязан к выбранному NPC | src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs:74 | Заменить weapon circles на hover panels |
| Начальный PlayerShipOnly paused, два оператора и одна ПРО игрока | src/DeepSpaceSaga.Client/Scenarios/PlayerShipOnly/scenario.json:9,202,220 | Новая комплектация только этого игрока; AI не двигается на pause |
| Engine basic имеет max4000m/s, linear400m/s², angular4deg/s | src/DeepSpaceSaga.Client/Data/Modules/Engine/modules-engine.json:10 | Реальные limits; не использовать turn90 торпеды для корабля |
| RuntimeMotion уже использует shared predictor/Approach | src/DeepSpaceSaga.Engine/RuntimeMotion.cs:10 | Добавить подтверждённые AI segments и collision path |
| Clock разносит календарь и физическое время; AdvanceWorldTo догоняет системы | src/DeepSpaceSaga.Engine/SimulationClock.cs:20; src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:12 | Поражение должно обрезать весь интервал, не только UI-speed |
| SaveAsync атомарно пишет захваченный Engine state | src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs:180 | Central post-death capture guard + file regression |

Полный ground-truth каждого изменяемого файла приведён в Code context тикетов; новые helper/DTO/test файлы явно помечены. Названия новых API предлагаемые. Текущие EP6/EP7 runtime зависимости существуют, но их native acceptance не объявляется закрытой этой проверкой.

## Итоговый игровой контракт

### Модули, точность и полёт

| Параметр | База | Владелец |
|---|---:|---|
| Дальность запуска торпеды | 200 км включительно | Тип торпедной установки |
| Манёвренность торпеды для вероятности перехвата | 5 | Тип торпедной установки, captured в торпеде при Fire |
| Торпеда speed / turn / damage | 3 км/с /90°/с /150HP | Тип торпедной установки |
| Точность ПРО | 55 | Тип установки ПРО, captured при её Fire |
| Auto / manual дальность ПРО | 100 /200 км включительно | Два независимых параметра типа установки ПРО |
| ПРО speed / turn | 12 км/с /90°/с | Тип установки ПРО |
| Максимальное время полёта ПРО | 60000 физических ms | Параметр установки; captured в полёте |
| Перезарядка ПРО | 10000 физических ms | Тип установки ПРО |
| MissedCoast после неуспеха | 2000 физических ms, не дольше remaining lifetime | Существующее правило EP7 |
| Контакт снарядов / hull contact | 500м =5world units | Существующая геометрия EP6/EP7 |
| Hull стандартного Тетрарха | 450HP | Существующий class config |

Шанс: `ChanceTenths = Round(clamp(Accuracy − CapturedTorpedoManeuverability,0,100)*10, AwayFromZero)`. 55−5=50.0%. Никакой прибавки50 и никаких множителей operator skill. Навык можно показывать как данные персонажа; назначение обязательно, skill0 допустим.

Один roll1..1000 из сохраняемого `combat.countermeasure.intercept.v1` при первом физическом контакте; успех `roll <= ChanceTenths`. Контакт ручной ПРО с0% всё равно расходует один draw, результат Miss; при100% draw также обязателен. Preview/отказы/потеря цели/timeout не делают draw.

При пуске торпеды фиксируются физические параметры и манёвренность. При пуске ПРО фиксируются accuracy, maneuverability цели, chance, mode, параметры движения и абсолютный `ExpiresAtMotionTimeMs=LaunchMotionTimeMs+MaxFlightTimeMs`. Все снаряды стартуют из центра по курсу носителя с собственной скоростью без прибавления скорости корабля, как в EP6/EP7. Replan, смена навыка, auto toggle и Save/Load не меняют captured значения. Новые типы модулей могут иметь иные значения; одинаковый тип не получает owner-specific бонусов.

### Автоматический и ручной перехват

Auto: рабочий ready аппарат, назначенный оператор, autoEnabled, incoming target на защищаемый корабль, chance>0, torpedo внутри100км, достижимый контакт до hull impact и не позже expiry ПРО. Только при продвижении физического времени.

Manual: `defense.fire(objectId,moduleId,targetTorpedoId,commandId)`; живой аппарат игрока, ready/operator, выбрана вражеская торпеда<=manualRange. Цель может лететь к другому объекту;0% или отсутствие решения не блокируют запуск. AutoEnabled не является guard ручного Fire. На паузе spawn/busy происходят, movement/lifetime стоят. Вторая ПРО может одновременно преследовать ту же цель.

Один активный снаряд на аппарат. После его удаления начинается reload; у второй установки собственный state. Первая успешная попытка любого режима резервирует **общий для всех установок automatic-attempt flag на данной торпеде**. Дополнительных auto-пусков не будет; manual игнорирует исчерпанный auto flag. Невалидная/rejected команда не ставит flag.

При нескольких угрозах сохраняется priority ближайшего hull impact, затем targetID. Для выбранной угрозы auto выбирает допустимую установку с самым ранним временем контакта, затем с максимальным шансом, затем по стабильному module ID. Если лучшая установка занята, рассматриваются остальные; reservation делается атомарно.

Успех удаляет цель и победившую ПРО; остальные Guiding ПРО теряют цель и удаляются без roll с началом своего reload. MissedCoast после уже случившегося промаха не сокращается от target loss, но ограничивается expiry. Потеря носителя удаляет его ПРО без reload; истечение lifetime не наносит урон и не считается неуспешным броском.

### Враждебный бой и относительная орбита

Все живые Enemy NpcShip с торпедной установкой получают поведение против игрока; старт не ограничен радиусом обнаружения. На стартовой паузе нет движения или AI-fire. Friend/Neutral не атакуют; изменение отношения учитывается на следующей physical boundary.

Вооружённый противник сближается, затем стремится к относительной орбите с центром в текущем движущемся корабле игрока и радиусом `0.5 * TorpedoRangeKm` (база100км). Направление обхода выбирается по меньшему первоначальному развороту и сохраняется на время боя/SaveLoad. Он может менять курс и скорость в рамках реального двигателя. При недостижимой орбите — pursuit на максимально доступной скорости с повторной оценкой входа в орбиту. Топливный расход на эти манёвры не вводится.

AI не телепортирует корабль и не привязывает его кинематически к игроку. Shared Motion исполняет подтверждённый путь; Client не принимает решений AI. Фазы Сближение/Выход на орбиту/Удержание и desired radius показываются только в info выбранного врага.

Внутри диапазона своего аппарата противник стреляет по живому игроку. Одна торпеда на launcher; после окончания её полёта следующий запуск без дополнительного cooldown, если всё ещё есть дальность/оператор/готовность. Дальность ограничивает только пуск обеих сторон, не дальнейший полёт. Ручной торпедный Fire игрока сохраняет прежние допустимые non-own цели кроме ПРО.

### Комплектация и интерфейс

Только игрок Player Ship and Pirate получает две установки того же базового типа, двух разных операторов ПРО и третьего человека-торпедиста. Новый вариант living module на3 каюты заменяет его старый; старый тип на2 не меняется. Пират сохраняет один аппарат ПРО; другие игроки/сценарии не получают усиленный loadout.

Для каждого player defense module — отдельная панель с module identity, оператором, toggle, Fire, шансом и расчётом, состоянием, flight/reload countdown. Выбрана non-torpedo/неhostile цель или нет ready/operator/range — Fire disabled с понятной причиной. При0%/unknown encounter Fire остаётся доступным. Pending/error связываются с конкретными CommandId/module, не блокируют второй аппарат и не переназначают цель.

Hover доступного Fire: прогноз shared Motion, предполагаемая встреча и authoritative chance; при неизвестной встрече явная надпись, без ложного креста. Hover панели торпедного аппарата показывает его range; hover конкретной ПРО-панели — её auto/manual radii различимым стилем. Без hover weapon circles не рисуются, прежний selected-pirate круг тоже убирается. Радиусы world-space, маркеры остаются5 canvas px.

Flight countdown виден в панели, selected PR info и возле ПРО на карте. Ordinary torpedo travelled trails остаются скрытыми; PR trails сохраняются. Журнал различает module/mode, accuracy/maneuverability/roll, success/miss/expiry/lost target. Реальные2s UI effects не ускоряются и не повторяются после Load.

### Поражение, сохранения и временные границы

Lethal hit игрока создаёт terminal outcome один раз; сохраняются death event, final journal и wreck, симуляция останавливается. Окно имеет только «Загрузить» и «Главное меню»; Escape/фон/Resume не возвращают в running мир. Отмена/ошибка Load остаётся в поражении; успешная загрузка совместимого pre-death save создаёт живую сессию.

Post-death Capture/Save запрещён в Engine и всех UI/transport entrypoints. Слоты не повреждаются. Нельзя продвинуть calendar/production/AI/RNG в остатке большого интервала после поражения или снять terminal stop закрытием modal.

SaveFormat **16** предлагается относительно текущего15; старые файлы сохранений отвергаются с понятным сообщением и без migration. Version0 New Game templates — не старые saves; они остаются валидным источником новых игр. Current-format saves до гибели сохраняют весь мир/экономику/географию, две установки, flight/TTL/mode, frozen данные, RNG/attempts, AI/направление/epochs/segments, sequences/commands/journal/selection. Invalid load атомарен.

| События одного физического времени | Приоритет |
|---|---|
| Hull impact торпеды | Первым; lethal player hit терминален |
| Удаление потерявших цель/носителя Guiding ПРО | До попытки контакта с уже удалённой целью, без RNG |
| Контакт ПРО с живой целью | Затем, stable projectileID/targetID; один roll на соответствующий контакт |
| Expiry ПРО и окончание MissedCoast | После контактов; reload от фактического удаления |
| Готовность аппаратов и новые auto-пуски | После разрешения контактов/удалений, stable ordering; без loops одного timestamp |

Время flight/coast/reload/AI — MotionTimeMs. Календарь300× не ускоряет физические интервалы. UI effects — monotonic real time. Пауза допускает только согласованные manual действия (включая старый torpedo.selfDestruct), без продвижения физики или AI.

## Технические assumptions и разрешение краёв

- **A01 — числовая схема.** Accuracy/Maneuverability — конечные неотрицательные decimal, upper bound100; range/speed/turn положительные конечные, maxFlightMs/reloadMs положительные с checked deadline. Точность вероятности остаётся0.1%. Отсутствующие новые поля у weapon definitions должны быть исправлены в каталоге/тестах, а не получать незаметный old rating.
- **A02 — hostility.** Ручная enemy torpedo определяется текущим Enemy отношением живого owner к игроку; если owner уже удалён, используется captured launch provenance (не меняется автоматически на neutral). Свои/дружественные/нейтральные снаряды не manual targets. Это правило выбора, не новая дипломатия.
- **A03 — loadout.** Второй ПРО занимает уже свободную structural cell(3,1), не расширяет hull и не двигает модули. Новый living typeId `living.quarters.mk1.three-cabins` сохраняет прочие свойства базового типа. Новому оператору skill50 только как metadata; имена/ID выбираются по существующим conventions.
- **A04 — controller.** Fixed100ms physical decision cadence, analytic execution между boundaries. Handedness сравнивается по абсолютному кратчайшему повороту к CW/CCW касательному направлению относительной орбиты; tie→CW. При совпадении поз radial direction берётся из heading корабля. Для достижимых stationary/равномерно движущихся тестовых целей после стабилизации удерживать |distance−R|<=5%R минимум3 полных оборота без смены направления; hysteresis выхода10%R исключает дрожание фаз. Время стабилизации ограничено независимой геометрической оценкой в тестах, не произвольным бесконечным ожиданием. Невозможность из engine speed/turn limits ведёт pursuit. Это baseline алгоритмической приёмки, не обещание глобально оптимального манёвра.
- **A05 — несколько оружий/engine.** Контролирующий engine и торпедный launcher выбираются по стабильному ordinal moduleID; их характеристики задают orbit radius/limits. Прочие ready торпедные аппараты, если имеются в custom world, стреляют независимо по своим range. Неоперабельный engine/docked ship не маневрирует; weapon guards действуют независимо. Стандартный scope не создаёт дополнительные торпедные аппараты.
- **A06 — отсутствие интерцепта.** Manual fallback остаётся bounded pursuit до contact/target loss/expiry. Auto не стреляет если возможный контакт только после срока жизни или hull hit. Unknown ETA отображается как неизвестное, а не ноль. Flight prediction не обещает успех roll.
- **A07 — равное время и прогресс.** При нескольких miss на одной живой цели последующие контакты могут разрешаться; после первого success остальные получают target loss без draw. Если miss точно на expiry — roll, затем немедленное удаление/reload. Во избежание нулевременного цикла повторный AI Fire после завершения предыдущей торпеды возможен не раньше следующей целой physical ms; это resolution времени, не отдельный gameplay cooldown.
- **A08 — pause ручных команд.** Paused Fire фиксирует deadline от текущего MotionTimeMs, но время не расходуется. Два manual Fire разных модулей в одном timestamp допустимы; каждый idempotent по CommandId. При racing auto/manual обрабатывается фактически принятая первая команда/событие, без reservation на основе UI preview.
- **A09 — terminal clock.** Collision fact хранит точный physical timestamp; integer session clock останавливается на первой целой ms не раньше него. Никаких новых gameplay events в остатке интервала; calendar cursor согласован с остановкой. Сохранение неизменного snapshot, захваченного до смерти, может завершить запись после неё; новый capture после death запрещён.
- **A10 — новая схема.** Save16/defense payload2/hostile state1 предлагаемые версии; на начале реализации проверить текущий HEAD и при конфликте выбрать следующий номер. Old saves не переписывать/не удалять; rejection покрывает manual/quick Load. Автосейв как новая функция не добавляется.
- **A11 — стили и scope.** Переиспользовать Xenon/локализацию/палитру, gray preview, разные auto/manual radius styles. Не делать общего переименования Screen классов или реорганизации всего UI. Старые навыки/тексты исправляются только там, где иначе утверждали бы неверную формулу.
- **A12 — исполнение.** Обычный production ticket планируется в одном layer и<=5files вместе с тестами. Исключения явно обозначены: test-only compatibility audit, полный documentation impact audit и generated Graphify набор. При запуске по EpicExecutionPrompt исполнитель может обоснованно расширить карточку без повторного вопроса; реальный перечень фиксируется до commit. Вызванные текущим тикетом старые тесты исправляются в этом же шаге, не оставляются заведомо сломанными до финального аудита.

## Порядок, покрытие и gates

13 историй,44 тикета. Полная карта: [Tickets.md](Tickets.md). Консервативная последовательная цепочка учитывает общие SimulationEngine partials. Каждая история имеет AC, каждый тикет serves; ссылки на решения Q находятся в историях. Последняя история обновляет **все релевантные документы проекта**, включая папки вне Documentation, затем Graphify.

Функциональные gates: новые module values → loadout → NPC fire/range → AI motion → manual fire → lifetime/arbitration → panels → map/journal → terminal state → recovery UI → save continuation → integrated/native acceptance → project documentation.

Планировочная готовность требует валидных IDs, ссылок, DAG, покрытия AC, существующих baseline files и явно новых файлов. Runtime готовность требует реальных tests/build/review; native окно — отдельный обязательный gate. Производительность нового AI/нескольких снарядов проверяется на реальном случае, не наследуется от старого performance report.

## За границами

Новые типы оружия (лазеры/пушки), hull-grid module damage, ручной подбор/рост экипажа, новые навыковые формулы, limited ammo, fuel economy манёвров, AI патрули/территории/обнаружение/групповая тактика, obstacle avoidance кораблей, смена целей AI кроме игрока, новая добыча/лут/победный экран, поддержка миграции старых saves. Бесконечный полёт торпеды при недостижимой цели остаётся допустимым; игрок уже имеет selfDestruct, NPC не получает неоговорённый TTL торпед.

## Review и результат текущей задачи

- 2026-10-07: зафиксирован журнал51 вопросов, позднее решение variable-speed orbit сохранено вместо постоянной скорости.
- Проверены текущие version15, common combat math, singleton panel, actual save path и engine limits; новые API не выданы за существующие.
- Созданы только Board-артефакты EP-0008. Canonical requirements, production, тесты, предыдущие эпики и промт выполнения не изменены.
- Planning validation: PASS — 60 Markdown-файлов,13 историй,44 тикета. Проверены уникальность ID/соответствие папок и файлов, dependency DAG и существование зависимостей, ссылки, coverage всех AC, до5 тикетов на историю и до5 файлов обычного production-тикета. Явные inventory-исключения A12 проверены отдельно. Битых относительных ссылок, replacement characters, conflict markers и trailing whitespace не найдено.
- Реализация, сборки, тесты продукта, native acceptance и commit/push в этой задаче не выполнялись. Все карточки остаются draft; planning PASS не является одобрением пользователя или runtime PASS.
