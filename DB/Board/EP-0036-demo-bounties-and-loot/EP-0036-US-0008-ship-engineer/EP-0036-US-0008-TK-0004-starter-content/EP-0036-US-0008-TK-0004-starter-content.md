---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0008-ship-engineer
ticket: EP-0036-US-0008-TK-0004-starter-content
title: Отдельный инженер в стартовом составе демо
stage: approved
approval_kind: workflow
layer: content-data
matching_test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0036-US-0008-TK-0002-identity-and-skill]
files_touched: 4
serves: [AC-01, AC-04]
created: 2026-11-17T19:18:11Z
revision: 1
implementation_status: implemented
---

# Отдельный инженер в стартовом составе демо

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Why

Как игрок, я вижу отдельного инженера в стартовом экипаже и его личный навык. Engine применяет этот навык к демонтажу и сбору металлолома, а сохранение/загрузка не меняют бонус. Этот тикет даёт результат: отдельный инженер в стартовом составе демо.

## Decisions

Пользователь: «Да, отдельный член экипажа с назначенной должностью инженера»; «Да, с базовым шансом 25%; инженер повышает его до 35–40%». На вариант личного бонуса +10…15 процентных пунктов, определяемого при создании персонажа: «1 и можешь задавать мне сразу несколько вопросов?».

Дословные ответы и время записи 2026-11-17T19:18:11Z (не время отправки); [полный контекст](../EP-0036-US-0008-ship-engineer.md). Пользовательские решения не подменяются техническими assumptions.

## Assumptions

A-08-01: целые значения 10,11,12,13,14,15 равновероятны; отдельный named seed по shipId/crewId не расходует боевой RNG. A-08-02: назначение в демо задаёт роль Engineer у одного стартового персонажа; отдельное меню назначения, найм инженеров и прокачка не входят. При нескольких Engineer данные отклоняются при загрузке вместо суммирования. A-08-03: имя Alex Morgan, CrewId CHR-PLAYER-ENGINEER и уже существующий portrait asset — рабочий content baseline, не художественное согласование.

Ниже новые API — проект, а не утверждение текущей реализации. Выполнить prerequisites до реализации; при drift сначала поправить Board allowlist. Не расширять её молча.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json` | `src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json:151 crew стартового носителя` | Добавить одного Engineer с уникальным id и существующим portrait; skill не задавать для seeded materialization. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Существующий словарь | Роль Engineer и подпись бонуса п.п. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Существующий словарь | Паритет ключей/параметров. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0008Tk0004Tests.cs` | Новый файл | Content/schema/start/locale tests. |

Разрешены ровно 4 implementation files, включая тестовый. Read-only входы: [story](../EP-0036-US-0008-ship-engineer.md), [epic](../../Documentation.md), prerequisite tickets в frontmatter. Это не разрешение менять другие production layers.

## Public API after the change

JSON crew нового персонажа role='Engineer', salvageBonusPercentagePoints omitted; localization Ship.Crew.Role.Engineer и Ship.Crew.SalvageBonus с параметром bonus.

## Implementation steps

1. Добавить одного отдельного персонажа, не превращать Ирину/пилота в инженера и не менять их навыки.
2. Использовать существующий валидный portraitAssetId (A-08-03), не генерировать изображения. Имя/ID проверить на уникальность.
3. Проверить фактический Demo New Game: capacity включает нового человека, rations не добавлять бесплатно через код. Старый Save не дополнять стартовым crew.
4. Добавить RU/EN подписи роли и личного бонуса.

## Served acceptance criteria

- AC-01: Demo New Game содержит отдельного Engineer с уникальным CrewId; его каюта, питание и жалованье учитываются как у обычного члена экипажа.
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
- `DemoEp0036Us0008Tk0004Tests.DemoStart_HasSeparateEngineer`
- `DemoEp0036Us0008Tk0004Tests.ExistingCrew_Preserved`
- `DemoEp0036Us0008Tk0004Tests.EngineerPortrait_Exists`
- `DemoEp0036Us0008Tk0004Tests.EngineerLocale_Parity`

Из корня репозитория; абсолютный run создаётся вне репозитория. Пример команды запуска всего небольшого named corpus этого тикета с process-tree cap 300000 ms:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0008-TK-0004-starter-content/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Path $run -Force | Out-Null
$testArgs = @('test', 'tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj', '-c', 'Release', '--filter', 'FullyQualifiedName~DemoEp0036Us0008Tk0004Tests', '--results-directory', $run, '--logger', 'trx')
$p = Start-Process dotnet -ArgumentList $testArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput "$run/test.stdout.log" -RedirectStandardError "$run/test.stderr.log"
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree cap reached' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build`; использовать тот же process-tree cap и перенаправление логов. Для C# слоя `dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include `; для content-only format не запускать. При длительном corpus делить по точным test IDs выше, не увеличивать cap.

## Definition of Done

- Все steps выполнены только в allowlist; указанные AC покрыты named tests.
- Focused tests и owning build проходят; исходные failures указаны явно и не скрыты.
- DTO/identity/defaults/save-load и rejection paths проверены; нет неоговорённых side effects.
- Документированы фактические проверки, API и remaining gates; native/product не закрываются автоматическими тестами.
- Нет незаписанных assumptions и скрытых изменений вне этого layer. Planning остаётся отдельным от implementation acceptance.

## Self-containment check

Входы, planned API, allowlist, AC, порядок и named проверки перечислены. Внешние prerequisite результаты должны существовать до исполнения. Если реализация требует иных файлов или продуктового решения, сначала уточнить этот Board тикет, а не расширять diff.

## Execution — 2026-11-17

Added separate Alex Morgan/CHR-PLAYER-ENGINEER with existing male portrait and omitted skill for seeded generation. Captain remains first crew member; existing identities/skills unchanged. RU/EN role/percentage-point labels added. JSON parse/scoped format/diff PASS. Real New Game and legacy-no-grant tests added, NOT RUN until US8 completion.
