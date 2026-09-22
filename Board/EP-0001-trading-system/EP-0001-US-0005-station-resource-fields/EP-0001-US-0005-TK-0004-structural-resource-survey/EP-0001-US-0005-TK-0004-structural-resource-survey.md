---
epic: EP-0001-trading-system
story: EP-0001-US-0005-station-resource-fields
ticket: EP-0001-US-0005-TK-0004-structural-resource-survey
title: Сканирование и сохранённые знания
stage: approved
layer: engine
depends_on: [EP-0001-US-0005-TK-0002-seeded-resource-fields, EP-0001-US-0005-TK-0003-resource-field-content]
files_touched: 4
serves: [AC-04, AC-05, AC-07]
created: 2026-09-21T09:56:58Z
revision: 1
---

# Сканирование и сохранённые знания

## Why

Постоянные ресурсные объекты должны раскрывать состав только после успешного StructuralScan. Каталог команды сам по себе не выполняет сканирование: TryStartCommand в SimulationEngine.cs:1830–1843 направляет остальные команды в engine handler. Нужен ограниченный authoritative lifecycle с сохранением знаний и незавершённой попытки.

## Decisions

Отдельных пользовательских решений нет. Исходный запрос записан в story; это необходимая часть её требования о раскрытии состава после сканирования.

## Assumptions

- Только scanner.structuralScan и generated permanent resource fields из manifest TK-0002. Они уже IsKnown=true; GeneralScan для них не требуется. Остальные scanner-команды и объекты — backlog.
- Range120km, DurationGameTimeMs60000, SuccessChancePercent85 берутся из saved Rules.StructuralScan. Одна попытка на модуль и на target одновременно; повтор после RNG failure допустим, после успеха запрещён.
- Scanner работает по календарю. Его jobs живут в StationResourceFieldsState.Surveys, а не в ActiveCycleData двигателя, чтобы не менять physical timeline существующих команд.
- В имеющейся модели нет обязательного списания отдельного энергетического запаса для этой команды; новую energy subsystem не вводить.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | BuildSnapshot advances world then applies commands:420–421; module projection:606; RestoreCommandJournal:358; dispatch:1830; AdvanceMotionTo:2742; PredictMotion:3032 | Подключить scanner handler, projection, staged restore и проверки движения; сохранять все существующие ветви команд |
| src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs | AdvanceWorldTo:12–49 связывает calendar boundaries с physical MotionAt; отдельные _processedWorldTimeMs/_processedSimulationTimeMs:8–9 | Включить scanner due boundary и interval validation в календарный цикл без изменения rates времени |
| src/DeepSpaceSaga.Engine/SimulationEngine.ResourceSurvey.cs | Новый partial; state/DTO/RNG helper предоставляет TK-0002 | Вся scanner eligibility, jobs, range interval checks, terminal outcome и knowledge projection |
| tests/DeepSpaceSaga.Engine.Tests/ResourceSurveyTests.cs | Новый файл; snapshot/command/save test hooks доступны в Engine test assembly | Headless lifecycle, точные clocks/RNG, save replay и regressions движения |

Matching test project: `D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj`.

## Public API after the change

No new public API. Использовать additive Survey DTO и ResourceSurveyReasonCodes из TK-0001; PlayerCommand(CommandId,ClientSequence,ObjectId,ModuleId,CommandType,TargetObjectId) из Contracts/PlayerCommand.cs:6. Команда scanner.structuralScan адресуется существующему установленному scanner module.

State из TK-0002: ResourceSurveyJobData(CommandId,ObjectId,ModuleId,TargetObjectId,StartedGameTimeMs,DueGameTimeMs,LastValidatedSimulationTimeMs). Первые два времени — calendar, последнее — physical. Сохранённый job не содержит generic ActiveCycle.

Новые internal/private методы разместить только в новом partial: start/validate/complete survey, validate restored jobs, project Survey/module busy, restore pending command IDs. Точные private names свободны; внешний контракт фиксирован ниже.

## Implementation steps

