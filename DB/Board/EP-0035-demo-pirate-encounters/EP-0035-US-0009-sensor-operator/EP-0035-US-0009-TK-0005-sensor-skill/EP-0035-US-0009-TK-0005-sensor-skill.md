---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0009-sensor-operator
ticket: EP-0035-US-0009-TK-0005-sensor-skill
title: Навык оператора и прибавка к базовому радиусу
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0035-US-0009-TK-0001-contracts, EP-0035-US-0007-TK-0004-sensor-runtime, EP-0035-US-0009-TK-0002-engine]
files_touched: 5
serves: [AC-01, AC-03, AC-05]
created: 2026-11-17T11:37:31Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Навык оператора и прибавка к базовому радиусу

## Why
Реальный назначенный оператор повышает радиус сенсора собственным навыком, который сохраняется с crew identity.

## Decisions
Пользователь: «Оператору сенсора нужно дать определенный скилл. В примере единственного сценария демо этот скилл должен давать прибавку еще в 10% от базовой дистанции сенсора».
D-35-10: бонус стартового оператора +0.10 * base radius установленного модуля, ровно один раз; не бонус стороне игрока.
R_player_module=1.20*R_pirate_module, поэтому effective player=1.10*R_player_module=1.32*R_pirate_module при указанном стартовом операторе. Пиратские operator бонусы этим не заданы.

## Assumptions
A-skill: имя поля SensorRangeBonusPercent (целое percent), default 0; стартовое значение 10. Новая progression шкала/XP не вводится без отдельного решения.
A-missing: без действительного назначенного оператора бонус 0, базовый пассивный сенсор сохраняется. Это рабочее допущение до решения G-35-06.
Несколько модулей: сначала max operational base radius US-0007, затем один бонус назначенного оператора для обслуживаемого сенсора; не суммировать и не применять рекурсивно.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs | ShipCrewMemberData:362. | Optional SensorRangeBonusPercent с default 0, validation. |
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | BuildSaveCrewMember:1347, materialization:1995, CrewMemberRuntime:4094. | Прокинуть skill через runtime/load/save; без соседних правил. |
| src/DeepSpaceSaga.Engine/SimulationEngine.Sensors.cs | Создаётся US-0007-TK-0004. | Однократная формула effective range через assignment US-0009. |
| src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs | BuildCrewProjection:98. | Передать навык в существующий crew snapshot. |
| tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0035Us0009Tk0005Tests.cs | Новый файл. | Именованные проверки. |

## Public API after the change
Проект serialized crew sensorRangeBonusPercent; CrewMemberSnapshot optional field принадлежит TK-0001. Internal effectiveRadius = baseRadius * (1 + assignedSkillPercent/100.0); finite inputs, percent>=0. Точное upper bound — G, не вводить произвольно.
Проект accessor assignment из TK-0002 должен быть согласован: до его исполнения runtime skill тестировать fixture assignment, integration после обоих тикетов.

## Implementation steps
1. Прокинуть optional skill через ShipCrewMemberData, runtime, save и projection; legacy save default 0, не выдавать +10% каждому старому crew.
2. Для рабочего сенсора использовать skill только реально назначенного члена crew; отделить base/effective radius в snapshot TK-0001.
3. Применить +10% от base, без повторного повышения при snapshot/load/reassignment; не переносить skill с потерянного crew на нового.
4. На assigned missing/invalid использовать script по D-35-30/31:80% base (192км), погрешность±2 single/±4 squad; не придумывать человека. Доступность Ирины по crew+assignment (D-35-41), неисправный sensor radius0.
5. Проверить реальную цепочку New Game с content TK-0003 и save/load после assignment TK-0002.

## Out of scope
Пиратские навыки, XP/прокачка оператора, изменение экономики, hidden world-side buff, выдача навыка всем старым saves.

## Invariants
Engine владеет вычислением; snapshot содержит base/effective и skill contribution, не клиентский расчёт. Fog не меняет skill/contact. Один crewId, без второго roster. +20% принадлежит стартовым модулям, +10% — навыку назначенного оператора.

