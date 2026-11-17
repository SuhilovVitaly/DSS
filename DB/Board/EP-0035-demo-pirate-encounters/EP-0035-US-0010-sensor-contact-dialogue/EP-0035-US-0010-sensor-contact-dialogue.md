---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0010-sensor-contact-dialogue
title: Диалоговый доклад о пиратском контакте
stage: draft
approval_kind: workflow
implementation_status: implemented
priority: P0
dependencies: [EP-0035-US-0001-visible-threats, EP-0035-US-0009-sensor-operator, EP-0035-US-0011-pirate-signatures, EP-0035-US-0012-pirate-squadrons]
created: 2026-11-17T11:37:31Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Original planning baseline; current execution and validation: [ExecutionStatus](../ExecutionStatus.md).

# Диалоговый доклад о пиратском контакте

## Связь с техническим заданием
Новый запрос о реальном операторе и диалоговом докладе при обнаружении пиратов, записан 2026-11-17T11:37:31Z. Дословный запрос — в [эпике](../Documentation.md#оператор-и-доклад-2026-11-17). Нарезка поручена подзадаче; production не запрошен.

## User story
Как игрок, при настоящем обнаружении пиратского носителя сенсором я получаю диалоговый доклад оператора. Доклад показывает пеленг, дальность, время подлёта и приблизительную оценку числа дронов. Открытая debug-карта сама по себе не вызывает доклад и не раскрывает скрытый состав.

## Зависимости
EP-0035-US-0001-visible-threats, EP-0035-US-0009-sensor-operator

## Completion evidence
- AC-01: Реальный sensor acquisition вызывает доклад от назначенного живого члена команды; fog visibility не вызывает.
- AC-02: Есть сообщение, пеленг, дальность, ETA/явная причина недоступности и approximate drone estimate/unknown по согласованным G-35-03/05.
- AC-03: Диалог использует existing flow, RU/EN, согласованные pause/queue/repeat правила G-35-04; snapshot/Load не создают копии или reroll.
- AC-04: Имя/портрет принадлежат реальному crew; нет hidden loadout/future orders; observed time и единицы указаны.

## Acceptance criteria
AC-01…04 выше обязательны. Draft сохраняется до применимых G; automated не подменяет native/product.

## Decisions, assumptions and gaps
D-35-05: для демо принят патруль по точкам: «Ответ - пока по точкам для демо устроит». Это не одобрение поиска у последней позиции, автоматического возврата, выбора других целей или бесконечной погони.
D-35-06: при обнаружении пиратов сенсором нужно диалоговое окно с докладом оператора — реального члена экипажа на сенсоре.
D-35-07: доклад содержит сообщение, пеленг, дальность, время подлёта и приблизительную оценку числа дронов пиратского носителя.
D-35-08: понятную нарезку можно выполнять в подзадачах параллельно архитектурному диалогу; авторизация только Board grooming.
D-35-11: назначить существующую Irina Volskaya (CHR-PLAYER-DEFENSE) оператором сенсоров. Пользователь подтвердил имя/назначение. Существующие экипаж, каюты и payroll не дублируются; совмещение/замена остаются G-35-06.
A-35-05: связь sensor moduleId ↔ crewId хранится как явное назначение кампании; role Officer не переименовывается молча в SensorOperator.
G-35-03 CLOSED D-35-29: earliest entry<=20км по observed current vectors; отображение real duration at1×=4*motion seconds, отсутствие пересечения=>нет прогноза.
D-35-09: обнаружение автоматически ставит Speed0; закрытие доклада оставляет паузу, курс задаётся на паузе, движение после явного resume. G-35-04 остаётся только для очередности нескольких контактов; повторный автоматический доклад запрещён D-35-21.
G-35-05 starter bounds CLOSED D-35-28/31: Irina±1/±2,script±2/±4; один integer estimate, точный тип, hidden classes запрещены. Прогрессия вне demo scope.
G-35-06 CLOSED D-35-41: crew+assignment, без смен/усталости; broken module0.
G-35-01 (остаток): конкретные координаты/dwell и target selection ещё обсуждаются. Waypoint patrol, скорости D-35-15/16 и lost-contact/return D-35-17 приняты.

## Grounding
- src/DeepSpaceSaga.Client/Data/Ships/starter-carrier.json:130 — crew содержит капитана, офицера Irina Volskaya / CHR-PLAYER-DEFENSE и четыре пилота; отдельный новый человек пока не нужен.
- src/DeepSpaceSaga.Client/Scenarios/Demo/scenario.json:6 — текущий Demo ссылается на общий starter-carrier.json; после merge EP-0034 сверить фактический manifest.
- src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:362 — ShipCrewMemberData хранит identity/name/portrait/role, assignment sensor отсутствует.
- src/DeepSpaceSaga.Contracts/CarrierOperations.cs:64 — CrewMemberSnapshot, единая crew identity.
- src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs:132 — занятые каюты считаются по реальному crew/passengers.
- src/DeepSpaceSaga.Engine/SimulationEngine.CrewUpkeep.cs:16 — employment создаётся для реального ship.Crew; :40 — food по crew/passengers.
- src/DeepSpaceSaga.Contracts/DialogueState.cs:5 — DialogueState с ParticipantId, speaker, portrait, Parameters.
- src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs:68 — StartDialogue; :77 выбирает StationCrew, :83–84 задаёт speaker, :85–88 ставит modal pause. Для crew speaker нужен явный bounded extension.
- src/DeepSpaceSaga.Engine/Dialogue/DialogueRuntime.cs:25 — DialogueSaveState сохраняет ActiveDialogue и ResumeSpeed.
- src/DeepSpaceSaga.Client/UI/Screens/Dialogue/DialogueScreen.cs:11 — snapshot-only dialogue screen.
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:2183 — ActiveDialogue открывает диалог; SkiaWindow.cs:1164–1168 уже маршрутизирует экран.
- src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipViewModel.cs:28 — CrewRow показывает Id/name/role, assignment пока отсутствует.

## Ограничения и gaps
ETA и starter accuracy согласованы D-35-28/29/31; batching также согласован D-35-33; критерии замены оператора остаются открытыми. Автопауза и сохранение Speed0 после закрытия уже приняты (D-35-09). Тикеты materialized, не execution-ready. Нет разрешения добавить нового оплачиваемого человека без проверки имеющегося состава.

## Approved ticket map
Workflow карта draft-тикетов:
| Ticket | Результат | Prerequisites |
|---|---|---|
| [EP-0035-US-0010-TK-0001-contracts](EP-0035-US-0010-TK-0001-contracts/EP-0035-US-0010-TK-0001-contracts.md) | Контракт сенсорного доклада | EP-0035-US-0001-TK-0001-contracts, EP-0035-US-0009-TK-0001-contracts |
| [EP-0035-US-0010-TK-0002-telemetry-engine](EP-0035-US-0010-TK-0002-telemetry-engine/EP-0035-US-0010-TK-0002-telemetry-engine.md) | Авторитетный доклад при сенсорном обнаружении | EP-0035-US-0010-TK-0001-contracts, EP-0035-US-0009-TK-0002-engine, EP-0035-US-0001-TK-0003-engine |
| [EP-0035-US-0010-TK-0003-dialogue-engine](EP-0035-US-0010-TK-0003-dialogue-engine/EP-0035-US-0010-TK-0003-dialogue-engine.md) | Диалоговый доклад реального члена экипажа | EP-0035-US-0010-TK-0002-telemetry-engine |
| [EP-0035-US-0010-TK-0004-dialogue-content](EP-0035-US-0010-TK-0004-dialogue-content/EP-0035-US-0010-TK-0004-dialogue-content.md) | Текст доклада и локализация | EP-0035-US-0010-TK-0001-contracts |
| [EP-0035-US-0010-TK-0005-dialogue-client](EP-0035-US-0010-TK-0005-dialogue-client/EP-0035-US-0010-TK-0005-dialogue-client.md) | Окно доклада оператора сенсора | EP-0035-US-0010-TK-0003-dialogue-engine, EP-0035-US-0010-TK-0004-dialogue-content, EP-0035-US-0009-TK-0004-crew-presentation |

## Review log
2026-11-17T11:37:31Z — пользователь разрешил нарезку подзадачами, принял patrol по точкам и описал обязательные поля доклада. D отдельно от A/G; human approval технических деталей не заявляется. US-0006 должна выполняться последней. Implementation/tests/native/product NOT STARTED / OPEN.


## Уточнение оператора и доклада 2026-11-17

D-35-09 (новое решение пользователя): sensor acquisition автоматически ставит симуляцию на паузу. После закрытия доклада игра остаётся на Speed0; игрок оценивает ситуацию и задаёт новый курс, движение только после явного resume.
Дословно: «Да, делаем автопаузу при обнаружении. После закрытия диалога игра остается на паузе, чтобы можно было оценить ситуацию и изменить курс. Добавляй это в тикеты.»
G-35-04 CLOSED: D-35-21 запрещает повторный automatic report; D-35-33 объединяет новые контакты одного шага в одну сводку; D-35-09 оставляет игру на паузе после закрытия.
Grounding: Dialogue/SimulationEngine.Dialogue.cs:227–229 сейчас восстанавливает ResumeSpeed; SimulationEngine.cs:177 позволяет изменить отложенный resume во время диалога. Для sensor report нужен явный stay-paused policy, не наследование обычного resume.
AC-05: обнаружение/доклад атомарно ставят Speed0; dismissal и Load сохраняют паузу, курс можно задать без движения, resume только явный.

## Уточнение сигнатур и скоростей 2026-11-17

D-35-12/13: источник оценки total — сигнатура Логово/Танкред; в докладе показать ship type и approximate total, никогда light/heavy list. Fixed actual totals 6/10; random только классы. Функция Sensor skill→accuracy остаётся G-35-05.

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
