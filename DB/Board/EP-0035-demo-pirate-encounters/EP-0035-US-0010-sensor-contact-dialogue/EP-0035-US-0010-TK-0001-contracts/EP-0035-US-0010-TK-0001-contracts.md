---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0010-sensor-contact-dialogue
ticket: EP-0035-US-0010-TK-0001-contracts
title: Контракт сенсорного доклада
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: contracts
matching_test_project: tests/DeepSpaceSaga.Contracts.Tests
depends_on: [EP-0035-US-0001-TK-0001-contracts, EP-0035-US-0009-TK-0001-contracts, EP-0035-US-0011-TK-0001-contracts]
files_touched: 3
serves: [AC-01, AC-02, AC-04]
created: 2026-11-17T11:37:31Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Контракт сенсорного доклада

## Why
Результат [истории](../EP-0035-US-0010-sensor-contact-dialogue.md) в пределах одного слоя. Тикет нарезан по поручению пользователя параллельно груммингу.

## Decisions
D-35-05…08 и дословный запрос в [эпике](../../Documentation.md#оператор-и-доклад-2026-11-17). Принят патруль по точкам и состав доклада; неподтверждённые детали не являются решениями пользователя.

## Assumptions
A-35-04/05 и G-35-03…06 перечислены в истории. Продуктовые G закрыты решениями D-35-47…50; повторное продуктовое согласование не требуется. Перед исполнением проверить technical source/allowlist drift и prerequisites, включая Demo manifest после EP-0034; acceptance остаётся OPEN.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Contracts/SensorContactReportContracts.cs | Новый файл/расширение owning prerequisite, создаётся в этой истории. | Только шаги ниже. |
| src/DeepSpaceSaga.Contracts/DemoCampaign.cs | Существующий источник; grounding в истории. | Только шаги ниже. |
| tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0035Us0010Tk0001Tests.cs | Новый matching test file этого тикета. | Только шаги ниже. |

## Public API after the change
Проект SensorContactReport(reportId,contactEpoch,groupId,operatorCrewId,observedMotionTimeMs,bearingDegrees,rangeWorldUnits,etaEstimate,etaUnavailableReason,droneEstimateMin,droneEstimateMax,estimateConfidence). Все значения — наблюдение, не скрытый loadout.

## Implementation steps
1. Ввести immutable typed report с stable identity, observation time и явными единицами.
2. ETA nullable с reason; approximate drone estimate — одно целое «около N», не UI-интервал. D-35-28/31: Ирина±1 single/±2 squad, script±2/±4; точные типы носителей, без скрытого состава/классов дронов. Batch DTO содержит все новые группы одного simulation step (D-35-33).
3. Не связывать creation report с debug visibility; отдельный факт sensor acquisition.

## Served acceptance criteria
AC-01, AC-02, AC-04 — вклад слоя contracts; полный пользовательский путь после integration.

## Out of scope
Новый generic quest/dialogue engine, новый roster, скрытая выдача экипажа, произвольный баланс зарплат/кают, новый боевой движок, скрытое знание AI, автоатака, публикация. Новые product decisions не принимать за пользователя.

## Invariants
EngineRequirements: Engine owns observed state/commands/save; 1 world unit=100 м, motion и calendar time различны. Renderer не создаёт события. Fog=false не означает sensor acquisition. Crew identity едина с CarrierOperations/CrewMemberSnapshot; assignment не удваивает occupancy/payroll. Save/load не повторяет report и не reroll оценки.

## Tests
Будущие тесты (NOT RUN):
- DemoEp0035Us0010Tk0001Tests.Report_RoundtripAndUnits
- DemoEp0035Us0010Tk0001Tests.UnknownEtaAndDroneEstimate_AreExplicit
- DemoEp0035Us0010Tk0001Tests.DebugVisibility_HasNoAcquisitionReport

Запуск из DSS: создать уникальный абсолютный <run> в D:/DeepSpaceSaga/artifacts/validation/ep0035-sensor-report/.
Ограничить каждый test process tree 300000 ms; timeout завершает дерево, не считается PASS.
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj -c Release --filter "FullyQualifiedName~DemoEp0035Us0010Tk0001Tests" --results-directory <run>/tests --logger trx
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --artifacts-path <run>/build
dotnet format src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --include src/DeepSpaceSaga.Contracts/SensorContactReportContracts.cs src/DeepSpaceSaga.Contracts/DemoCampaign.cs tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0035Us0010Tk0001Tests.cs
Для JSON — loader/schema tests вместо format. Native/product/dialogue UX проверяются отдельно.

## Definition of Done
- Закрытые продуктовые G не требуют повторного approval; перед исполнением проверены technical prerequisites и allowlists, stage не заменяет acceptance.
- Steps выполнены в allowlist; served AC покрыты named tests и integrated evidence.
- Build/focused tests/format проходят либо записаны конкретные исходные failures.
- Инварианты, units, stable identity, observer knowledge и UI pending соблюдены.
- Не добавлен фиктивный speaker/второй roster, открытые продуктовые решения не замаскированы assumptions.
- US-0006 обновляет фактический контракт после всех функциональных историй.

## Self-containment check
Prerequisites, owning files, steps и named tests перечислены; grounding в истории. G явно блокируют соответствующую часть, поэтому документ не выдаётся за execution-ready. При drift сначала обновить Board.


## Уточнение сигнатур и скоростей 2026-11-17

Добавить signatureTypeId/shipTypeNameKey к report; список классов дронов и точный private loadout отсутствуют. Fixed initial totals profiles 6/10 не означают публичное раскрытие hidden state.

## D-35-21: один автоматический доклад на группу за кампанию
Пользователь: «Да, один автоматический доклад на группу за кампанию».
Сохранить consumedAutomaticReport по campaignId/stable groupId. Только первое реальное sensor acquisition создаёт modal и atomic Speed0; dismissal сохраняет pause. Recontact, повтор snapshot, Save/Load, debug fog reveal не открывают modal и не меняют speed. Эскадра — одна группа, не два независимых автоматических доклада. Flag и report creation сохраняются атомарно, не терять pending report при Load.
G-35-04 CLOSED для once/group и batching D-35-33; только точный UX-order/manual reopen — A. Немодальное уведомление при recontact тоже не обязательное D.
Tests NOT RUN: FirstGroupContact_PausesOnce; Recontact_NoModalNoPause; SaveLoad_ConsumedPersists; TwoSquadMembers_OneGroupReport; FogReveal_NoDiscovery.
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
## D-35-32: общая точка и лучший действующий сенсор эскадры
Пользователь: «Да, принимаем одну общую точку эскадры и лучший действующий сенсор без сложения радиусов».
Одна authoritative group position — центр map token. Её используют world range, bearing, ETA по D-35-29 и capture<=20км по D-35-19; tactical позиции отдельных carrier не становятся отдельными map contacts. Все расчёты в world units, zoom/DPI/window не меняют distance.
Squad sensor range=max operational radius её carriers, НЕ сумма. Два исправных Логова=>200км, один неработающий sensor=>200км от другого; оба неработающие=>0. Нулевая дальность означает отсутствие detection coverage, а не detection при coincident point. Не суммировать площадь/радиусы и не создавать дополнительный sensor origin для tactical member positions.
G-35-12 geometry/reference/sensor aggregation CLOSED. A-report-shape: один отчёт группы с двумя точными типами и одним approximate aggregate total; два automatic окна по членам недопустимы вследствие D-35-21 one/group. UI layout списка членов — implementation choice.
G-35-04 simultaneous batching CLOSED D-35-33: одна сводка всех новых групп текущего observation step; сортировка/selection/scroll — A.
Tests NOT RUN: TwoLairs_Max200NotSum400; OneSensorDisabled_UsesOther; AllSensorsDisabled_NoCoverage; GroupReference_SharedByRangeBearingEtaCapture; MemberTacticalPositions_NotMapContacts; CaptureAndSensorIndependentOfZoom.
## D-35-33: одна сводка новых групп за один шаг
Пользователь: «Да, одна сводка для всех новых групп за один шаг симуляции».
В одном authoritative observation/simulation step собрать ВСЕ первые реальные sensor acquisitions ещё не reported stable groups, создать один batch report и одну autopause. Не останавливать обработку шага после первой группы; не продвигать дополнительные ticks ради ожидания новых контактов. Squad2xЛогово — одна группа. Debug visibility и уже consumed groups не входят в batch.
Persist batchId/content/pending presentation и consumed flags всех включённых groupIds атомарно: Save/Load не теряет записи и не создаёт повторный modal. Pending batch после Load доступен ровно один раз; flags не должны молча поглотить ещё не показанный доклад. Закрытие сводки сохраняет Speed0; новые контакты другого шага после explicit resume образуют новую сводку только для новых groups.
G-35-04 simultaneous batching CLOSED. Детерминированный порядок обязателен для replay; A-order: ETA затем range затем stable groupId; конкретный сортировочный UX/selection/scroll не является отдельным D. Manual reopening/recontact notice остаются необязательными A/отложенными, не повод повторять automatic modal.
US0010 contracts: collection group reports+batch identity; telemetry: collect/commit once after full current observation step; dialogue: один batch speaker/report и stay-paused; content/client: список новых групп без раскрытия hidden composition. Owning save хранит pending batch целиком.
Tests NOT RUN: TwoNewGroupsOneStep_OneSummaryOnePause; SquadIsOneGroup; ConsumedGroupExcluded; DifferentResumedSteps_SeparateNewSummaries; PendingBatchLoad_NoLossNoDuplicate; ClosingBatch_StaysPaused; NoExtraTicksForBatch; FogReveal_NoBatch.
## D-35-40…42: overrides, оператор и состоявшийся перехват
D-35-40 пользователь: «Да, настройки действуют до закрытия приложения, между запусками не сохраняются».
Host хранит explicit overrides только в app process memory: fog немедленно и после NewGame/Load; chance только следующий NewGame. Console main menu read/set без session допустим. Restart=>fogtrue/chance10; saves/profiles не содержат overrides. Failed/stale active application не выдаётся за effective success. Options применяются до первого engine tick/snapshot. G-35-13 lifetime и G-35-17 CLOSED.
D-35-41 пользователь: «Да, принимаем по наличию в экипаже и назначению, без смен и усталости».
Irina activecrew+sensor assignment=>264км,±1/±2; absent/unassigned=>script192км,±2/±4. Broken/disabled sensor=>0 coverage независимо от operator/script. No shifts/fatigue/injury schema. Reassignment восстанавливает бонус, не сбрасывает consumed groups; новые контакты расширенного радиуса идут через standard batch/autopause. G-35-06 CLOSED.
D-35-42 пользователь: «Да, принимаем: если перехват состоялся, после доклада и паузы начинается неизбежный бой».
При реальном pirate sensor contact и distance<=20км authoritative atomic capture фиксирует encounter. Player detection alone не capture. Для новой группы report+pause по batch policy; dismissal сохраняет Speed0; explicitcontinue начинает бой. Уже captured нельзя отменить сменой курса на паузе. Previously reported group не создаёт повторный automatic modal; используется существующий battle transition. Save/Load сохраняет pending capture, не даёт free escape. G-35-09 normal single-group inside-capture CLOSED; остаток: simultaneous capture CLOSED D-35-45: select earliest, no merge; recovery CLOSED D-35-47.
Tests NOT RUN: MainMenuOverrides_NoSession; OverridesSurviveNewGameLoadUntilExit; Restart_NormalDefaults; Save_NoOverrides; Chance_ExistingGroupsUnchanged; IrinaAbsentUnassigned_Fallback; AssignedIrina_Bonus; BrokenSensor_NoDetection; Reassignment_NoDuplicateReport; NewlyRevealedGroup_NormalBatch; CaptureThenReport_StaysPaused; ExplicitContinue_StartsUnavoidableBattle; PausedCourse_CannotUndoCapture; LoadPendingCapture_NoEscape; PlayerDetectionOnly_NoCapture.
Placement: all human station spawn400км A-35-17, START hard800км D-35-43/A-35-19; конкретные patrol routes ещё балансируются.
