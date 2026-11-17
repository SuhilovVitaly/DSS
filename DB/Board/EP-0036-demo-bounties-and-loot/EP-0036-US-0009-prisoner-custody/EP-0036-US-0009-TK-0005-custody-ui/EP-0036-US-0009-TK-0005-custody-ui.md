---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0009-prisoner-custody
ticket: EP-0036-US-0009-TK-0005-custody-ui
title: Арест в окне лута и список пленных на корабле
stage: approved
approval_kind: workflow
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0036-US-0009-TK-0003-capacity-and-rations, EP-0036-US-0009-TK-0004-localized-feedback, EP-0036-US-0007-TK-0004-postbattle-map]
files_touched: 3
serves: [AC-01, AC-04, AC-05]
created: 2026-11-17T19:18:11Z
revision: 1
implementation_status: not-started
---

# Арест в окне лута и список пленных на корабле

## Why

Как игрок, после победы я могу арестовать живого пилота выведенного из строя вражеского дрона. Он занимает одну общую каюту, остаётся отдельным пленником и потребляет пайки без жалованья; повторное действие или Load не создаёт второго пленного. Этот тикет даёт результат: арест в окне лута и список пленных на корабле.

## Decisions

Пользователь: «В демо только арест и содержание; взаимодействия отдельным будущим эпиком»; «Одна каюта на каждого пленного»; «Убираем постоянный резерв: экипаж, пассажиры и пленные используют общий фонд; пленный занимает одну свободную каюту»; «Да, по существующей норме на человека, без жалованья».

Дословные ответы и время записи 2026-11-17T19:18:11Z (не время отправки); [полный контекст](../EP-0036-US-0009-prisoner-custody.md). Пользовательские решения не подменяются техническими assumptions.

## Assumptions

A-09-01: пленник отдельная запись, не CrewMember и не контрактный Passenger. При нехватке пищи используется уже существующий MissingRations, без новой модели голода/смерти. A-09-02: принятие пассажирского обязательства, ещё не посаженного на борт, не позволяет украсть обещанную каюту при аресте; свободное место учитывает такое обязательство. Арест сам по себе не требует запаса еды как дополнительного неоговорённого условия. A-09-03: не вводятся побеги, лечение, освобождение, переводы, зарплата, продажа или награда за пленного.

Ниже новые API — проект, а не утверждение текущей реализации. Выполнить prerequisites до реализации; при drift сначала поправить Board allowlist. Не расширять её молча.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Demo/BattleLootWindow.cs` | Planned US7 file | Кнопка ареста и receipt/reason без клиентской мутации. |
| `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.cs` | Существующий экран | Отдельный read-only список пленных и общий cabin count. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0009Tk0005Tests.cs` | Новый файл | UI pending/reopen/empty/rejection/locale tests. |

Разрешены ровно 3 implementation files, включая тестовый. Read-only входы: [story](../EP-0036-US-0009-prisoner-custody.md), [epic](../../Documentation.md), prerequisite tickets в frontmatter. Это не разрешение менять другие production layers.

## Public API after the change

Existing ExecuteDemoCampaignAsync transports ArrestPilotCommand; PrisonerCustodySnapshot comes from Company. No extra local-client API; UI uses callback supplied by owning loot window.

## Implementation steps

1. После клика по Disabled enemy drone показать живого пилота и 'Арестовать'; кнопка использует engine can-capture/reason из source projection US7.
2. При клике отправить command identity/revision, показать pending; повторный ввод не создаёт локально пленного.
3. После подтверждения показать captured state, список на корабле и обновлённые free cabins. При отказе сохранить возможность исправить ситуацию в рамках открытой loot session.
4. Выход с остатками использует предупреждение US7, включает неарестованных доступных пилотов. Не добавлять отдельное автоматическое закрытие/повтор ареста.
5. Показать RU/EN причины и отсутствие пленных. Никаких кнопок пыток/вербовки/сдачи.

## Served acceptance criteria

- AC-01: На открытой postbattle loot session арест живого пилота Disabled enemy drone разрешён при одной доступной общей каюте. Destroyed/no pilot/wrong side/closed battle не допускаются.
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
- `DemoEp0036Us0009Tk0005Tests.ArrestButton_UsesAuthoritativeTarget`
- `DemoEp0036Us0009Tk0005Tests.Pending_NoLocalPrisoner`
- `DemoEp0036Us0009Tk0005Tests.NoCabin_ShowsReason`
- `DemoEp0036Us0009Tk0005Tests.Reopen_DoesNotResubmit`
- `DemoEp0036Us0009Tk0005Tests.ShipRoster_ShowsPrisonersSeparately`
- `DemoEp0036Us0009Tk0005Tests.Locales_CoverAllCustodyReasons`

Из корня репозитория; абсолютный run создаётся вне репозитория. Пример команды запуска всего небольшого named corpus этого тикета с process-tree cap 300000 ms:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0009-TK-0005-custody-ui/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Path $run -Force | Out-Null
$testArgs = @('test', 'tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj', '-c', 'Release', '--filter', 'FullyQualifiedName~DemoEp0036Us0009Tk0005Tests', '--results-directory', $run, '--logger', 'trx')
$p = Start-Process dotnet -ArgumentList $testArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput "$run/test.stdout.log" -RedirectStandardError "$run/test.stderr.log"
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree cap reached' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build`; использовать тот же process-tree cap и перенаправление логов. Для C# слоя `dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include src/DeepSpaceSaga.Client/UI/Demo/BattleLootWindow.cs src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.cs`; для content-only format не запускать. При длительном corpus делить по точным test IDs выше, не увеличивать cap.

## Definition of Done

- Все steps выполнены только в allowlist; указанные AC покрыты named tests.
- Focused tests и owning build проходят; исходные failures указаны явно и не скрыты.
- DTO/identity/defaults/save-load и rejection paths проверены; нет неоговорённых side effects.
- Документированы фактические проверки, API и remaining gates; native/product не закрываются автоматическими тестами.
- Нет незаписанных assumptions и скрытых изменений вне этого layer. Planning остаётся отдельным от implementation acceptance.

## Self-containment check

Входы, planned API, allowlist, AC, порядок и named проверки перечислены. Внешние prerequisite результаты должны существовать до исполнения. Если реализация требует иных файлов или продуктового решения, сначала уточнить этот Board тикет, а не расширять diff.

## Execution
Loot pilot panel sends ArrestPilotCommand through shared pending/retry controller; capture display follows authoritative receipt only. Ship has a separate scrollable read-only prisoner tab with real free cabin count and upkeep explanation. Client/test build PASS (D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us9-tk5-build.log); actual click/pending/receipt test authored, deferred to US9 boundary. Native NOT RUN.