1. Добавить branch StructuralScan до engine fallback. Validate actor=PlayerShipObjectId, actor/target существуют и не destroyed, module принадлежит actor, exposes command и PowerState On/OperationalState Ready/StructurePoints>0. Invalid actor/module/command используют UnknownObject/UnknownModule/UnknownCommandType из Contracts/CommandResult.cs:80–86; missing/unknown target — MissingTarget/UnknownTarget; unsupported generated target — ResourceSurveyReasonCodes.UnsupportedTarget; unknown target — TargetNotIdentified; known composition — AlreadyKnown; distance>range — OutOfRange; occupied module или target — Rejected с существующим reason Busy (не outcome Deferred, иначе existing queue запустит попытку позже без нового клика). Сравнивать расстояние в km (world units /10), equality at120km допускается. Любой reject не создаёт job, не меняет знания и не делает success draw.
2. Принятая команда создаёт job с start=_processedWorldTimeMs после AdvanceWorldTo, due=checked(start+DurationGameTimeMs), LastValidatedSimulationTimeMs=current physical time. Overflow отклонять до изменений с Rejected/ResourceSurveyReasonCodes.InvalidTime. Outcome Started; не писать Executed при старте. ActiveCommandType в InstalledModuleSnapshot для такого модуля — scanner.structuralScan, generic engine cycles остаются независимы. Pending same CommandId идемпотентен.
3. Добавить ближайший DueGameTimeMs в IncludeBoundary календарного scheduler. До завершения каждой попытки проверять target/module и весь пройденный физический интервал. После каждого authoritative advance обновлять LastValidatedSimulationTimeMs. При выходе из range — немедленно Failed/OutOfRange без RNG; target исчез/уничтожен — Failed/TargetLost; module/actor перестал быть доступен — Cancelled/ModuleUnavailable. Jobs удаляются с одним terminal result; inactive module не оставляет вечный busy.
4. Не ограничиваться проверками начала/конца большого advance: выход и возвращение до следующего snapshot тоже проваливают scan. В AdvanceMotionTo при наличии survey посетить существующие steering/cycle boundaries до их применения, сохранив порядок security/motion effects. Для неподвижного target проверка прямого участка использует максимум расстояния на концах. Для Approach разобрать три L/R/S участка сохранённого ApproachRoute (First/Second/Third, Type, ElapsedMs), плюс конечный прямой участок. Радиус r=SpeedKmS*10/(TurnRate*pi/180); центр дуги C=(x+r/sign*cos(heading), y+r/sign*sin(heading)); концы и радиальная точка дуги в направлении от target дают максимум расстояния, если её угол лежит в traversed arc. Полный оборот включает эту точку. Существующий ApproachLineCaptureMath.PredictPose(ApproachRoute,double) даёт точные позиции; не менять Motion API. При обнаруженном выходе найти первую границу range на проверенном монотонном участке бисекцией по physical time до1ms и пересчитать event timestamp тем же calendar/physical mapping. Tangency при distance==range не считается выходом. Не менять маршрут/скорость ради сканирования.
5. В due boundary сначала range/availability validation, затем ровно один draw. Stream name `ResourceSurvey:{ObjectId}:{ModuleId}`, seed=DeriveStreamSeed(masterSeed,name); ResourceFieldRandom из TK-0002, skip1000/counter10000 при первом создании, +10 за draw. floor(NextDouble()*100)<SuccessChancePercent. Success: CompositionKnown=true и Executed; failure: Failed/ScanFailed, знания false. Обновление stream/job/knowledge/result атомарно под existing engine lock; no auto-repeat. Busy/rejected/range failure не создают stream. Использовать RecordCommandResult с исходным CommandId/target/module; при восстановлении PlayerCommand ClientSequence=0, так как completion/result не используют sequence для порядка. Terminal timestamp — календарное время события.
6. Snapshot Survey: MassKg всегда; CompositionKnown=false скрывает CompositionType/Resources и actual ice image. CanStructuralScan вычисляется Engine по supported/identified/not-known/not-already-being-surveyed/range (доступность выбранного модуля проверяется отдельно). После успеха actual coarse composition и immutable fractions, CanStructuralScan=false; actual image разрешено раскрыть. Не проецировать manifest field kind/variant/profile IDs. Другие объекты имеют Survey=null.
7. LoadScenario до замены мира дополнительно валидирует сохранённые jobs: unique CommandId/(ObjectId,ModuleId)/TargetObjectId, существующий player/module с command, module без generic ActiveCycle и доступен; target generated/unknown/in-range; start<=saved calendar<due, LastValidatedSimulationTimeMs==saved physical cursor. Отрицательные/переполненные времена, conflict с terminal journal и pending command same ID отвергаются. Сначала staged validation, потом существующий world commit. После RestoreCommandJournal добавить active job IDs в _knownCommands под _commandGate, чтобы retry после load не запустил вторую попытку. CaptureSaveState сначала advances jobs, потом записывает materialized state; загрузка не расходует RNG и не продлевает срок.
8. Проверить real content TK-0003 end-to-end: New Game → field snapshot → scan → save → load → same revealed fraction/neutral-or-revealed image/stream counter. На save, сделанном внутри попытки, следующий outcome совпадает с непрерывным запуском при том же времени. Latest config не меняет saved scan rules. Пауза не двигает job и не расходует RNG.

