---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0009-prisoner-custody
ticket: EP-0036-US-0009-TK-0004-localized-feedback
title: Тексты ареста и содержания RU/EN
stage: approved
approval_kind: workflow
layer: content-data
matching_test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0036-US-0009-TK-0001-contracts]
files_touched: 3
serves: [AC-04]
created: 2026-11-17T19:18:11Z
revision: 1
implementation_status: not-started
---

# Тексты ареста и содержания RU/EN

## Why

Как игрок, после победы я могу арестовать живого пилота выведенного из строя вражеского дрона. Он занимает одну общую каюту, остаётся отдельным пленником и потребляет пайки без жалованья; повторное действие или Load не создаёт второго пленного. Этот тикет даёт результат: тексты ареста и содержания ru/en.

## Decisions

Пользователь: «В демо только арест и содержание; взаимодействия отдельным будущим эпиком»; «Одна каюта на каждого пленного»; «Убираем постоянный резерв: экипаж, пассажиры и пленные используют общий фонд; пленный занимает одну свободную каюту»; «Да, по существующей норме на человека, без жалованья».

Дословные ответы и время записи 2026-11-17T19:18:11Z (не время отправки); [полный контекст](../EP-0036-US-0009-prisoner-custody.md). Пользовательские решения не подменяются техническими assumptions.

## Assumptions

A-09-01: пленник отдельная запись, не CrewMember и не контрактный Passenger. При нехватке пищи используется уже существующий MissingRations, без новой модели голода/смерти. A-09-02: принятие пассажирского обязательства, ещё не посаженного на борт, не позволяет украсть обещанную каюту при аресте; свободное место учитывает такое обязательство. Арест сам по себе не требует запаса еды как дополнительного неоговорённого условия. A-09-03: не вводятся побеги, лечение, освобождение, переводы, зарплата, продажа или награда за пленного.

Ниже новые API — проект, а не утверждение текущей реализации. Выполнить prerequisites до реализации; при drift сначала поправить Board allowlist. Не расширять её молча.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Существующий словарь | Ключи roster/arrest/pending/cabin/отказов. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Существующий словарь | Паритет keys и parameters. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0009Tk0004Tests.cs` | Новый файл | Locale keys/reason coverage tests. |

Разрешены ровно 3 implementation files, включая тестовый. Read-only входы: [story](../EP-0036-US-0009-prisoner-custody.md), [epic](../../Documentation.md), prerequisite tickets в frontmatter. Это не разрешение менять другие production layers.

## Public API after the change

No API change. Keys Prisoners.Title/Empty/Captured/Arrest/Pending/NoCabin/NotCapturable/AlreadyCaptured/LootClosed/CabinCount; параметры name/count/free.

## Implementation steps

1. Добавить RU/EN короткие пользовательские тексты всех причин TK1 и состояние captured/pending.
2. В текстах пленный занимает1 каюту и ест без жалованья; не обещать сдачу/вербовку или оплату.
3. Проверить одинаковые keys/parameters и использование этих ключей будущим TK5.

## Served acceptance criteria

- AC-04: Окно лута показывает арест/отказ, корабль показывает пленников и фактическую вместимость; отсутствуют сдача, пытки, вербовка и выплаты за пленного.

## Out of scope

Другие stories/layers, game launch, изменения требований, commit/push. Не вводить независимые копии денег/кают/навыков в Client. Пытки, вербовка и сдача пленных не входят в демо; установка трофейных модулей также вне текущего демо.

## Invariants

- Engine владеет изменением мира и RNG; Client принимает подтверждённые snapshots (EngineRequirements: архитектура/команды).
- Stable identity и revision: повторная команда и Load не создают повторный расход/человека/бонус.
- Crew identity не заменяется именем или портретом; отсутствие optional state не создаёт сущность.
- Никакой новой реализации нельзя объявлять по наличию этого тикета; acceptance остаётся OPEN до evidence.

## Tests

Будущие именованные проверки, NOT RUN:
- `DemoEp0036Us0009Tk0004Tests.CustodyReasons_HaveTranslations`
- `DemoEp0036Us0009Tk0004Tests.RussianEnglish_ParameterParity`
- `DemoEp0036Us0009Tk0004Tests.NoFutureInteractionsAdvertised`

Из корня репозитория; абсолютный run создаётся вне репозитория. Пример команды запуска всего небольшого named corpus этого тикета с process-tree cap 300000 ms:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0009-TK-0004-localized-feedback/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Path $run -Force | Out-Null
$testArgs = @('test', 'tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj', '-c', 'Release', '--filter', 'FullyQualifiedName~DemoEp0036Us0009Tk0004Tests', '--results-directory', $run, '--logger', 'trx')
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
