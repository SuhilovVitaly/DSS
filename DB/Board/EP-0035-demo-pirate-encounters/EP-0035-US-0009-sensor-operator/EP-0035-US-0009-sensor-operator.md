---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0009-sensor-operator
title: Оператор сенсора из реального экипажа
stage: draft
approval_kind: workflow
implementation_status: not-started
priority: P0
dependencies: [EP-0035-US-0007-carrier-sensors, EP-0033-US-0004-pilot-recruitment]
created: 2026-11-17T11:37:31Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Оператор сенсора из реального экипажа

## Связь с техническим заданием
Новый запрос о реальном операторе и диалоговом докладе при обнаружении пиратов, записан 2026-11-17T11:37:31Z. Дословный запрос — в [эпике](../Documentation.md#оператор-и-доклад-2026-11-17). Нарезка поручена подзадаче; production не запрошен.

## User story
Как игрок, я вижу, кто из моей команды работает на сенсоре носителя. Оператор имеет то же имя, портрет и identity, что в экипаже, а его назначение сохраняется. Новый декоративный персонаж вместо настоящего члена команды не создаётся.

## Зависимости
EP-0035-US-0007-carrier-sensors, EP-0033-US-0004-pilot-recruitment

## Completion evidence
- AC-01: Demo start имеет явное назначение реального crewId на установленный sensor moduleId; UI показывает связь.
- AC-02: По D-35-11 переиспользуется существующая Ирина; crew count, occupancy, food/payroll не удваиваются.
- AC-03: Save/load сохраняет назначение, недействительные ссылки отклоняются; отсутствующий/погибший operator не выдаётся за живого speaker.
- AC-04: RU/EN и экран экипажа показывают assignment/availability по authoritative snapshot.

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
D-35-09: обнаружение автоматически ставит Speed0; закрытие доклада оставляет паузу, курс задаётся на паузе, движение после явного resume. G-35-04 остаётся только для очередности нескольких контактов и повторного обнаружения.
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
ETA, точность оценки, повтор/очередь и замена оператора требуют продолжения диалога с владельцем. Автопауза и сохранение Speed0 после закрытия уже приняты (D-35-09). Тикеты materialized, не execution-ready. Нет разрешения добавить нового оплачиваемого человека без проверки имеющегося состава.

## Approved ticket map
Workflow карта draft-тикетов:
| Ticket | Результат | Prerequisites |
|---|---|---|
| [EP-0035-US-0009-TK-0001-contracts](EP-0035-US-0009-TK-0001-contracts/EP-0035-US-0009-TK-0001-contracts.md) | Контракт назначения оператора сенсора | EP-0035-US-0007-TK-0001-contracts |
| [EP-0035-US-0009-TK-0002-engine](EP-0035-US-0009-TK-0002-engine/EP-0035-US-0009-TK-0002-engine.md) | Сохранение назначения реального оператора | EP-0035-US-0009-TK-0001-contracts, EP-0035-US-0007-TK-0004-sensor-runtime |
| [EP-0035-US-0009-TK-0003-start-content](EP-0035-US-0009-TK-0003-start-content/EP-0035-US-0009-TK-0003-start-content.md) | Оператор в стартовой кампании и тексты назначения | EP-0035-US-0009-TK-0002-engine, EP-0035-US-0007-TK-0003-sensor-content |
| [EP-0035-US-0009-TK-0004-crew-presentation](EP-0035-US-0009-TK-0004-crew-presentation/EP-0035-US-0009-TK-0004-crew-presentation.md) | Показ оператора сенсора в экипаже | EP-0035-US-0009-TK-0003-start-content |

## Review log
2026-11-17T11:37:31Z — пользователь разрешил нарезку подзадачами, принял patrol по точкам и описал обязательные поля доклада. D отдельно от A/G; human approval технических деталей не заявляется. US-0006 должна выполняться последней. Implementation/tests/native/product NOT STARTED / OPEN.


## Уточнение оператора и доклада 2026-11-17

D-35-10 (новое решение пользователя): «Оператору сенсора нужно дать определенный скилл. В примере единственного сценария демо этот скилл должен давать прибавку еще в 10% от базовой дистанции сенсора».
Проект SensorRangeBonusPercent=10 у стартового оператора; bonus=0.10*baseRadius установленного сенсора, один раз. R_player_module=1.20*R_pirate_module; effective player=1.10*R_player_module=1.32*R_pirate_module. Пиратские operator bonuses не установлены.
A-missing: без действительного назначенного оператора бонус 0, базовый сенсор работает; до решения G-35-06 это assumption. Legacy crew default skill 0.
AC-05: Demo operator имеет sensor skill +10% base; сохранение навыка/назначения и отсутствие compound bonus проверены.
Добавлен TK-0005-sensor-skill; новый порядок: TK-0001 → TK-0002 → TK-0005 → TK-0003 → TK-0004. Всего 5 тикетов.
[EP-0035-US-0009-TK-0005-sensor-skill](EP-0035-US-0009-TK-0005-sensor-skill/EP-0035-US-0009-TK-0005-sensor-skill.md).

## Уточнение сигнатур и скоростей 2026-11-17

D-35-11: пользователь прямо назначил Ирину — CHR-PLAYER-DEFENSE — оператором сенсоров. A-35-04 закрыт в этой части; нового человека не добавлять.

## D-35-20 — стартовые радиусы сенсоров

D-35-20 (пользователь): «Давай 200 км у пиратов, 240 у игрока и 264 с Ириной. Добавляй в тикеты. Теперь давай разберем повторные обнаружения: не хочется, чтобы диалог постоянно ставил игру на паузу».
Точные стартовые радиусы демо: оба пиратских сенсора 200 км = 2000 world units; базовый модуль игрока 240 км = 2400 world units; effective radius с назначенной Ириной 264 км = 2640 world units. Формула ровно 2400 + 0.10*2400, один раз. Пиратские демо-контакты используют 2000 world units, без неоговорённой надбавки.
Численная дальность G-35-02 закрыта. Отдельный G-35-02M — технические параметры установки модуля (масса, footprint, прочность, питание), которые надо записать перед content implementation; это не неизвестный радиус и не повторный вопрос о дальности.
Capture radius D-35-19 остаётся 20 км = 200 world units. Для sensor detection граница distance<=effective radius; для capture <=200. Показ debug-маркера вне радиуса не вызывает sensor acquisition.
Текущее уточнение: G-35-04 CLOSED D-35-21/33 — один automatic report на stable group за кампанию, все новые группы одного шага в одной сводке. Manual report вне утверждённого объёма демо.

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