---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0008-ship-engineer
ticket: EP-0036-US-0008-TK-0003-crew-projection
title: Проекция реального инженера без второго состояния
stage: approved
approval_kind: workflow
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0036-US-0008-TK-0002-identity-and-skill]
files_touched: 2
serves: [AC-01, AC-03, AC-04]
created: 2026-11-17T19:18:11Z
revision: 1
implementation_status: not-started
---

# Проекция реального инженера без второго состояния

## Why

Как игрок, я вижу отдельного инженера в стартовом экипаже и его личный навык. Engine применяет этот навык к демонтажу и сбору металлолома, а сохранение/загрузка не меняют бонус. Этот тикет даёт результат: проекция реального инженера без второго состояния.

## Decisions

Пользователь: «Да, отдельный член экипажа с назначенной должностью инженера»; «Да, с базовым шансом 25%; инженер повышает его до 35–40%». На вариант личного бонуса +10…15 процентных пунктов, определяемого при создании персонажа: «1 и можешь задавать мне сразу несколько вопросов?».

Дословные ответы и время записи 2026-11-17T19:18:11Z (не время отправки); [полный контекст](../EP-0036-US-0008-ship-engineer.md). Пользовательские решения не подменяются техническими assumptions.

## Assumptions

A-08-01: целые значения 10,11,12,13,14,15 равновероятны; отдельный named seed по shipId/crewId не расходует боевой RNG. A-08-02: назначение в демо задаёт роль Engineer у одного стартового персонажа; отдельное меню назначения, найм инженеров и прокачка не входят. При нескольких Engineer данные отклоняются при загрузке вместо суммирования. A-08-03: имя Alex Morgan, CrewId CHR-PLAYER-ENGINEER и уже существующий portrait asset — рабочий content baseline, не художественное согласование.

Ниже новые API — проект, а не утверждение текущей реализации. Выполнить prerequisites до реализации; при drift сначала поправить Board allowlist. Не расширять её молча.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs` | `src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs:98 BuildCrewProjection, :133 occupancy` | Передать runtime skill в immutable CrewMemberSnapshot; существующий count Crew не дублировать. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0036Us0008Tk0003Tests.cs` | Новый файл | Projection/cabin/upkeep boundary tests. |

Разрешены ровно 2 implementation files, включая тестовый. Read-only входы: [story](../EP-0036-US-0008-ship-engineer.md), [epic](../../Documentation.md), prerequisite tickets в frontmatter. Это не разрешение менять другие production layers.

## Public API after the change

No new commands. BuildCrewProjection передаёт SalvageBonusPercentagePoints из runtime; GetAssignedEngineerBonusFraction из предыдущего тикета — единый источник для US3/US6.

## Implementation steps

1. Добавить передачу навыка в crew snapshot; имя/portrait/role остаются из того же CrewMemberRuntime.
2. Проверить через public carrier/session projection совпадение identity и бонуса.
3. На реальном engineer crew проверить увеличение занятости/PeopleFed и обычное employment без отдельного специального списания. При отсутствии инженера snapshot не выдумывает персонажа.

## Served acceptance criteria

- AC-01: Demo New Game содержит отдельного Engineer с уникальным CrewId; его каюта, питание и жалованье учитываются как у обычного члена экипажа.
- AC-03: Engine предоставляет один authoritative getter бонуса для лута; UI не вычисляет дополнительный независимый навык и не складывает нескольких инженеров.
- AC-04: Корабельный список экипажа показывает инженера и бонус в RU/EN; Load сохраняет личность/навык. Старые saves не получают нового члена экипажа автоматически.

## Out of scope

Другие stories/layers, game launch, изменения требований, commit/push. Не вводить независимые копии денег/кают/навыков в Client. Пытки, вербовка и сдача пленных не входят в демо; установка трофейных модулей также вне текущего демо.

## Invariants

- Engine владеет изменением мира и RNG; Client принимает подтверждённые snapshots (EngineRequirements: архитектура/команды).
- Stable identity и revision: повторная команда и Load не создают повторный расход/человека/бонус.
- Crew identity не заменяется именем или портретом; отсутствие optional state не создаёт сущность.
- Никакой новой реализации нельзя объявлять по наличию этого тикета; acceptance остаётся OPEN до evidence.

## Tests

Будущие именованные проверки, NOT RUN:
- `DemoEp0036Us0008Tk0003Tests.Projection_MatchesAuthoritativeEngineer`
- `DemoEp0036Us0008Tk0003Tests.Engineer_CountsOnceForCabinAndFood`
- `DemoEp0036Us0008Tk0003Tests.Engineer_UsesNormalEmployment`
- `DemoEp0036Us0008Tk0003Tests.NoEngineer_NoFabricatedCrew`

Из корня репозитория; абсолютный run создаётся вне репозитория. Пример команды запуска всего небольшого named corpus этого тикета с process-tree cap 300000 ms:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0008-TK-0003-crew-projection/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Path $run -Force | Out-Null
$testArgs = @('test', 'tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj', '-c', 'Release', '--filter', 'FullyQualifiedName~DemoEp0036Us0008Tk0003Tests', '--results-directory', $run, '--logger', 'trx')
$p = Start-Process dotnet -ArgumentList $testArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput "$run/test.stdout.log" -RedirectStandardError "$run/test.stderr.log"
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree cap reached' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build`; использовать тот же process-tree cap и перенаправление логов. Для C# слоя `dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs`; для content-only format не запускать. При длительном corpus делить по точным test IDs выше, не увеличивать cap.

## Definition of Done

- Все steps выполнены только в allowlist; указанные AC покрыты named tests.
- Focused tests и owning build проходят; исходные failures указаны явно и не скрыты.
- DTO/identity/defaults/save-load и rejection paths проверены; нет неоговорённых side effects.
- Документированы фактические проверки, API и remaining gates; native/product не закрываются автоматическими тестами.
- Нет незаписанных assumptions и скрытых изменений вне этого layer. Planning остаётся отдельным от implementation acceptance.

## Self-containment check

Входы, planned API, allowlist, AC, порядок и named проверки перечислены. Внешние prerequisite результаты должны существовать до исполнения. Если реализация требует иных файлов или продуктового решения, сначала уточнить этот Board тикет, а не расширять diff.
