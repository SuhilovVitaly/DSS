---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0008-ship-engineer
ticket: EP-0036-US-0008-TK-0002-identity-and-skill
title: Создание и сохранение личного навыка инженера
stage: approved
approval_kind: workflow
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0036-US-0008-TK-0001-contracts]
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-11-17T19:18:11Z
revision: 1
implementation_status: implemented
---

# Создание и сохранение личного навыка инженера

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Why

Как игрок, я вижу отдельного инженера в стартовом экипаже и его личный навык. Engine применяет этот навык к демонтажу и сбору металлолома, а сохранение/загрузка не меняют бонус. Этот тикет даёт результат: создание и сохранение личного навыка инженера.

## Decisions

Пользователь: «Да, отдельный член экипажа с назначенной должностью инженера»; «Да, с базовым шансом 25%; инженер повышает его до 35–40%». На вариант личного бонуса +10…15 процентных пунктов, определяемого при создании персонажа: «1 и можешь задавать мне сразу несколько вопросов?».

Дословные ответы и время записи 2026-11-17T19:18:11Z (не время отправки); [полный контекст](../EP-0036-US-0008-ship-engineer.md). Пользовательские решения не подменяются техническими assumptions.

## Assumptions

A-08-01: целые значения 10,11,12,13,14,15 равновероятны; отдельный named seed по shipId/crewId не расходует боевой RNG. A-08-02: назначение в демо задаёт роль Engineer у одного стартового персонажа; отдельное меню назначения, найм инженеров и прокачка не входят. При нескольких Engineer данные отклоняются при загрузке вместо суммирования. A-08-03: имя Alex Morgan, CrewId CHR-PLAYER-ENGINEER и уже существующий portrait asset — рабочий content baseline, не художественное согласование.

