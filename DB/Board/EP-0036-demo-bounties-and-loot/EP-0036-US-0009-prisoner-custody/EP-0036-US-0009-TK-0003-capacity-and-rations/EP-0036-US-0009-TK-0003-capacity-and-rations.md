---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0009-prisoner-custody
ticket: EP-0036-US-0009-TK-0003-capacity-and-rations
title: Общие каюты и питание без жалованья
stage: approved
approval_kind: workflow
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0036-US-0009-TK-0002-arrest-state]
files_touched: 5
serves: [AC-02, AC-03, AC-05]
created: 2026-11-17T19:18:11Z
revision: 1
implementation_status: implemented
---

# Общие каюты и питание без жалованья

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Why

Как игрок, после победы я могу арестовать живого пилота выведенного из строя вражеского дрона. Он занимает одну общую каюту, остаётся отдельным пленником и потребляет пайки без жалованья; повторное действие или Load не создаёт второго пленного. Этот тикет даёт результат: общие каюты и питание без жалованья.

## Decisions

Пользователь: «В демо только арест и содержание; взаимодействия отдельным будущим эпиком»; «Одна каюта на каждого пленного»; «Убираем постоянный резерв: экипаж, пассажиры и пленные используют общий фонд; пленный занимает одну свободную каюту»; «Да, по существующей норме на человека, без жалованья».

Дословные ответы и время записи 2026-11-17T19:18:11Z (не время отправки); [полный контекст](../EP-0036-US-0009-prisoner-custody.md). Пользовательские решения не подменяются техническими assumptions.

## Assumptions

A-09-01: пленник отдельная запись, не CrewMember и не контрактный Passenger. При нехватке пищи используется уже существующий MissingRations, без новой модели голода/смерти. A-09-02: принятие пассажирского обязательства, ещё не посаженного на борт, не позволяет украсть обещанную каюту при аресте; свободное место учитывает такое обязательство. Арест сам по себе не требует запаса еды как дополнительного неоговорённого условия. A-09-03: не вводятся побеги, лечение, освобождение, переводы, зарплата, продажа или награда за пленного.

