---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0009-prisoner-custody
ticket: EP-0036-US-0009-TK-0002-arrest-state
title: Однократный арест из подтверждённого результата боя
stage: approved
approval_kind: workflow
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0036-US-0009-TK-0001-contracts, EP-0036-US-0003-TK-0003-engine, EP-0036-US-0007-TK-0003-postbattle-lifecycle]
files_touched: 5
serves: [AC-01, AC-02, AC-05]
created: 2026-11-17T19:18:11Z
revision: 1
implementation_status: not-started
---

# Однократный арест из подтверждённого результата боя

## Why

Как игрок, после победы я могу арестовать живого пилота выведенного из строя вражеского дрона. Он занимает одну общую каюту, остаётся отдельным пленником и потребляет пайки без жалованья; повторное действие или Load не создаёт второго пленного. Этот тикет даёт результат: однократный арест из подтверждённого результата боя.

## Decisions

Пользователь: «В демо только арест и содержание; взаимодействия отдельным будущим эпиком»; «Одна каюта на каждого пленного»; «Убираем постоянный резерв: экипаж, пассажиры и пленные используют общий фонд; пленный занимает одну свободную каюту»; «Да, по существующей норме на человека, без жалованья».

Дословные ответы и время записи 2026-11-17T19:18:11Z (не время отправки); [полный контекст](../EP-0036-US-0009-prisoner-custody.md). Пользовательские решения не подменяются техническими assumptions.

## Assumptions

A-09-01: пленник отдельная запись, не CrewMember и не контрактный Passenger. При нехватке пищи используется уже существующий MissingRations, без новой модели голода/смерти. A-09-02: принятие пассажирского обязательства, ещё не посаженного на борт, не позволяет украсть обещанную каюту при аресте; свободное место учитывает такое обязательство. Арест сам по себе не требует запаса еды как дополнительного неоговорённого условия. A-09-03: не вводятся побеги, лечение, освобождение, переводы, зарплата, продажа или награда за пленного.