## Tests
Будущие tests:
- DemoEp0035Us0009Tk0005Tests.TenPercent_IsAddedToBaseExactlyOnce
- DemoEp0035Us0009Tk0005Tests.StartingPlayer_Is132PercentOfPirateBase
- DemoEp0035Us0009Tk0005Tests.SaveLoad_PreservesSkillAndAssignment
- DemoEp0035Us0009Tk0005Tests.LegacyCrew_DefaultsToZero
- DemoEp0035Us0009Tk0005Tests.MissingOperator_HasNoBonus
- DemoEp0035Us0009Tk0005Tests.RepeatedProjection_DoesNotCompoundBonus

Создать абсолютный <run> в D:/DeepSpaceSaga/artifacts/validation/ep0035-sensor-report/; каждый process tree <=300000 ms, остановить при timeout.
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter "FullyQualifiedName~DemoEp0035Us0009Tk0005Tests" --results-directory <run>/tests --logger trx
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <run>/build
dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.Sensors.cs src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0035Us0009Tk0005Tests.cs

## Definition of Done
- Skill сохраняется с crew, legacy compatible, +10% применяется один раз к base.
- AC-01/03/05 покрыты tests и реальным Demo start.
- G/A, связанные с отсутствующим оператором, явно разрешены до полной product acceptance.
- Только allowlist, focused checks, нет скрытой выдачи бонусов.

## Self-containment check
Source signatures, формула, boundaries, files/tests перечислены. Название поля — proposed, не отдельное human approval. Assignment integration согласована с TK-0002; перед implementation сверить актуальный Demo manifest.


## Уточнение оператора и доклада 2026-11-17

Точное prerequisite assignment accessor создаёт TK-0002; skill integration выполняется после него.

## D-35-20 — стартовые радиусы сенсоров

D-35-20 (пользователь): «Давай 200 км у пиратов, 240 у игрока и 264 с Ириной. Добавляй в тикеты. Теперь давай разберем повторные обнаружения: не хочется, чтобы диалог постоянно ставил игру на паузу».
Точные стартовые радиусы демо: оба пиратских сенсора 200 км = 2000 world units; базовый модуль игрока 240 км = 2400 world units; effective radius с назначенной Ириной 264 км = 2640 world units. Формула ровно 2400 + 0.10*2400, один раз. Пиратские демо-контакты используют 2000 world units, без неоговорённой надбавки.
Численная дальность G-35-02 закрыта. Отдельный G-35-02M — технические параметры установки модуля (масса, footprint, прочность, питание), которые надо записать перед content implementation; это не неизвестный радиус и не повторный вопрос о дальности.
Capture radius D-35-19 остаётся 20 км = 200 world units. Для sensor detection граница distance<=effective radius; для capture <=200. Показ debug-маркера вне радиуса не вызывает sensor acquisition.
Текущее уточнение: G-35-04 CLOSED D-35-21/33 — один automatic report на stable group за кампанию, все новые группы одного шага в одной сводке. Manual report вне утверждённого объёма демо.
Named tests: PirateSensor_2000WorldUnits; PlayerModule_2400WorldUnits; IrinaEffective_2640WorldUnitsExactlyOnce; AtEffectiveRadius_Detects_Outside_DoesNot; SaveLoad_PreservesBaseSkillAndEffectiveRange; FogVisibility_DoesNotChange2000_2400_2640. Проверить отсутствие compound bonus и различие base/effective.