Ниже новые API — проект, а не утверждение текущей реализации. Выполнить prerequisites до реализации; при drift сначала поправить Board allowlist. Не расширять её молча.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs` | `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:367 ShipCrewMemberData` | Trailing nullable salvageBonusPercentagePoints. |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs` | `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:639 проверка sensor skill` | Валидация engineer role/диапазона и уникальности роли. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.cs:1994 ResolveShipCrew, :1364 BuildSaveCrewMember, :4124 CrewMemberRuntime` | Передать resolvedMasterSeed в ResolveShipCrew, materialize и persist навык. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.ShipEngineer.cs` | Новый файл | Чистое разрешение personal bonus и authoritative getter. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0008Tk0002Tests.cs` | Новый файл | Seed/load/getter/invalid data tests. |

Разрешены ровно 5 implementation files, включая тестовый. Read-only входы: [story](../EP-0036-US-0008-ship-engineer.md), [epic](../../Documentation.md), prerequisite tickets в frontmatter. Это не разрешение менять другие production layers.

## Public API after the change

Планируемые internal/private: ResolveShipCrew(SpaceObjectData obj, ulong resolvedMasterSeed); int ResolveEngineerBonus(ulong seed,string shipId,ShipCrewMemberData member); double GetAssignedEngineerBonusFraction(). ShipCrewMemberData nullable int SalvageBonusPercentagePoints; CrewMemberRuntime int SalvageBonusPercentagePoints=0.

## Implementation steps

1. Разрешать ненулевой skill только Engineer, заданный диапазон10…15; отсутствие роли/старые crew даёт0. Запретить нескольких Engineer одного корабля.
2. Для Engineer с отсутствующим полем создать10…15 из независимого seed с ключом shipId/crewId; передавать seed текущей загружаемой сессии, не старое MasterSeed.
3. Сохранить материализованный skill через BuildSaveCrewMember; повтор Load не reroll. Существующий Engineer с explicit значением сохраняет его.
4. Getter ищет живой player ship и его Crew с ролью Engineer: нет корабля/он destroyed/нет инженера→0; иначе skill/100.0. Не читать UI, не добавлять скрыто второго инженера, не выдавать предметы.

## Served acceptance criteria

- AC-01: Demo New Game содержит отдельного Engineer с уникальным CrewId; его каюта, питание и жалованье учитываются как у обычного члена экипажа.
- AC-02: Личный целочисленный бонус 10…15 п.п. определяется один раз при создании и сохраняется. Без действующего инженера бонус0; со своим инженером10…15 п.п.
- AC-03: Engine предоставляет один authoritative getter бонуса для лута; UI не вычисляет дополнительный независимый навык и не складывает нескольких инженеров.

## Out of scope

Другие stories/layers, game launch, изменения требований, commit/push. Не вводить независимые копии денег/кают/навыков в Client. Пытки, вербовка и сдача пленных не входят в демо; установка трофейных модулей также вне текущего демо.

## Invariants

- Engine владеет изменением мира и RNG; Client принимает подтверждённые snapshots (EngineRequirements: архитектура/команды).
- Stable identity и revision: повторная команда и Load не создают повторный расход/человека/бонус.
- Crew identity не заменяется именем или портретом; отсутствие optional state не создаёт сущность.
- Никакой новой реализации нельзя объявлять по наличию этого тикета; acceptance остаётся OPEN до evidence.

## Tests

Будущие именованные проверки, NOT RUN:
- `DemoEp0036Us0008Tk0002Tests.NewEngineer_GeneratesWithinTenToFifteen`
- `DemoEp0036Us0008Tk0002Tests.SaveLoad_PreservesBonusAndIdentity`
- `DemoEp0036Us0008Tk0002Tests.MissingEngineer_ReturnsZero`
- `DemoEp0036Us0008Tk0002Tests.DestroyedCarrier_ReturnsZero`
- `DemoEp0036Us0008Tk0002Tests.InvalidOrMultipleEngineers_Rejected`
- `DemoEp0036Us0008Tk0002Tests.SkillStream_DoesNotChangeBattleRng`

Из корня репозитория; абсолютный run создаётся вне репозитория. Пример команды запуска всего небольшого named corpus этого тикета с process-tree cap 300000 ms:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0008-TK-0002-identity-and-skill/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Path $run -Force | Out-Null
$testArgs = @('test', 'tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj', '-c', 'Release', '--filter', 'FullyQualifiedName~DemoEp0036Us0008Tk0002Tests', '--results-directory', $run, '--logger', 'trx')
$p = Start-Process dotnet -ArgumentList $testArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput "$run/test.stdout.log" -RedirectStandardError "$run/test.stderr.log"
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree cap reached' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build`; использовать тот же process-tree cap и перенаправление логов. Для C# слоя `dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.ShipEngineer.cs`; для content-only format не запускать. При длительном corpus делить по точным test IDs выше, не увеличивать cap.

## Definition of Done

- Все steps выполнены только в allowlist; указанные AC покрыты named tests.
- Focused tests и owning build проходят; исходные failures указаны явно и не скрыты.
- DTO/identity/defaults/save-load и rejection paths проверены; нет неоговорённых side effects.
- Документированы фактические проверки, API и remaining gates; native/product не закрываются автоматическими тестами.
- Нет незаписанных assumptions и скрытых изменений вне этого layer. Planning остаётся отдельным от implementation acceptance.

## Self-containment check

Входы, planned API, allowlist, AC, порядок и named проверки перечислены. Внешние prerequisite результаты должны существовать до исполнения. Если реализация требует иных файлов или продуктового решения, сначала уточнить этот Board тикет, а не расширять diff.

## Execution — 2026-11-17

Persisted nullable salvage skill, deterministic independent engineer-salvage/v1/ship/crew seed, direct Load and JSON validation, one-engineer limit, zero without live carrier. Saved explicit bonuses remain unchanged. Engine build/scoped formatting/diff PASS. Named tests prepared, NOT RUN until US8 completion. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us8-tk2-build.log.

Independent review correction: engineer role detection now uses the existing shared ParseCrewRole, so numeric/whitespace aliases cannot bypass skill range or one-engineer validation while appearing as Engineer in UI. Added RoleAliases_UseSameValidationAndGetter regression. Repeat affected Engine checks after publication.
