---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0007-carrier-sensors
title: Сенсор носителя и независимое обнаружение
stage: draft
approval_kind: workflow
implementation_status: not-started
priority: P0
dependencies: [EP-0032-US-0004-demo-campaign-start]
created: 2026-11-17T11:23:54Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Сенсор носителя и независимое обнаружение

## Связь с техническим заданием
Поручение 2026-11-17: сенсор, +20%, normal default-true fog + explicit debug-false override и консоль. Дословный запрос — в [эпике](../Documentation.md#grooming-2026-11-17). Материализация разрешена; implementation не запрошен.

## User story
Я обнаруживаю корабли сенсором носителя. Стартовый радиус на 20% больше радиуса обычных пиратов. Открытая карта не заменяет сенсорный контакт.

## Зависимости
EP-0032-US-0004-demo-campaign-start

## Completion evidence
- AC-01: Носитель оснащён сенсором; радиус из operational module, Scanner сохранён.
- AC-02: Demo New Game: R_player=1.20*R_pirate обоих составов; в промежуточной полосе обнаруживает только игрок.
- AC-03: Направленные контакты не зависят от fog, renderer, курса и RNG.
- AC-04: Граница радиуса, отказ сенсора, пауза и Load воспроизводимы; старым saves не выдаётся модуль.

## Acceptance criteria
AC-01…04 выше обязательны, headless не закрывает native/product.

## Decisions and assumptions
D-35-01 (пользователь): сенсор — установленный модуль носителя с радиусом.
D-35-02 (пользователь): стартовый R_player = 1.20 * R_pirate обоих обычных составов. Это стартовая комплектация, не вечный бонус игроку.
D-35-03 (пользователь): SUPERSEDED D-35-37: normal FogOfWarEnabled=true; explicit owner debug override=false через консоль. Открытая карта — визуализация, не обнаружение и не знание AI.
D-35-04: поручено добавить тикеты; продолжается грумминг, production implementation не запрошен.

A-35-01: пассивный всенаправленный сенсор, обнаружение при R>0 и distance<=R без RNG. Disabled/destroyed/отсутствующий модуль даёт R=0; несколько рабочих модулей дают max. Активное сканирование и новая модель энергопотребления вне scope.
A-35-02 SUPERSEDED D-35-37: normal defaulttrue, explicit debugfalse; lifetime override D-35-40.
A-35-03: fog фильтрует динамические пиратские маркеры, не известные станции/географию. Last-known маркеры пока вне scope.
D-35-05: патруль по точкам принят для демо. Остаток G-35-01: конкретные точки/стоянки и выбор других целей ещё обсуждаются. Потеря контакта/возврат приняты D-35-17; скорости приняты D-35-15/16.
G-35-02 [CLOSED D-35-20]: радиусы 200/240/264 км утверждены. G-35-02M отдельно: физические параметры установки модуля ещё нужно записать перед content implementation; это не неопределённая дальность.

## Grounding
- src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs:5 — каталог модулей; sensor radius пока отсутствует.
- src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:922 — ModuleImplementationDto.
- src/DeepSpaceSaga.Client/Data/Ships/starter-carrier.json:80 — установлен Scanner ресурсов; сохранить его.
- src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs:23 — DemoPirateGroup с SightRadius/PatrolSpeed; это схема, не подтверждение движения.
- src/DeepSpaceSaga.Engine/Scenario/StarterCarrierManifest.cs:13 — manifest установленных модулей.
- src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs:83 — projection и hooks sections.
- src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs:6 — CreateDefault; fog-команды нет.
- src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommand.cs:21 — IDebugCommand, async session context.
- EP-0030 Documentation.md:16 — поставленная консоль, её реализацию не дублировать.

## Ограничения и gaps
G-35-01 остаётся для следующего обсуждения. G-35-02 закрыт D-35-20; G-35-02M отдельно ограничивает готовность установки content, не дальность. После EP-0034 сверить фактический Demo manifest.

## Approved ticket map
Workflow карта; draft строки не являются готовыми к исполнению.
| Ticket | Результат | Prerequisites |
|---|---|---|
| [EP-0035-US-0007-TK-0001-contracts](EP-0035-US-0007-TK-0001-contracts/EP-0035-US-0007-TK-0001-contracts.md) | Контракт сенсорного радиуса и контактов | none |
| [EP-0035-US-0007-TK-0002-module-schema](EP-0035-US-0007-TK-0002-module-schema/EP-0035-US-0007-TK-0002-module-schema.md) | Радиус сенсора в каталоге модулей | EP-0035-US-0007-TK-0001-contracts |
| [EP-0035-US-0007-TK-0003-sensor-content](EP-0035-US-0007-TK-0003-sensor-content/EP-0035-US-0007-TK-0003-sensor-content.md) | Стартовые сенсоры с преимуществом 20 процентов | EP-0035-US-0007-TK-0002-module-schema |
| [EP-0035-US-0007-TK-0004-sensor-runtime](EP-0035-US-0007-TK-0004-sensor-runtime/EP-0035-US-0007-TK-0004-sensor-runtime.md) | Авторитетное независимое обнаружение | EP-0035-US-0007-TK-0001-contracts, EP-0035-US-0007-TK-0003-sensor-content |

## Review log
2026-11-17T11:23:54Z — материализация по прямому поручению «Добавляй нужные тикеты … мы продолжаем грумминг». D от пользователя, A/G отдельно. История draft до завершения грумминга. Implementation NOT STARTED; tests/native/product NOT RUN / OPEN. US-0006 выполняется последней.


## Уточнение оператора и доклада 2026-11-17

D-35-10 (новое решение пользователя): «Оператору сенсора нужно дать определенный скилл. В примере единственного сценария демо этот скилл должен давать прибавку еще в 10% от базовой дистанции сенсора».
Проект SensorRangeBonusPercent=10 у стартового оператора; bonus=0.10*baseRadius установленного сенсора, один раз. R_player_module=1.20*R_pirate_module; effective player=1.10*R_player_module=1.32*R_pirate_module. Пиратские operator bonuses не установлены.
A-missing: без действительного назначенного оператора бонус 0, базовый сенсор работает; до решения G-35-06 это assumption. Legacy crew default skill 0.
AC-02 относится к базовым стартовым модулям (+20%); effective radius после навыка оператора рассчитывает US-0009-TK-0005. Предыдущее raw-module правило расширено, не отменено.

## D-35-20 — стартовые радиусы сенсоров

D-35-20 (пользователь): «Давай 200 км у пиратов, 240 у игрока и 264 с Ириной. Добавляй в тикеты. Теперь давай разберем повторные обнаружения: не хочется, чтобы диалог постоянно ставил игру на паузу».
Точные стартовые радиусы демо: оба пиратских сенсора 200 км = 2000 world units; базовый модуль игрока 240 км = 2400 world units; effective radius с назначенной Ириной 264 км = 2640 world units. Формула ровно 2400 + 0.10*2400, один раз. Пиратские демо-контакты используют 2000 world units, без неоговорённой надбавки.
Численная дальность G-35-02 закрыта. Отдельный G-35-02M — технические параметры установки модуля (масса, footprint, прочность, питание), которые надо записать перед content implementation; это не неизвестный радиус и не повторный вопрос о дальности.
Capture radius D-35-19 остаётся 20 км = 200 world units. Для sensor detection граница distance<=effective radius; для capture <=200. Показ debug-маркера вне радиуса не вызывает sensor acquisition.
Текущее уточнение: G-35-04 CLOSED D-35-21/33 — один automatic report на stable group за кампанию, все новые группы одного шага в одной сводке. Manual report вне утверждённого объёма демо.

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