## Out of scope

GeneralScan, NearbySignatures, сканирование legacy temporary астероидов/кораблей/станций, общий scanner framework, consumption/energy implementation, mining, GUI, motion equations/Approach planner, переработка всех ActiveCycleData, изменение time multipliers или market/production rules.

## Invariants

- StructuralScan: Documentation/01-Requirements/EngineRequirements.md:1404–1432; повтор известного состава запрещён:1574.
- Physical time не ускоряется календарём: SimulationEngine.EconomyTime.cs:16–20; существующая RuntimeMotion.At:10–24 выбирает Approach либо LinearMotionPredictor.
- Constant-speed route и точные дуги: src/DeepSpaceSaga.Motion/ApproachLineCaptureMath.cs:5–8,85–115; src/DeepSpaceSaga.Contracts/ApproachRoute.cs:8–16. Эти файлы — read-only reference, не Code context для правок.
- Command result journal: SimulationEngine.cs:1756; CommandResultStatus включает Failed: Contracts/CommandResult.cs:35.
- Проекция доступности не заменяет authoritative validation команды. No success draw на отказ/выход из range.

## Tests

ResourceSurveyTests:
- `Scan_requires_owned_ready_scanner_identified_supported_target_and_range` — AC-04; matrix rejected reasons, equality120km и just-outside, no state/RNG changes.
- `Busy_module_target_and_duplicate_command_do_not_start_second_job` — AC-04/05.
- `Scan_finishes_at_60000_calendar_ms_without_accelerating_motion` — AC-04/07; start/due-1/due, разный calendar multiplier, ship displacement/control world равны.
- `Seeded_scan_success_and_failure_use_one_saved_draw` — AC-04/05; фиксированные seeds с заранее вычисленным draw, probability0/100 fixtures и production85; retry failure разрешён, known target отклонён.
- `Leaving_range_fails_before_due_without_rng_even_after_reentry` — AC-04; прямой и curved Approach, coarse versus fine snapshot cadence одинаковы.
- `Range_tangency_and_arc_crossing_are_distinguished` — AC-04; endpoints inside with arc outside, exact tangent, first crossing timestamp.
- `Target_loss_and_module_unavailability_finish_once` — AC-04/05.
- `Save_mid_scan_resumes_same_deadline_outcome_counter_and_command_id` / `Invalid_saved_job_leaves_previous_world_unchanged` — AC-05; duplicate command after load, paused save, overdue/corrupt job.
- `Unknown_snapshot_never_leaks_resources_or_ice_image` / `Success_reveals_only_target_and_survives_reload` — AC-04/05.
- `Real_content_scan_changes_no_market_budget_credits_or_cargo` — AC-07; сравнение с контрольным world, продвинутым на те же времена, чтобы законные production/ration effects не принять за mutation сканера.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~ResourceSurveyTests|FullyQualifiedName~ApproachCommandTests|FullyQualifiedName~EconomyTime|FullyQualifiedName~CommandJournal"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps только в четырёх разрешённых файлах; отдельные jobs соблюдают calendar/physical границу.
- Каждый served criterion покрыт named tests, включая interval range check, replay и реальный контент.
- Tests/build/format проходят либо конкретные baseline failures явно записаны отдельно.
- API/invariants/out-of-scope соблюдены; нет скрытого generic scanner rewrite, нового RNG на restore или знания до success.
- Assumptions записаны, блокирующих вопросов нет; результат наблюдаем через snapshot, command result и save.

## Self-containment check

Контракт jobs/RNG/Survey находится в перечисленных dependencies; здесь заданы dispatch, eligibility/reasons, calendars, geometric interval validation, lifecycle, restore и projection. Read-only motion API и формула дуги приведены явно. Не требуется менять Contracts, Motion, LocalClient, content или runtime object schema сверх TK-0002.