Ниже новые API — проект, а не утверждение текущей реализации. Выполнить prerequisites до реализации; при drift сначала поправить Board allowlist. Не расширять её молча.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs:70 writer, :96 constant2, :133 occupancy` | Удалить постоянный резерв, real prisoner counts; writer reserved0. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.PassengerCapacity.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.PassengerCapacity.cs:51–67 crew/passenger/reserved/free and ration feasibility` | PrisonerCabins и общий occupied/free, учёт пищи. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:8 MealIntervalMs, :141 ConsumeScheduledRations` | Добавить prisoners к needed; неизменное расписание. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CrewUpkeep.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.CrewUpkeep.cs:16 Employment, :40 PeopleFed, :53 wages` | PeopleFed включает prisoners; wages остаются только live employed Crew. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0009Tk0003Tests.cs` | Новый файл | Capacity/ration/wage/clock tests. |

Разрешены ровно 5 implementation files, включая тестовый. Read-only входы: [story](../EP-0036-US-0009-prisoner-custody.md), [epic](../../Documentation.md), prerequisite tickets в frontmatter. Это не разрешение менять другие production layers.

## Public API after the change

No new public command. Total used=crew+passengers+prisoners; ReservedCabins=0; FreeCabins=max(0,total-used), deficit retains existing overcommitted reason. Existing GetPrisonerCount owns count.

## Implementation steps

1. Убрать ReservedPrisonerCabins=2 и связанные вычитания; producer и carrier writer выдаёт reserved0.
2. Считать реальные prisoner records в обеих проекциях одним helper; занятость не дублируется с PassengerCabins/CrewCabins.
3. В расписании питания каждые12 игровых часов добавлять1 ration на каждого пленного; existing missing ration fallback сохраняется.
4. CrewUpkeep.PeopleFed включает пленных, Employment/DailyWage никогда не создаются для них.
5. Проверить найм/boarding на capacity вместе с инженером/пассажирами/пленными, pause/time slicing/save-load и terminal ship destroyed.

## Served acceptance criteria

- AC-02: Каждый пленный сохраняется с source battle/drone/pilot identity и занимает ровно1 каюту; постоянный резерв2 устранён. Пассажирские обязательства и экипаж не вытесняются.
- AC-03: Пленный питается по существующему расписанию/норме одного человека, без employment и жалованья. Пауза/дробление времени/SaveLoad не удваивают расход.
- AC-05: Repeat command/повторный арест/Load не дублирует человека; неподтверждённый отказ не меняет людей, каюты и деньги.

## Out of scope

Другие stories/layers, game launch, изменения требований, commit/push. Не вводить независимые копии денег/кают/навыков в Client. Пытки, вербовка и сдача пленных не входят в демо; установка трофейных модулей также вне текущего демо.

## Invariants

- Engine владеет изменением мира и RNG; Client принимает подтверждённые snapshots (EngineRequirements: архитектура/команды).
- Stable identity и revision: повторная команда и Load не создают повторный расход/человека/бонус.
- Crew identity не заменяется именем или портретом; отсутствие optional state не создаёт сущность.
- Никакой новой реализации нельзя объявлять по наличию этого тикета; acceptance остаётся OPEN до evidence.

## Tests

Будущие именованные проверки, NOT RUN:
- `DemoEp0036Us0009Tk0003Tests.AllPeople_ShareCabinsWithoutReserve`
- `DemoEp0036Us0009Tk0003Tests.Arrest_ReducesFreeCabinsByOne`
- `DemoEp0036Us0009Tk0003Tests.HiringAndBoarding_RespectPrisoners`
- `DemoEp0036Us0009Tk0003Tests.Prisoner_ConsumesNormalRation`
- `DemoEp0036Us0009Tk0003Tests.Prisoner_NeverAccruesWages`
- `DemoEp0036Us0009Tk0003Tests.PauseAndSplitTick_NoDoubleMeal`
- `DemoEp0036Us0009Tk0003Tests.SaveLoad_PreservesCount`

Из корня репозитория; абсолютный run создаётся вне репозитория. Пример команды запуска всего небольшого named corpus этого тикета с process-tree cap 300000 ms:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0009-TK-0003-capacity-and-rations/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Path $run -Force | Out-Null
$testArgs = @('test', 'tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj', '-c', 'Release', '--filter', 'FullyQualifiedName~DemoEp0036Us0009Tk0003Tests', '--results-directory', $run, '--logger', 'trx')
$p = Start-Process dotnet -ArgumentList $testArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput "$run/test.stdout.log" -RedirectStandardError "$run/test.stderr.log"
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree cap reached' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build`; использовать тот же process-tree cap и перенаправление логов. Для C# слоя `dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs src/DeepSpaceSaga.Engine/SimulationEngine.PassengerCapacity.cs src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs src/DeepSpaceSaga.Engine/SimulationEngine.CrewUpkeep.cs`; для content-only format не запускать. При длительном corpus делить по точным test IDs выше, не увеличивать cap.

## Definition of Done

- Все steps выполнены только в allowlist; указанные AC покрыты named tests.
- Focused tests и owning build проходят; исходные failures указаны явно и не скрыты.
- DTO/identity/defaults/save-load и rejection paths проверены; нет неоговорённых side effects.
- Документированы фактические проверки, API и remaining gates; native/product не закрываются автоматическими тестами.
- Нет незаписанных assumptions и скрытых изменений вне этого layer. Planning остаётся отдельным от implementation acceptance.

## Self-containment check

Входы, planned API, allowlist, AC, порядок и named проверки перечислены. Внешние prerequisite результаты должны существовать до исполнения. Если реализация требует иных файлов или продуктового решения, сначала уточнить этот Board тикет, а не расширять diff.

## Scope correction
Include StationJobs.cs offer sizing (subtract actual prisoners), CrewRecruitment.cs (respect unboarded promises through the shared cabin helper), SimulationEngine.cs Load validation (custody must belong to the player carrier). These are necessary shared-capacity entry points beyond the original five-file estimate.

## Execution
Actual prisoners now consume shared cabins and scheduled rations; fixed reserve producer is zero. Crew payroll remains employment-only. Recruitment and passenger offer capacity respect custody; Load rejects prisoners assigned to a foreign ship. Engine/tests build PASS (D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us9-tk3-build.log); tests deferred to US9 boundary.

Boundary regression scope: update EP34 passenger-capacity fixture to seven real demo crew, zero reserve, five passenger cabins and twelve actual meal consumers; retain full-capacity, damaged, missing-food and legacy-save assertions. Fixture outputs now use this task's artifact run.

Boundary fixture correction: retain the explicit-time helper's returned save rather than recapturing with the unchanged test clock. The latter manufactured a gameTime older than LastTick. 32/33 passed before this fixture correction (us9-engine-repeat.log).
