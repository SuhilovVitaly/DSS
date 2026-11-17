---
epic: EP-0033-demo-company-services
story: EP-0033-US-0002-ammunition-supply
ticket: EP-0033-US-0002-TK-0002-content
title: Контент: Покупка торпед и восстановление расходников
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: content-data
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0033-US-0002-TK-0001-contracts]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

# Контент: Покупка торпед и восстановление расходников

## Why

Покупка торпед и восстановление расходников. Тикет 2/5 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0033-US-0002-ammunition-supply.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0033-US-0002-TK-0001-contracts. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Items/Ammunition/items-torpedoes.json` | `src/DeepSpaceSaga.Client/Data/Items/Ammunition/items-torpedoes.json:1` — { | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Client/Data/Markets/station-market-profiles.json` | `src/DeepSpaceSaga.Client/Data/Markets/station-market-profiles.json:2` — "schemaVersion": 1, | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Client/Data/Demo/services.json` | Планируемый файл; создаётся в `EP-0033-US-0001-TK-0002-content`, не текущая реализация. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0033Us0002Tk0002Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Именованные проверки этого тикета; не менять production из тестов. |

Разрешены только эти 4 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

Версионированный JSON существующие item/market schemas и services v1

## Implementation steps

1. Добавить доступность существующих торпед на сервисных станциях, конечные остатки/пополнение и стоимость зарядов ловушек.
2. Проверить capacity/цены, не менять правило один реальный выстрел — одна торпеда.

## Served acceptance criteria

- **AC-01:** Покупка учитывает деньги, остатки, вместимость и финансовую запись.
- **AC-02:** Перезарядка сохраняет баланс трюм/магазин; заряды ловушек не возникают бесплатно от выхода или загрузки.
- **AC-03:** Игрок видит, сколько вылетов или выстрелов позволяет текущий запас без обещания неизвестного исхода.
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

- `DemoEp0033Us0002Tk0002Tests.Content_LoadsAndReferencesResolve` — проверяет шаг 1 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0033Us0002Tk0002Tests.DemoLimits_AreSatisfied` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0033Us0002Tk0002Tests.LegacyScenario_DoesNotReceiveDemoGrant` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.

Запуск из `D:/DeepSpaceSaga/DSS`; создать уникальный run, ограничить **каждый process tree 300000 ms**, не продолжать после timeout/failure. Пример PowerShell для именованного test corpus:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0033-US-0002-TK-0002-content/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0033Us0002Tk0002Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build`. Format: `dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0033Us0002Tk0002Tests.cs`; тот же timeout wrapper и абсолютные stdout/stderr. Не запускать format для content-only. Длительный corpus делить по указанным полным IDs, не увеличивать cap. Native/manual сценарий: AC-01: Покупка учитывает деньги, остатки, вместимость и финансовую запись. AC-02: Перезарядка сохраняет баланс трюм/магазин; заряды ловушек не возникают бесплатно от выхода или загрузки. AC-03: Игрок видит, сколько вылетов или выстрелов позволяет текущий запас без обещания неизвестного исхода. AC-04: Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

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

## Execution
Existing torpedo item unchanged. Transit and industrial markets: 24 initial units, two produced per calendar hour subject to existing economy rules, target 24. Decoy charge price remains 25. Baseline balance requires playtest. Validation pending story boundary.

Story-boundary correction: initial market validation rejected missing supply/demand membership; transit validation correctly prohibits production. Final design: industrial produces two torpedoes per hour; transit demands/consumes one and receives deliveries via existing industrial-to-transit cargo flow. Both start with 24 units before size scaling. No transit production exception. Real engine fixtures now load successfully (engine5.log).