## D-35-28: точность стартового доклада Ирины
Пользователь: «Да, принимаем ±1 дрон на носитель и ±2 для эскадры. Добавляй в тикеты».
Стартовый sensor skill Ирины даёт одно целое приблизительное reported total: ошибка относительно actual total не более1 для одиночного носителя, не более2 для demo squad2xЛогово. В докладе «около N», точный тип носителя / типы обоих Логов; light/heavy классы и equipment скрыты. Не показывать симметричный диапазон вокруг actual total, из которого восстанавливается скрытый true count.
Допустимые значения: одиночное Логово5..7 при actual6; Танкред9..11 при actual10; эскадра10..14 при actual12. Это границы допустимого одного числа, НЕ UI-диапазон и НЕ случайное actual число дронов.
G-35-05 CLOSED для starter Irina accuracy bounds и формата одного приблизительного числа. Открытый остаток: прогрессия навыка/другие уровни/другие операторы; distribution ошибки не утверждён пользователем. A-35-12: равномерная целочисленная ошибка в допустимых границах, независимый named estimate RNG stream; если принята при реализации, явно сохранить как assumption. Стабильная сохранённая оценка/no reroll — рекомендованная A, не новое пользовательское D.
+10% дальности от module base — отдельный эффект навыка; не трактовать как10% точности. Module радиусы200/240/264км и capture20км без изменений. Squad report один на группу, approximate aggregate total±2, не два самостоятельных forced reports.
G-35-03 CLOSED D-35-29: до20км по текущим observed vectors, real duration при1×; историческое обсуждение цели/часов superseded.
Tests NOT RUN: IrinaSingleEstimate_WithinOne_Integer; IrinaSquadEstimate_WithinTwo_Integer; Report_OneApproximateNumberAndExactTypes; Report_NoClassOrHiddenLoadout; AccuracyDoesNotAlterActual6_10_12; RangeBonusIsNotAccuracyPercent; StableEstimateOnLoad (A). Проверять разрешённый observation DTO, не переносить private composition в client.
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
## D-35-40…42: overrides, оператор и состоявшийся перехват
D-35-40 пользователь: «Да, настройки действуют до закрытия приложения, между запусками не сохраняются».
Host хранит explicit overrides только в app process memory: fog немедленно и после NewGame/Load; chance только следующий NewGame. Console main menu read/set без session допустим. Restart=>fogtrue/chance10; saves/profiles не содержат overrides. Failed/stale active application не выдаётся за effective success. Options применяются до первого engine tick/snapshot. G-35-13 lifetime и G-35-17 CLOSED.
D-35-41 пользователь: «Да, принимаем по наличию в экипаже и назначению, без смен и усталости».
Irina activecrew+sensor assignment=>264км,±1/±2; absent/unassigned=>script192км,±2/±4. Broken/disabled sensor=>0 coverage независимо от operator/script. No shifts/fatigue/injury schema. Reassignment восстанавливает бонус, не сбрасывает consumed groups; новые контакты расширенного радиуса идут через standard batch/autopause. G-35-06 CLOSED.
D-35-42 пользователь: «Да, принимаем: если перехват состоялся, после доклада и паузы начинается неизбежный бой».
При реальном pirate sensor contact и distance<=20км authoritative atomic capture фиксирует encounter. Player detection alone не capture. Для новой группы report+pause по batch policy; dismissal сохраняет Speed0; explicitcontinue начинает бой. Уже captured нельзя отменить сменой курса на паузе. Previously reported group не создаёт повторный automatic modal; используется существующий battle transition. Save/Load сохраняет pending capture, не даёт free escape. G-35-09 normal single-group inside-capture CLOSED; остаток: simultaneous capture CLOSED D-35-45: select earliest, no merge; recovery CLOSED D-35-47.
Tests NOT RUN: MainMenuOverrides_NoSession; OverridesSurviveNewGameLoadUntilExit; Restart_NormalDefaults; Save_NoOverrides; Chance_ExistingGroupsUnchanged; IrinaAbsentUnassigned_Fallback; AssignedIrina_Bonus; BrokenSensor_NoDetection; Reassignment_NoDuplicateReport; NewlyRevealedGroup_NormalBatch; CaptureThenReport_StaysPaused; ExplicitContinue_StartsUnavoidableBattle; PausedCourse_CannotUndoCapture; LoadPendingCapture_NoEscape; PlayerDetectionOnly_NoCapture.
Placement: all human station spawn400км A-35-17, START hard800км D-35-43/A-35-19; конкретные patrol routes ещё балансируются.