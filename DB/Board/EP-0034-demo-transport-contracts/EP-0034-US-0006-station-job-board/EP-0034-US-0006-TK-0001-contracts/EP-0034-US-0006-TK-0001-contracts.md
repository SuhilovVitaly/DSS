---
epic: EP-0034-demo-transport-contracts
story: EP-0034-US-0006-station-job-board
ticket: EP-0034-US-0006-TK-0001-contracts
title: Контракт: Доска заказов станции
stage: approved
approval_kind: workflow
implementation_status: implemented-validation-pending
layer: contracts
matching_test_project: tests/DeepSpaceSaga.Contracts.Tests
depends_on: [EP-0032-US-0004-TK-0004-client]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

# Контракт: Доска заказов станции

## Why

Доска заказов станции. Тикет 1/5 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0034-US-0006-station-job-board.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0032-US-0004-TK-0004-client. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/DemoCampaign.cs` | Планируемый файл; создаётся в `EP-0032-US-0007-TK-0001-campaign-contract`, не текущая реализация. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Contracts/StationJobsContracts.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0034Us0006Tk0001Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Именованные проверки этого тикета; не менять production из тестов. |

Разрешены только эти 3 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

StationJobsSnapshot(offers,obligations,observedAt); JobOffer(id,type,origin,destination,count,mass,payout,deadlineDuration,expiresAt,riskBand); typed accept команды owning stories.

## Implementation steps

1. Добавить типизированные команды/immutable DTO раздела StationJobs; использовать общие CommandId/ExpectedRevision и stable reason codes.
2. StationJobsSnapshot(offers,obligations,observedAt); JobOffer(id,type,origin,destination,count,mass,payout,deadlineDuration,expiresAt,riskBand); typed accept команды owning stories.
3. Числа, недоступность и варианты действий приходят из авторитетного snapshot; optional поля не превращают отсутствие данных в бесплатные ресурсы.

## Served acceptance criteria

- **AC-01:** Engine формирует предложения детерминированно по seed и роли станции; их число ограничено, у предложения есть срок публикации, обновление идёт по игровому календарю и не зависит от повторного открытия экрана.
- **AC-02:** Оплата предложения объяснима: она зависит от расстояния, срочности и известного риска маршрута; заказы ведут к станциям демо-кластера и выполнимы текущим носителем.
- **AC-03:** Экран Contracts перестаёт быть заглушкой: доска показывает типы заказов EP-0034 и EP-0036, а единый журнал обязательств доступен с карты и со станции.
- **AC-04:** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Вклад этого тикета ограничен слоем `contracts`; полный AC принимается после всей карты истории, включая реальный пользовательский путь. DTO/test scaffold сам по себе не закрывает AC.

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

- `DemoEp0034Us0006Tk0001Tests.Payload_RoundTrips` — проверяет шаг 1 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0034Us0006Tk0001Tests.LegacyPayload_UsesSafeDefaults` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0034Us0006Tk0001Tests.ReasonCodes_AreStable` — проверяет шаг 3 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.

Запуск из `D:/DeepSpaceSaga/DSS`; создать уникальный run, ограничить **каждый process tree 300000 ms**, не продолжать после timeout/failure. Пример PowerShell для именованного test corpus:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0034-US-0006-TK-0001-contracts/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0034Us0006Tk0001Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --artifacts-path <absolute-run>/build`. Format: `dotnet format src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --include src/DeepSpaceSaga.Contracts/DemoCampaign.cs src/DeepSpaceSaga.Contracts/StationJobsContracts.cs tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0034Us0006Tk0001Tests.cs`; тот же timeout wrapper и абсолютные stdout/stderr. Не запускать format для content-only. Длительный corpus делить по указанным полным IDs, не увеличивать cap. Native/manual сценарий: AC-01: Engine формирует предложения детерминированно по seed и роли станции; их число ограничено, у предложения есть срок публикации, обновление идёт по игровому календарю и не зависит от повторного открытия экрана. AC-02: Оплата предложения объяснима: она зависит от расстояния, срочности и известного риска маршрута; заказы ведут к станциям демо-кластера и выполнимы текущим носителем. AC-03: Экран Contracts перестаёт быть заглушкой: доска показывает типы заказов EP-0034 и EP-0036, а единый журнал обязательств доступен с карты и со станции. AC-04: Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

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

## Execution result (2026-11-17)

Status: implemented; **validation pending at story end** (owner cadence: per-ticket build, story-scoped tests at the end of US-0006). No story PASS is claimed here.

Changed files (within the allowlist; `DemoCampaign.cs` did not need a change because `DemoTransportSnapshot` is already `partial` since US-0001):

- `src/DeepSpaceSaga.Contracts/StationJobsContracts.cs` — new: `StationJobTypes` (`passenger`, `urgent_passenger`, `cargo`, `bounty` — the last one published by EP-0036), `StationJobRiskBands` (`safe/normal/elevated`), `StationJobStatuses` (`active/completed/failed/cancelled`), `JobPayoutTerms(BaseTokens, LoadTokens, DistanceTokens, UrgencyPermille, RiskPermille)`, `JobOffer(Id, Type, OriginStationId, DestinationStationId, Count, MassKg, Payout, DeadlineDurationMs, PublishedAtGameTimeMs, ExpiresAtGameTimeMs, RiskBand, RouteDistanceKm, Terms, ItemTypeId?)`, `JobObligation(ContractId, OfferId, Type, Origin, Destination, Count, MassKg, Payout, AcceptedAtGameTimeMs, DeadlineGameTimeMs, Status, RiskBand, ItemTypeId?, ClosedAtGameTimeMs?)`, `JobOfferAvailability(OfferId, Blocker)`, `StationJobsSnapshot(Offers, Obligations, ObservedAtGameTimeMs, DockedStationId?, Availability)`, partial `DemoTransportSnapshot.StationJobs`, typed `AcceptJobOfferCommand(CommandId, ExpectedRevision, OfferId) : DemoCampaignCommand`, stable `StationJobReasons` (`offer_not_found`, `offer_expired`, `offer_already_accepted`, `not_docked_at_origin`, `insufficient_cargo_space`) and `AcceptBlocker(board, offerId)`.
- `tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0034Us0006Tk0001Tests.cs` — `Payload_RoundTrips`, `LegacyPayload_UsesSafeDefaults`, `ReasonCodes_AreStable`.

Checks: `dotnet build tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj -c Release` — succeeded, 0 warnings/0 errors. A quick focused run `--filter FullyQualifiedName~DemoEp0034Us0006Tk0001Tests` gave 3/3 (results `D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0006-20261117/tk0001`); the story run at the end is the evidence of record.

Assumptions:

- A-01 (technical): the board owns one generic `AcceptJobOfferCommand(offerId)`; it turns an offer into an `Active` journal obligation. The planned typed accept commands of US-0002 (`AcceptPassengerCommand`) and US-0004 (`AcceptCargoCommand`) either extend what acceptance does for their order type (boarding, sealed cargo, payout on delivery) or delegate to the same acceptance core; the board does not duplicate those rules. Capacity blockers reuse `PassengerCapacityReasons` from US-0001.
- A-02 (technical): acceptance verdicts are published per offer (`Availability`); an offer without a verdict, a board without verdicts or a missing board mean `unavailable`, never "acceptable". Missing JSON arrays deserialize as default arrays, so readers use `IsDefaultOrEmpty`.
- A-03 (technical): payout is explained by fixed integer terms (Tokens and permille), matching the determinism rule "factors fixed-point (1000 = 1.0)"; no `double` crosses the contract for money or time.