Ниже новые API — проект, а не утверждение текущей реализации. Выполнить prerequisites до реализации; при drift сначала поправить Board allowlist. Не расширять её молча.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Prisoners.cs` | Новый файл | PrepareArrestPilot, immutable projection, GetPrisonerCount. |
| `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs` | `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs:8 campaign persistence/Validate` | Typed prisoner records, uniqueness/source validation. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs:104 dispatch, :94 projection` | Arrest dispatch и company projection. |
| `src/DeepSpaceSaga.Engine/Scenario/CarrierStateData.cs` | `src/DeepSpaceSaga.Engine/Scenario/CarrierStateData.cs:15 ReservedCabins default2` | Default0; legacy reserved field accepted as obsolete, no fake occupied people. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0009Tk0002Tests.cs` | Новый файл | Arrest identity/state/load/negative tests. |

Разрешены ровно 5 implementation files, включая тестовый. Read-only входы: [story](../EP-0036-US-0009-prisoner-custody.md), [epic](../../Documentation.md), prerequisite tickets в frontmatter. Это не разрешение менять другие production layers.

## Public API after the change

Planned private PrepareArrestPilot(ArrestPilotCommand command,ref DemoCampaignData candidate,ref string? reason,ref Action? commit); int GetPrisonerCount(string shipId); int GetAvailablePrisonerCabins(SpaceObjectRuntime ship, DemoCampaignData state), вычисляющий operational cabin count минус crew/passengers/prisoners/awaiting-boarding commitments, без прежнего reserved2. Этот helper использует TK3 для общей интеграции. BuildPrisonerCustodyProjection(DemoCampaignData state). Persist ImmutableArray<PrisonerSnapshot> Prisoners=[] in DemoCampaignData; capture status derives from this single ledger. US7 source record supplies frozen pilot identity/name/portrait/alive; no BattleLootData schema edits in this ticket; source uniqueness (BattleId,DroneId,PilotId).

## Implementation steps

1. Дождаться US7 authoritative open loot session с source drone/pilot record. Проверить победу, enemy Disabled, живого пилота, player carrier alive, session not closed, revision/identity.
2. Через GetAvailablePrisonerCabins проверить доступную общую каюту с учётом уже взятых пассажирских обязательств; не использовать прежний fixed reserve2 как реальных людей. NoFreeCabin не меняет captured flag и не уничтожает пилота.
3. Атомарно добавить одну prisoner record; статус source captured выводится из этого ledger по source identity, без второго сохраняемого флага в BattleLootData; reused command возвращает прежний receipt, новая команда для уже арестованного не создаёт дубль.
4. Persist/Validate полный состав и identity до публикации. При старом save без prisoners пусто; ReservedCabins=2 не создаёт пленников и не оставляет постоянный резерв.
5. Не добавлять пленного в crew/employment/passenger delivery; после закрытия loot session арест недоступен.

## Served acceptance criteria

- AC-01: На открытой postbattle loot session арест живого пилота Disabled enemy drone разрешён при одной доступной общей каюте. Destroyed/no pilot/wrong side/closed battle не допускаются.
- AC-02: Каждый пленный сохраняется с source battle/drone/pilot identity и занимает ровно1 каюту; постоянный резерв2 устранён. Пассажирские обязательства и экипаж не вытесняются.
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
- `DemoEp0036Us0009Tk0002Tests.DisabledEnemyPilot_ArrestedOnce`
- `DemoEp0036Us0009Tk0002Tests.NoFreeCabin_NoMutation`
- `DemoEp0036Us0009Tk0002Tests.PassengerCommitment_CannotBeStolen`
- `DemoEp0036Us0009Tk0002Tests.DestroyedOrFriendlyPilot_Rejected`
- `DemoEp0036Us0009Tk0002Tests.RepeatAndLoad_NoDuplicate`
- `DemoEp0036Us0009Tk0002Tests.OldReservedSave_NoPhantomPrisoners`
- `DemoEp0036Us0009Tk0002Tests.ClosedLoot_RejectsArrest`

Из корня репозитория; абсолютный run создаётся вне репозитория. Пример команды запуска всего небольшого named corpus этого тикета с process-tree cap 300000 ms:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0009-TK-0002-arrest-state/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Path $run -Force | Out-Null
$testArgs = @('test', 'tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj', '-c', 'Release', '--filter', 'FullyQualifiedName~DemoEp0036Us0009Tk0002Tests', '--results-directory', $run, '--logger', 'trx')
$p = Start-Process dotnet -ArgumentList $testArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput "$run/test.stdout.log" -RedirectStandardError "$run/test.stderr.log"
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree cap reached' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build`; использовать тот же process-tree cap и перенаправление логов. Для C# слоя `dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.Prisoners.cs src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs src/DeepSpaceSaga.Engine/Scenario/CarrierStateData.cs`; для content-only format не запускать. При длительном corpus делить по точным test IDs выше, не увеличивать cap.

## Definition of Done

- Все steps выполнены только в allowlist; указанные AC покрыты named tests.
- Focused tests и owning build проходят; исходные failures указаны явно и не скрыты.
- DTO/identity/defaults/save-load и rejection paths проверены; нет неоговорённых side effects.
- Документированы фактические проверки, API и remaining gates; native/product не закрываются автоматическими тестами.
- Нет незаписанных assumptions и скрытых изменений вне этого layer. Planning остаётся отдельным от implementation acceptance.

## Self-containment check

Входы, planned API, allowlist, AC, порядок и named проверки перечислены. Внешние prerequisite результаты должны существовать до исполнения. Если реализация требует иных файлов или продуктового решения, сначала уточнить этот Board тикет, а не расширять diff.

## Scope correction
Include SimulationEngine.CampaignRoundtrip.cs (actual company projection owner), SimulationEngine.BattleLoot.cs (derive capture state from prisoner ledger and namespace catalog pilot aliases by battle/drone), SimulationEngine.BattleLootLifecycle.cs (validate frozen pilot against outcome). This prevents duplicate capture flags and forged pilot identity on Load.

## Execution
Implemented atomic arrest with actual free cabins minus boarded people and unboarded commitments; no food prerequisite. Frozen pilot identity namespaced by battle/drone, prisoner provenance validated against retained victory. UI capture flag derives solely from custody ledger. Old absent custody remains empty; legacy reserved field default0. Engine/test-project build PASS (D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us9-tk2-build.log); runtime tests deferred to complete US9 boundary.
