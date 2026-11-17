---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0007-carrier-sensors
ticket: EP-0035-US-0007-TK-0003-sensor-content
title: Стартовые сенсоры с преимуществом 20 процентов
stage: draft
approval_kind: workflow
implementation_status: implemented-tests-pending
layer: content-data
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0035-US-0007-TK-0002-module-schema]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T11:23:54Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Original planning baseline; implementation status and published commits: see current execution ledger.

# Стартовые сенсоры с преимуществом 20 процентов

## Why
Выполнить owning слой [истории](../EP-0035-US-0007-carrier-sensors.md), не дублировать консоль EP-0030.

## Decisions
D-35-01…04 и дословный запрос — в [эпике](../../Documentation.md#grooming-2026-11-17). Board grooming разрешён; production не запрошен. Workflow approval не означает human review деталей.

## Assumptions
A-35-01…03 и G-35-01…02 перечислены в истории. API ниже — проект. Численный радиус G-35-02 закрыт D-35-20. Content остаётся draft только до фиксации отдельных технических параметров установки G-35-02M; радиус заново не согласовывать.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/Data/module-types.json | Grounding в истории; новые Sensor/Fog файлы создаёт этот тикет. | Только шаги ниже, без соседних механик. |
| src/DeepSpaceSaga.Client/Data/Modules/Sensor/modules-sensor.json | Grounding в истории; новые Sensor/Fog файлы создаёт этот тикет. | Только шаги ниже, без соседних механик. |
| src/DeepSpaceSaga.Client/Data/Ships/starter-carrier.json | Grounding в истории; новые Sensor/Fog файлы создаёт этот тикет. | Только шаги ниже, без соседних механик. |
| tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0035Us0007Tk0003Tests.cs | Новый matching test file. | Только шаги ниже, без соседних механик. |

## Public API after the change
Проект: module.sensor.player-start / module.sensor.pirate-start, R_player=1.20*R_pirate.

## Implementation steps
1. D-35-20: записать sensorRadiusWorldUnits=2000 для module.sensor.pirate-start и 2400 для module.sensor.player-start. С Ириной runtime даёт 2640. Радиусы утверждены; отдельно G-35-02M — зафиксировать массу/footprint/прочность/питание, не выдавать тестовые значения за баланс.
2. Создать пассивную категорию и два определения; оба обычных пиратских состава используют pirate-start.
3. Установить player-start с уникальным moduleId без overlap; сохранить Scanner. Проверить загрузку hull.
4. Если EP-0034 создал отдельный demo-starter-carrier.json, уточнить allowlist на фактически используемый Demo manifest; нельзя оставить New Game без сенсора. Не требуется ждать весь EP-0034.

## Served acceptance criteria
AC-01…04: вклад только слоя content-data; всю историю проверять после integration.

## Out of scope
Ресурсный Scanner, реализация патруля (владелец US0001/0002, G-35-01 закрыт), stealth/occlusion, тактическое отступление, экономика перевозок, новая save writer version, публикация.

## Invariants
EngineRequirements.md, «Архитектура и авторитет», «Время и единицы», «Мир и наблюдаемость»: Engine владеет контактами, 1 world unit=100 м, renderer не мутирует AI. Fog не меняет сенсор, RNG, движение и pursuit timers; не раскрывает future orders/скрытый loadout.

## Tests
Будущие проверки, NOT RUN:
- DemoEp0035Us0007Tk0003Tests.DemoStart_HasSensorWithoutOverlap
- DemoEp0035Us0007Tk0003Tests.PlayerRadius_Is120PercentOfBothPirates
- DemoEp0035Us0007Tk0003Tests.Scanner_RemainsInstalled

Из корня DSS создать уникальный абсолютный <run> внутри D:/DeepSpaceSaga/artifacts/validation/ep0035-sensors/.
Ограничить каждый test process tree 300000 ms, остановить дерево при timeout.
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter "FullyQualifiedName~DemoEp0035Us0007Tk0003Tests" --results-directory <run>/tests --logger trx
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <run>/build
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0035Us0007Tk0003Tests.cs
JSON проверять loader tests; native/product acceptance отдельно.

## Definition of Done
- Steps выполнены в allowlist, served AC покрыты вкладом слоя и integration evidence.
- Focused tests/build/format проходят либо записаны конкретные исходные failures.
- API, invariants, out-of-scope соблюдены; нет скрытой работы/допущений.
- Численные радиусы D-35-20 соблюдены; отдельно G-35-02M технические параметры установки записаны до исполнения content.
- Пользовательский путь проверяем, headless не подменяет native.

## Self-containment check
Prerequisites, API, files, steps и проверки перечислены. Grounding/решения в истории; при drift, особенно Demo manifest после EP-0034, сначала уточнить Board. G-35-01 о патруле не решается этим тикетом.


## D-35-20 — стартовые радиусы сенсоров

D-35-20 (пользователь): «Давай 200 км у пиратов, 240 у игрока и 264 с Ириной. Добавляй в тикеты. Теперь давай разберем повторные обнаружения: не хочется, чтобы диалог постоянно ставил игру на паузу».
Точные стартовые радиусы демо: оба пиратских сенсора 200 км = 2000 world units; базовый модуль игрока 240 км = 2400 world units; effective radius с назначенной Ириной 264 км = 2640 world units. Формула ровно 2400 + 0.10*2400, один раз. Пиратские демо-контакты используют 2000 world units, без неоговорённой надбавки.
Численная дальность G-35-02 закрыта. Отдельный G-35-02M — технические параметры установки модуля (масса, footprint, прочность, питание), которые надо записать перед content implementation; это не неизвестный радиус и не повторный вопрос о дальности.
Capture radius D-35-19 остаётся 20 км = 200 world units. Для sensor detection граница distance<=effective radius; для capture <=200. Показ debug-маркера вне радиуса не вызывает sensor acquisition.
Текущее уточнение: G-35-04 CLOSED D-35-21/33 — один automatic report на stable group за кампанию, все новые группы одного шага в одной сводке. Manual report вне утверждённого объёма демо.
Named tests: PirateSensor_2000WorldUnits; PlayerModule_2400WorldUnits; IrinaEffective_2640WorldUnitsExactlyOnce; AtEffectiveRadius_Detects_Outside_DoesNot; SaveLoad_PreservesBaseSkillAndEffectiveRange; FogVisibility_DoesNotChange2000_2400_2640. Проверить отсутствие compound bonus и различие base/effective.

## D-35-29: ETA до20км по текущим курсам при1×
Пользователь: «Да, принимаем до 20 км по текущим курсам и время при 1×».
ETA — earliest nonnegative время входа в distance<=20км при постоянных НАБЛЮДАЕМЫХ текущих world velocities двух объектов. r=observed target position-player position; v=observed target velocity-player velocity; найти минимальный t>=0 с |r+v*t|<=200world units, motion units согласовать. Не использовать будущие waypoint/orders, скрытый курс, мгновенный поворот/ускорение pursuit. Поле evaluation timestamp привязано к наблюдению, UI не пересчитывает скрытые данные.
Уже внутри/на20км =>0 («на дистанции боя»). Нет пересечения/расхождение => «при текущих курсах сближения до дистанции боя нет», не infinity/NaN/выдуманное время. Zero relative speed снаружи=>нет прогноза; tangent включается; numerical near-zero/discriminant обработать устойчиво, результат finite/nonnegative.
Показывать ожидаемое реальное время ожидания при игре1×, явно пометить «при1×». Grounding EngineRequirements.md:21–31: на1× одна motion second проходит за4 real seconds; поэтому display duration=4*t_motion. Calendar factor300 здесь НЕ используется. Current pause/speed не меняет этот прогноз; pause не создаёт division by zero.
G-35-03 CLOSED: цель, формула и шкала согласованы. US0010 contracts фиксирует nullable forecast/reason/observation timestamp, telemetry считает authoritative value, dialogue content/client отображает готовое значение и единицы.
Tests NOT RUN: Closing_EntryTo20km; Tangent_Included; AlreadyInside_Zero; Diverging_NoForecast; ZeroRelativeVelocity_InsideOutside; NearZero_Finite; FirstEntry_NotClosestApproachTime; MotionSecond_DisplaysFourRealSecondsAt1x; PauseAndCurrentSpeed_DoNotChange1xForecast; FutureOrdersNotRead.
## D-35-30: скрипт вместо недоступной Ирины
Пользователь: «Вместо нее работает скрипт с 80% эффективностью».
D: если Ирина недоступна, работу выполняет скрипт с эффективностью80%. D-35-31 утвердил192км и±2/±4; G-35-06 остаётся только для unavailable/замены/module gate.
A-35-13 SUPERSEDED D-35-31: fallback192км=0.8*240км base, а не0.8*264км.
A-35-14: unavailable=отсутствует/не назначена/недееспособна по существующим crew состояниям; конкретную проверку согласовать с реальными supported statuses, не создавать фиктивный health API. Неработающий sensor module отдельно: скрипт не ремонтирует и не обходит physical module gate.
Нет фиктивной реплики/портрета Ирины: системный sender для script. A-35-15: сохраняются один automatic report/group и pause/dismissal policy; fallback/replacement не сбрасывает discovery/consumed flag. Способ presentation пока предложен как A.
G-35-06F CLOSED D-35-31: script±2 singleton/±4 squad; Irina±1/±2 сохраняется.
## D-35-31: численные параметры скрипта
Пользователь: «Да, принимаем для скрипта 192 км и погрешность ±2/±4».
Fallback radius=192км=1920world units=0.8*240км BASE установленного стартового player sensor; не0.8*264км с бонусом Ирины. A-35-13 pending superseded этим D.
Script report содержит одно целое «около N»: actual total±2 для singleton, actual aggregate total±4 для squad2xЛогово. Допустимые single values Логово4..8,Танкред8..12; squad8..16. Это НЕ отображаемые диапазоны и не изменение actual total6/10/12. Точные типы раскрываются, light/heavy/equipment остаются скрыты.
G-35-06F numerical accuracy CLOSED; G-35-06 radius base CLOSED. Starter Irina264км и±1/±2 сохраняются. ETA D-35-29 вычисляется по observed vectors до20км и отображается при1×;80% не искажает пеленг/дальность/ETA автоматически.
A-35-14 criteria unavailable/module gate и A-35-15 system speaker/once-report pause продолжают быть A: пользователь сейчас подтвердил только radius/error. Script не изображается Ириной; конкретная недоступность/замена и physical module behavior требуют согласования существующих crew/module states.
Distribution ошибки и stable sampled estimate/no reroll — прежние A, не новые D.
Tests NOT RUN: FallbackRadius1920WorldUnits; FallbackDoesNotMultiplyIrina264; SingleErrorWithinTwo; SquadErrorWithinFour; EstimateIntegerOnlyAndNoHiddenClasses; FallbackDoesNotDistortEta; SwitchingOperator_DoesNotChangeActualLoadout.
## D-35-32: общая точка и лучший действующий сенсор эскадры
Пользователь: «Да, принимаем одну общую точку эскадры и лучший действующий сенсор без сложения радиусов».
Одна authoritative group position — центр map token. Её используют world range, bearing, ETA по D-35-29 и capture<=20км по D-35-19; tactical позиции отдельных carrier не становятся отдельными map contacts. Все расчёты в world units, zoom/DPI/window не меняют distance.
Squad sensor range=max operational radius её carriers, НЕ сумма. Два исправных Логова=>200км, один неработающий sensor=>200км от другого; оба неработающие=>0. Нулевая дальность означает отсутствие detection coverage, а не detection при coincident point. Не суммировать площадь/радиусы и не создавать дополнительный sensor origin для tactical member positions.
G-35-12 geometry/reference/sensor aggregation CLOSED. A-report-shape: один отчёт группы с двумя точными типами и одним approximate aggregate total; два automatic окна по членам недопустимы вследствие D-35-21 one/group. UI layout списка членов — implementation choice.
G-35-04 simultaneous batching CLOSED D-35-33: одна сводка всех новых групп текущего observation step; сортировка/selection/scroll — A.
Tests NOT RUN: TwoLairs_Max200NotSum400; OneSensorDisabled_UsesOther; AllSensorsDisabled_NoCoverage; GroupReference_SharedByRangeBearingEtaCapture; MemberTacticalPositions_NotMapContacts; CaptureAndSensorIndependentOfZoom.
## D-35-40…42: overrides, оператор и состоявшийся перехват
D-35-40 пользователь: «Да, настройки действуют до закрытия приложения, между запусками не сохраняются».
Host хранит explicit overrides только в app process memory: fog немедленно и после NewGame/Load; chance только следующий NewGame. Console main menu read/set без session допустим. Restart=>fogtrue/chance10; saves/profiles не содержат overrides. Failed/stale active application не выдаётся за effective success. Options применяются до первого engine tick/snapshot. G-35-13 lifetime и G-35-17 CLOSED.
D-35-41 пользователь: «Да, принимаем по наличию в экипаже и назначению, без смен и усталости».
Irina activecrew+sensor assignment=>264км,±1/±2; absent/unassigned=>script192км,±2/±4. Broken/disabled sensor=>0 coverage независимо от operator/script. No shifts/fatigue/injury schema. Reassignment восстанавливает бонус, не сбрасывает consumed groups; новые контакты расширенного радиуса идут через standard batch/autopause. G-35-06 CLOSED.
D-35-42 пользователь: «Да, принимаем: если перехват состоялся, после доклада и паузы начинается неизбежный бой».
При реальном pirate sensor contact и distance<=20км authoritative atomic capture фиксирует encounter. Player detection alone не capture. Для новой группы report+pause по batch policy; dismissal сохраняет Speed0; explicitcontinue начинает бой. Уже captured нельзя отменить сменой курса на паузе. Previously reported group не создаёт повторный automatic modal; используется существующий battle transition. Save/Load сохраняет pending capture, не даёт free escape. G-35-09 normal single-group inside-capture CLOSED; остаток: simultaneous capture CLOSED D-35-45: select earliest, no merge; recovery CLOSED D-35-47.
Tests NOT RUN: MainMenuOverrides_NoSession; OverridesSurviveNewGameLoadUntilExit; Restart_NormalDefaults; Save_NoOverrides; Chance_ExistingGroupsUnchanged; IrinaAbsentUnassigned_Fallback; AssignedIrina_Bonus; BrokenSensor_NoDetection; Reassignment_NoDuplicateReport; NewlyRevealedGroup_NormalBatch; CaptureThenReport_StaysPaused; ExplicitContinue_StartsUnavoidableBattle; PausedCourse_CannotUndoCapture; LoadPendingCapture_NoEscape; PlayerDetectionOnly_NoCapture.
Placement: all human station spawn400км A-35-17, START hard800км D-35-43/A-35-19; конкретные patrol routes ещё балансируются.

## Implementation assumptions and scope 2026-11-17
G-35-02M technical baseline: passive 1-cell sensor, mass 100 kg, 150 structure points, powerConsumptionW 0 (no new power-network mechanic). Player 2400 and pirate 2000 world units. Install at free hull cell (3,4); Scanner remains at (4,3). These physical values are implementation assumptions, not accepted balance. Actual main Demo references shared starter-carrier.json. If EP34 is integrated, its separate demo manifest must also receive the sensor before acceptance. Use tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0035Us0007Tk0003Tests.cs instead of a Client test: test the actual loader/manifest without any UI or game executable.

## Execution 2026-11-17
Passive category, player/pirate modules and non-overlapping starter installation implemented. Release Engine.Tests build, scoped whitespace and diff checks PASS. Headless content tests compiled, NOT RUN until US0007 completion. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-execution/sensor-content-build.log and sensor-content-format.log. Pirate world-group installation is verified by its owning group story, not claimed here.
