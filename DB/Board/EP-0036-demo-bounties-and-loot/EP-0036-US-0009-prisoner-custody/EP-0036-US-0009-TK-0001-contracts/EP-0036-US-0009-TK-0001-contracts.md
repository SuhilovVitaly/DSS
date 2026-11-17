---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0009-prisoner-custody
ticket: EP-0036-US-0009-TK-0001-contracts
title: Команда ареста и проекция пленных
stage: approved
approval_kind: workflow
layer: contracts
matching_test_project: tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj
depends_on: [EP-0036-US-0003-TK-0001-contracts]
files_touched: 5
serves: [AC-01, AC-02, AC-04, AC-05]
created: 2026-11-17T19:18:11Z
revision: 1
implementation_status: not-started
---

# Команда ареста и проекция пленных

## Why

Как игрок, после победы я могу арестовать живого пилота выведенного из строя вражеского дрона. Он занимает одну общую каюту, остаётся отдельным пленником и потребляет пайки без жалованья; повторное действие или Load не создаёт второго пленного. Этот тикет даёт результат: команда ареста и проекция пленных.

## Decisions

Пользователь: «В демо только арест и содержание; взаимодействия отдельным будущим эпиком»; «Одна каюта на каждого пленного»; «Убираем постоянный резерв: экипаж, пассажиры и пленные используют общий фонд; пленный занимает одну свободную каюту»; «Да, по существующей норме на человека, без жалованья».

Дословные ответы и время записи 2026-11-17T19:18:11Z (не время отправки); [полный контекст](../EP-0036-US-0009-prisoner-custody.md). Пользовательские решения не подменяются техническими assumptions.

## Assumptions

A-09-01: пленник отдельная запись, не CrewMember и не контрактный Passenger. При нехватке пищи используется уже существующий MissingRations, без новой модели голода/смерти. A-09-02: принятие пассажирского обязательства, ещё не посаженного на борт, не позволяет украсть обещанную каюту при аресте; свободное место учитывает такое обязательство. Арест сам по себе не требует запаса еды как дополнительного неоговорённого условия. A-09-03: не вводятся побеги, лечение, освобождение, переводы, зарплата, продажа или награда за пленного.

