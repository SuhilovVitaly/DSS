# Наблюдаемый прогноз будущей линии NPC — EP-0011

Дата реализации: 2026-10-18. Client-only; реализация и headless validation завершены, native/human acceptance OPEN / NOT RUN. [Board](../../Board/EP-0011-short-fixes/EP-0011-short-fixes.md) · [команды, SHA и evidence](../../Board/EP-0011-short-fixes/ImplementationStatus.md).

## Пользовательский результат

Будущая линия известного живого NpcShip продолжает наблюдавшееся движение: три позы образуют два секундных интервала. Одинаковые ненулевые signed изменения курса подтверждают поворот; в остальных случаях линия сразу прямая по текущему курсу. Скрытые phase, mode, acceleration, turn controls, route и будущие пакеты не определяют эту линию. Два прямых интервала остаются прямыми. Прогноз не меняет отображаемую текущую позу и authoritative мир.

## Источник и время

SnapshotBuffer владеет NpcMotionObservationHistory. ObserveScene получает только ObjectId, физическое MotionTimeMs, X/Y, SpeedKmS, Direction — hidden controls в его DTO отсутствуют. В legacy режиме наблюдается принятый Update snapshot. При combat presentation baseline наблюдается при bootstrap новой сессии, затем каждый промежуточный frame в PreparePresentation после его допуска к отображению. Ingress full snapshots и ожидающие frames не являются наблюдениями; compact frames предварительно сливаются с display baseline, отсутствующий в compact delta объект остаётся живым до явного RemovedObjectIds.

Сетка 1000 MotionTimeMs начинается с первой доступной позы NPC. Только интерполяция между подтверждёнными доступными позами: координаты линейно, курс по кратчайшей signed дуге. Классификатор хранит headings; координаты нужны для обнаружения discontinuity и не участвуют в определении rate. Экстраполяция истории запрещена. Две позы не подтверждают поворот. FPS, camera, selection, wall clock, calendar time и скорость симуляции не создают дополнительные секунды наблюдений.

## Численные правила и lifecycle

Signed delta нормализуется через 0/360. Нулевой поворот: |delta| <= 0.001°. Поворот подтверждается, если оба delta ненулевые, одного знака, различаются не более 0.001°; rate — их среднее за секунду. Ровно 180° неоднозначно, используется fallback/reset. Несовпадение на очередном секундном отсчёте сразу даёт rate=0, прежняя тенденция не удерживается.

Разрыв raw observations более 1000 мс (+1e-6 допуска), rollback, новая сессия, новая SnapshotBuffer после load, unknown/destroyed/removal очищают соответствующую историю. Повтор той же физической позы/времени при паузе не добавляет sample; иная поза на том же времени сбрасывает track. Повторно появившийся ID начинает заново. Память: один raw pose и максимум три headings на активный NPC; отсутствующие IDs удаляются. История не сохраняется в saves.

## Геометрия и реальный путь карты

FutureTrajectoryProjector.Project/ProjectInto/ProjectViewportInto принимают optional npcTurnRateDegPerSec=0. NPC branch по RenderObjectType предшествует любому hidden/linear/discrete predictor. GameSessionScreen.DrawFutureTrajectories передаёт buffer.GetNpcObservedTurnRate и текущую отображаемую позу. Существующие правила selection и рисования сохраняются; история ведётся для всех eligible NPC независимо от selection и viewport.

Скорость постоянна и равна текущей отображаемой SpeedKmS, скрытое ускорение игнорируется. 1 км/с = 10 world units/с; heading 0° вверх, 90° вправо. При v=SpeedKmS*10, h=heading в radians, w=rate в radians/с:

- w != 0: dx=v*(cos(h)-cos(h+w*t))/w, dy=v*(sin(h)-sin(h+w*t))/w;
- w=0: dx=v*t*sin(h), dy=-v*t*cos(h).

Реализация использует алгебраически эквивалентную midpoint/sinc форму, устойчивую при w около нуля. Начальная точка точно равна отображаемой позе. Нулевая скорость не создаёт движения. Горизонт 200000 мс, шаг 250 мс (801 точка), прежние цвета/стили. Прямой viewport-прогноз продлевается до края прежним механизмом; дуга остаётся в горизонте и не получает hidden terminal/route extension.

## Границы изменения

Engine, Contracts, Motion, AI/RNG, save schema, guidance, реальное движение NPC, player navigation, орбиты и станции не меняются. CombatForecastCache, прогнозы торпед/ПРО, launch preview и cancellation сохраняют прежнюю conditional модель. Согласование этих прогнозов с наблюдаемой моделью NPC — отдельный backlog A-08. На текущем draw path общий target forecast подавляет дублирующую обычную линию для тех же combat targets; это прежнее поведение сохранено. Эта история не устраняет скрытые controls из всех combat forecasts.

## Покрытие и приёмка

| Критерии | Проверяемая граница |
|---|---|
| AC-01 | HiddenControlsDoNotChangeObservedNpcLine, FuturePacketAndDuplicateCannotRevealAi; rejecting predictor не допускает скрытого вызова |
| AC-02/03 | TwoEqualTurnsConfirmRate, TwoStraightIntervalsStayStraight, ChangedTurnImmediatelyReturnsToStraight, StraightThenTurnNeedsTwoIntervals |
| AC-04 | WrapAroundKeepsTurnSign, ToleranceAndHalfTurnBoundaries, OnlyPhysicalSecondsCount, ConfirmedInterpolationNeverExtrapolates, PauseAndSpeedDoNotInventSeconds |
| AC-05 | BothProjectionEntrypointsUseObservedRate, ObservedArcStartsAtDisplayedPose, StraightNpcExtendsToViewportWithoutHiddenTerminal; независимый analytic arc oracle |
| AC-06 | SessionAndObjectRemovalClearHistory, HistoryStorageIsBounded, LoadRemovalUnknownAndDestroyedResetObservation, CameraAndSelectionPreserveTrend, EquivalentPresentedFramesIgnoreRenderFrequency |
| AC-07 | PlayerAndOrbitalProjectionRemainUnchanged, полный Client и Engine CombatForecast; существующие assertions сохранены |
| AC-08 | Client 2057/2057, Engine CombatForecast 47/47; native/human отдельно OPEN |

Все три requested implementation tickets опубликованы последовательно, отдельный commit/push после каждого. Self-review выполнено, независимое review/APPROVED не заявлено. Raster render проверяет production draw wiring, не настоящее окно/GPU/input. Игру агент не запускал по явному ограничению истории.

Ручной gate: две секунды равного поворота дают дугу; следующий неравный секундный интервал даёт прямую; пауза не подтверждает trend; новая сессия сбрасывает историю. Выполнение пользователем не зафиксировано.
