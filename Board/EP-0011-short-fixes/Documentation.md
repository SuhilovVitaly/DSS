# EP-0011 — Grounding и технический контракт

Planning timestamp: 2026-10-18T07:47:19+00:00. Baseline `ba1b3da258899b8ebe5892dc5f7da40f135c2127`, рабочее дерево до планирования чистое. Прочитаны AGENTS/CLAUDE и релевантные разделы EngineRequirements (источник истины), руководство D:/DeepSpaceSaga/Agents/DSS-StoryBuilder/AGENTS.md. Requirements-документы не изменяются. Missing requirements-engineer обойдён по прямому разрешению пользователя.

## Проверенные факты

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/FutureTrajectoryProjector.cs:42`, `ProjectInto(ObjectMotionSnapshot, List<FutureTrajectoryPoint>)`: ветка на строке 46 использует `CurrentManeuverForecast.Sample` для HostileCombat.
- `src/DeepSpaceSaga.Motion/CurrentManeuverForecast.cs:14`: sampler читает скрытые AccelerationKmS2/TurnRateDegPerSec текущего HostileCombat.Segment. Истории наблюдений здесь нет.
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/FutureTrajectoryProjector.cs:75`, `ProjectViewportInto(...)`: отдельный viewport/linear fast path. Оба entrypoints нуждаются в раннем NPC branch, иначе скрытые controls могут попасть в линию/её продолжение.
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:1828`: фактический вызов viewport-прогноза из отображаемой позы. Player helper на строке 1933 сохраняет прежний путь.
- `src/DeepSpaceSaga.Client/SnapshotBuffer.cs:88`, `Update(AuthoritativeSnapshot)`: legacy ingress. Presentation mode не должен получать наблюдения будущего full snapshot через него.
- `src/DeepSpaceSaga.Client/SnapshotBuffer.Presentation.cs:57–59`: Presented/HasCombatPresentation/PresentationSessionId; `PreparePresentation()` на строке 230 применяет все промежуточные revisions. `_presented = new(frame, now)` на строке 269 — точка наблюдения фактически применённого frame, до этой точки будущие сетевые данные не наблюдались.
- `src/DeepSpaceSaga.Contracts/SpaceObjectType.cs:7`: NpcShip. `ObjectMotionSnapshot.cs:57`: RenderObjectType. Область определяется типом, не наличием HostileCombat.
- `tests/DeepSpaceSaga.Client.Tests/FutureTrajectoryTests.cs:11`: существующая тестовая поверхность. EngineRequirements §§21–23 разделяют подтверждённую сцену и prediction и запрещают влияние визуализации на Engine.

Graphify query `forecast` выполнен read-only; релевантную цепочку он не раскрыл, выводы проверены в исходниках. Граф не перестраивался.

## Решения пользователя

D-01: «Если два тика (секунды) подряд был поворот в одном и том же направлении с одним и тем же градусом то предсказание должно продолжать эту тенденцию вне зависимости от тоо что действительно выбрал нпс кораль». То же правило для прямого движения. Игрок видит только карту и не знает режимов NPC.

D-02: «Вариант А — прямую по текущему курсу до подтверждения новой тенденции. Создай в борде эпик для коротких фиксов и юзерстори с тикетами для этого изменения.»

D-03: «Да, создай EP-0011, юзерстори и тикеты без requirements-engineer.»

Источник — текущий чат. Время записи решений 2026-10-18T07:47:19+00:00; точное время отправки исходных сообщений недоступно и не выдумывается.

## Технические предположения

Это выбранные при планировании параметры, отдельно от решений пользователя.

- A-01: тик = 1000 MotionTimeMs (физическая секунда), не wall-clock, FPS или 100-мс пакет. Два интервала требуют трёх поз. Секундная сетка начинается с первой доступной позы NPC.
- A-02: источник — подтверждённые позы, уже допущенные к отображению. На пересечении секундной сетки разрешена интерполяция между двумя такими позами: координаты линейно, курс по кратчайшей signed дуге. Наблюдения не экстраполировать. Разрыв соседних raw observations более 1000 мс (+1e-6 для double) сбрасывает историю; пропущенные секунды не достраивать.
- A-03: нормализовать signed delta через 0/360. Допуск равенства и нулевого поворота 0.001 градуса за секундный интервал. Ровно 180° неоднозначно: fallback/reset. Два ненулевых delta подтверждают поворот при одинаковом знаке и разнице не более допуска; rate = среднее delta за секунду. Иначе rate=0 сразу, старую тенденцию не удерживать.
- A-04: начало линии — текущая отображаемая поза, скорость — текущая наблюдаемая постоянная. Скрытое ускорение не использовать; нулевая скорость не создаёт движения. Прогноз не меняет текущую позу.
- A-05: прежние горизонт 200000 мс и шаг 250 мс, стиль линии сохраняются. Прямая продлевается до viewport прежним механизмом; дуга не получает скрытого terminal/route extension. Для v=SpeedKmS*10 units/с, h=direction, w=rate в рад/с: dx=v*(cos(h)-cos(h+w*t))/w; dy=v*(sin(h)-sin(h+w*t))/w; при w≈0: dx=v*t*sin(h), dy=-v*t*cos(h).
- A-06: eligible — известный живой NpcShip независимо от phase/HostileCombat/выбора. История общая для отображаемой карты, не зависит от камеры/selection. Unknown/destroyed/removed, новая session/load, откат времени, разрыв наблюдений очищают относящиеся записи. Пауза/повтор времени не наращивают историю; повтор времени с отличающейся позой сбрасывает её как discontinuity.
- A-07: не более трёх секундных samples и одной предыдущей raw pose на активный NPC; удалять ушедшие IDs. Нет неограниченного журнала или обработки истории на render frame.
- A-08: scope — будущая линия самого NPC. Прогнозы попадания торпед/ПРО и launch preview используют прежнюю модель; согласование этих прогнозов с моделью наблюдений — отдельный backlog. Не обещать, что эта история устраняет скрытые controls во всех боевых прогнозах.

## Связь с требованиями / gaps

Требование пользователя уточняет только будущую линию NPC и заменяет её прежнюю зависимость от внутренних controls. Точная интерполяция подтверждённой сцены и Engine сохраняются. Блокирующих вопросов нет. Порог A-03, gap policy и постоянная скорость — явные assumptions; native/human gate OPEN. Глобальная фильтрация DTO, сохранение истории и общая переработка боевых прогнозов вне scope.

## Реализация 2026-10-18 — supersedes planning-only scope

Первые разделы этого документа сохраняют исходный grounding timestamp и решения D-01–D-03. Текущий пользовательский запрос отдельно разрешил исполнение по EpicExecutionPrompt. Добавлена обязательная US-0002 документации; её scope включает уточнение EngineRequirements §22 и актуализацию всех затронутых текстов. A-01–A-08 сохранены; вместо прямого деления разности cos/sin на w используется эквивалентная sinc форма интеграла для устойчивости при малом w. Transport/API/Engine не меняются. Named tests, полные команды, публикация и отдельный native gate: [ImplementationStatus](ImplementationStatus.md). Текущая реализация: [инженерный контракт](../../Documentation/04-Engineering/NpcObservedTrajectories.md).

## Расширение палитры — 2026-10-18T10:03:20Z

Новый запрос: «сделай из этого тикет в боарде и последнем эпике». Добавлена [US-0003](EP-0011-US-0003-tactical-map-palette/EP-0011-US-0003-tactical-map-palette.md) и один [продуктовый draft-тикет](EP-0011-US-0003-tactical-map-palette/EP-0011-US-0003-TK-0001-tactical-map-color-redesign/EP-0011-US-0003-TK-0001-tactical-map-color-redesign.md) с полными таблицами RGB/HSL S/opacity из чата, правилами отображения, инвентаризацией source seams и AC/validation. A-P01: последний эпик по максимальному ID — EP-0011. Пользователь разрешил материализацию Board, не реализацию/публикацию/запуск игры.

Текущий baseline: `f8846f055e16e19b0371f949ff41f3c02ef9a3e0`, имеются сторонние Client/Tests/scenario изменения; они сохранены. Gameplay/Engine source of truth и действующие design docs не изменены. Старые D-01–D-03/A-01–A-08 описывают прежний scope и не являются решениями по палитре. Новые D-P/A-P/G-P находятся в тикете.

Readiness: один запрошенный продуктовый draft, перед execution нужна декомпозиция по лимиту пяти implementation files. Implementation NOT STARTED, automated/native/product NOT RUN; прежние evidence не доказывают готовность палитры.

## Palette execution — 2026-10-18

D-P03: пользователь разрешил подготовить исполнительные тикеты и немедленно реализовать их с проверками, без commit/push и запуска игры. Предыдущие planning-only ограничения US-0003 superseded. Полный продуктовый TK-0001 сохранён; четыре implementation tickets в US-0003 и три в связанной US-0004, каждый <=5 allowlisted files. Production layer Client, tests DeepSpaceSaga.Client.Tests; Engine/Contracts/Motion/save/AI/RNG/forecast math не изменены.

Точные APIs/allowlists и dependency order: [Tickets](Tickets.md). Реализация DONE; focused175/175, full2085/2085, Client build0 warnings/errors, diff PASS. Scoped format остальных файлов PASS; 9 прежних diagnostics GridRendererTests lines22/23/60 подтверждены в HEAD, оставлены как baseline debt. [Execution commands и ограничения](ImplementationStatus.md#palette-execution--2026-10-18), [validation summary](evidence/palette-validation.json). Native screenshots/product readability/FPS OPEN/NOT RUN. Commit/push NOT RUN. Историческое evidence US-0001/US-0002 сохранено.
