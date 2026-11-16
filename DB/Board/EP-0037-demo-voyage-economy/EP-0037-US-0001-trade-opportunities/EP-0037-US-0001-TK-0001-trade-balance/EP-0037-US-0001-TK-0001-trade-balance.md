---
epic: EP-0037-demo-voyage-economy
story: EP-0037-US-0001-trade-opportunities
ticket: EP-0037-US-0001-TK-0001-trade-balance
title: Шесть–десять товаров и первый прибыльный маршрут
stage: approved
approval_kind: workflow
implementation_status: not-started
layer: content-data
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0034-US-0001-TK-0004-client, EP-0032-US-0004-TK-0004-client, EP-0034-US-0002-TK-0003-client]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

# Шесть–десять товаров и первый прибыльный маршрут

## Why

Понятные торговые возможности первого рейса. Тикет 1/3 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0037-US-0001-trade-opportunities.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0034-US-0001-TK-0004-client, EP-0032-US-0004-TK-0004-client, EP-0034-US-0002-TK-0003-client. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Items/Good/items-good.json` | `src/DeepSpaceSaga.Client/Data/Items/Good/items-good.json:87` — "schemaVersion": 2, | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Client/Data/Items/Resource/items-resource.json` | `src/DeepSpaceSaga.Client/Data/Items/Resource/items-resource.json:76` — "schemaVersion": 2, | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Client/Data/Markets/station-market-profiles.json` | `src/DeepSpaceSaga.Client/Data/Markets/station-market-profiles.json:2` — "schemaVersion": 1, | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Client/Data/Demo/cluster.json` | Планируемый файл; создаётся в `EP-0032-US-0004-TK-0002-content`, не текущая реализация. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0037Us0001Tk0001Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Именованные проверки этого тикета; не менять production из тестов. |

Разрешены только эти 5 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

Штатные item/market schemas; cluster roles не меняют физику.

## Implementation steps

1. A-01: выбрать 8 существующих основных товаров; закрепить источники/спрос по пяти station roles без дублирования item IDs. У каждой партии конечные доступные stock/capacity/budget.
2. На стартовом капитале после первого топлива/сборов остаются деньги на доступную партию и пассажирский рейс; купить/продать через реальные quotes, не по справочной цене.
3. Фиксированный baseline route должен дать положительный net после топлива/сборов/содержания, не требуя bounty. Фиксировать диапазоны, не гарантировать прибыль после произвольной задержки/объёма.

## Served acceptance criteria

- **AC-01:** Для 6–10 основных товаров заданы различимые источники и спрос; есть доступный первый прибыльный маршрут.
- **AC-02:** Удалённое наблюдение показано с возрастом, не выдаётся за гарантированную котировку.
- **AC-03:** Конечные запасы, бюджеты и топливо сохраняют смысл выбора; торговля не требует боевой награды для старта.
- **AC-04:** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Вклад этого тикета ограничен слоем `content-data`; полный AC принимается после всей карты истории, включая реальный пользовательский путь. DTO/test scaffold сам по себе не закрывает AC.

## Out of scope

Полная верфь, другие носители, фракционная симуляция, пленные, тактическое отступление, сохранение внутри боя, универсальный квестовый движок и публикация. Не переписывать поставленный EP-0029, не ремонтировать несвязанные файлы и не расширять лимит пяти файлов молча.

## Invariants

- `DB/Documentation/01-Requirements/EngineRequirements.md`, «Архитектура и авторитет» / «Команды, события и воспроизводимость»: Engine владеет исходами/временем/RNG/деньгами; render не мутирует мир.
- `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`: асинхронный transport. `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs:17`: использовать единый settlement, не создавать второй inventory.
- `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:40`: на grounding writer Save21; переход к Save22 принадлежит EP-0032-US-0007. Последующие тикеты не назначают конкурирующие версии writer.
- Reject/duplicate не даёт частичных денег, груза, награды, опыта или повторного звука. Pause не двигает deadlines; calendar и motion units не смешиваются.
- Реализация gameplay-инвариантов — в owning engine ticket; клиентский/content/tooling тикет их не дублирует.

## Tests

Будущие тесты (ещё не реализованы/не запущены):

- `DemoEp0037Us0001Tk0001Tests.EightGoods_HaveSupplyAndDemand` — проверяет шаг 1 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0037Us0001Tk0001Tests.FirstVoyage_AffordableWithoutBounty` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0037Us0001Tk0001Tests.FiniteBudget_PreventsInfiniteArbitrage` — проверяет шаг 3 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.

Запуск из `D:/DeepSpaceSaga/DSS`; создать уникальный run, ограничить **каждый process tree 300000 ms**, не продолжать после timeout/failure. Пример PowerShell для именованного test corpus:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0037-US-0001-TK-0001-trade-balance/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0037Us0001Tk0001Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build`. Format: `dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0037Us0001Tk0001Tests.cs`; тот же timeout wrapper и абсолютные stdout/stderr. Не запускать format для content-only. Длительный corpus делить по указанным полным IDs, не увеличивать cap. Native/manual сценарий: AC-01: Для 6–10 основных товаров заданы различимые источники и спрос; есть доступный первый прибыльный маршрут. AC-02: Удалённое наблюдение показано с возрастом, не выдаётся за гарантированную котировку. AC-03: Конечные запасы, бюджеты и топливо сохраняют смысл выбора; торговля не требует боевой награды для старта. AC-04: Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Native запуск — только после проверки display layout, на меньшем мониторе с isolated profile/explicit index и `DSS_VALIDATION_SMALL_MONITOR=1`; иначе NOT RUN. Автотест не закрывает native, слуховую, независимую или продуктовую приёмку.

## Definition of Done

- Все шаги выполнены только в allowlist, тесты проверяют указанные исходы и ошибки.
- Served AC имеют evidence вклада этого слоя; story end-to-end/native/product gates не закрыты автоматически.
- Focused checks и owning build/format проходят либо конкретное исходное падение записано как блокер; timeout не PASS.
- Public API, units, persistence, idempotency и ограничения соблюдены; нет скрытого follow-up вне Board.
- Evidence в `D:/DeepSpaceSaga/artifacts/validation/<ticket>/<unique-run>/`, status/решения в Board, актуальные контракты в Documentation.
- Удалены только собственные disposable scripts/caches/results; сохранены необходимые ссылки evidence, нет мусора в корнях workspace/repo.

## Self-containment check

Prerequisites, allowlist, новые схемы/команды, concrete steps, критерии и named tests перечислены здесь. Для новых файлов указан создатель; их наличие до исполнения dependency не предполагается. Чтение источников допустимо для проверки drift, но новый продуктовый выбор или расширение files требует явной правки Board.