Ниже новые API — проект, а не утверждение текущей реализации. Выполнить prerequisites до реализации; при drift сначала поправить Board allowlist. Не расширять её молча.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/PrisonerCustodyContracts.cs` | Новый файл | ArrestPilotCommand, PrisonerSnapshot, PrisonerCustodySnapshot, reasons. |
| `src/DeepSpaceSaga.Contracts/DemoCampaign.cs` | `src/DeepSpaceSaga.Contracts/DemoCampaign.cs:33 DemoCompanySnapshot` | Trailing PrisonerCustodySnapshot? Prisoners=null. |
| `src/DeepSpaceSaga.Contracts/PassengerCapacityContracts.cs` | `src/DeepSpaceSaga.Contracts/PassengerCapacityContracts.cs:11 PassengerCapacitySnapshot` | Trailing int PrisonerCabins=0, ReservedCabins retained compatibility. |
| `src/DeepSpaceSaga.Contracts/CarrierOperations.cs` | `src/DeepSpaceSaga.Contracts/CarrierOperations.cs:51 CabinReservationSnapshot` | Документировать occupied включает всех, reserved=0, prisoner real count. |
| `tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0036Us0009Tk0001Tests.cs` | Новый файл | Defaults/identity/roundtrip tests. |

Разрешены ровно 5 implementation files, включая тестовый. Read-only входы: [story](../EP-0036-US-0009-prisoner-custody.md), [epic](../../Documentation.md), prerequisite tickets в frontmatter. Это не разрешение менять другие production layers.

## Public API after the change

ArrestPilotCommand(string CommandId,long ExpectedRevision,string BattleId,string DroneId):DemoCampaignCommand, ILootCampaignCommand (marker US3-TK0001, BattleId matches current LootOpen session). PrisonerSnapshot(string Id,string ShipId,string SourceBattleId,string SourceDroneId,string SourcePilotId,string DisplayName,string? PortraitAssetId,long CapturedAt). PrisonerCustodySnapshot(ImmutableArray<PrisonerSnapshot> Prisoners). Reasons: prisoner_no_cabin, prisoner_not_capturable, prisoner_already_captured, loot_closed. DTO default без пленных = empty, не две выдуманные записи.

## Implementation steps

1. Добавить command identity/revision и устойчивую source identity; клиент не передаёт имя/живость/место.
2. Ввести typed company projection и отдельное число пленников в passenger capacity.
3. Сохранить backward defaults; ReservedCabins остаётся для совместимости, текущий producer выдаёт0.
4. Не добавлять команды пыток/вербовки/сдачи.

## Served acceptance criteria

- AC-01: На открытой postbattle loot session арест живого пилота Disabled enemy drone разрешён при одной доступной общей каюте. Destroyed/no pilot/wrong side/closed battle не допускаются.
- AC-02: Каждый пленный сохраняется с source battle/drone/pilot identity и занимает ровно1 каюту; постоянный резерв2 устранён. Пассажирские обязательства и экипаж не вытесняются.
- AC-04: Окно лута показывает арест/отказ, корабль показывает пленников и фактическую вместимость; отсутствуют сдача, пытки, вербовка и выплаты за пленного.
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
- `DemoEp0036Us0009Tk0001Tests.OldCompanyPayload_NoPrisoners`
- `DemoEp0036Us0009Tk0001Tests.PrisonerIdentity_RoundTrips`
- `DemoEp0036Us0009Tk0001Tests.CapacityDefaults_NoReservedPeople`
- `DemoEp0036Us0009Tk0001Tests.ArrestCommand_UsesCampaignIdentity`

Из корня репозитория; абсолютный run создаётся вне репозитория. Пример команды запуска всего небольшого named corpus этого тикета с process-tree cap 300000 ms:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0009-TK-0001-contracts/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Path $run -Force | Out-Null
$testArgs = @('test', 'tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj', '-c', 'Release', '--filter', 'FullyQualifiedName~DemoEp0036Us0009Tk0001Tests', '--results-directory', $run, '--logger', 'trx')
$p = Start-Process dotnet -ArgumentList $testArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput "$run/test.stdout.log" -RedirectStandardError "$run/test.stderr.log"
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree cap reached' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --artifacts-path <absolute-run>/build`; использовать тот же process-tree cap и перенаправление логов. Для C# слоя `dotnet format src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --include src/DeepSpaceSaga.Contracts/PrisonerCustodyContracts.cs src/DeepSpaceSaga.Contracts/DemoCampaign.cs src/DeepSpaceSaga.Contracts/PassengerCapacityContracts.cs src/DeepSpaceSaga.Contracts/CarrierOperations.cs`; для content-only format не запускать. При длительном corpus делить по точным test IDs выше, не увеличивать cap.

## Definition of Done

- Все steps выполнены только в allowlist; указанные AC покрыты named tests.
- Focused tests и owning build проходят; исходные failures указаны явно и не скрыты.
- DTO/identity/defaults/save-load и rejection paths проверены; нет неоговорённых side effects.
- Документированы фактические проверки, API и remaining gates; native/product не закрываются автоматическими тестами.
- Нет незаписанных assumptions и скрытых изменений вне этого layer. Planning остаётся отдельным от implementation acceptance.

## Self-containment check

Входы, planned API, allowlist, AC, порядок и named проверки перечислены. Внешние prerequisite результаты должны существовать до исполнения. Если реализация требует иных файлов или продуктового решения, сначала уточнить этот Board тикет, а не расширять diff.

## Execution
Implemented command with shared loot identity, optional company prisoners, prisoner cabin count and immutable identity projection. Gender is a trailing backward-compatible field required by TK6 portrait body selection. Contracts build PASS (D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us9-tk1-build.log); tests authored, deferred to US9 boundary.
