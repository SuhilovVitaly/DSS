---
epic: EP-0034-demo-transport-contracts
story: EP-0034-US-0004-cargo-contract
ticket: EP-0034-US-0004-TK-0001-contracts
title: Контракт: Доставка контрактного груза
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: contracts
matching_test_project: tests/DeepSpaceSaga.Contracts.Tests
depends_on: [EP-0034-US-0002-TK-0003-client, EP-0034-US-0006-TK-0004-client]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

# Контракт: Доставка контрактного груза

## Why

Доставка контрактного груза. Тикет 1/4 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0034-US-0004-cargo-contract.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0034-US-0002-TK-0003-client, EP-0034-US-0006-TK-0004-client. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/DemoCampaign.cs` | Планируемый файл; создаётся в `EP-0032-US-0007-TK-0001-campaign-contract`, не текущая реализация. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Contracts/CargoContractsContracts.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0034Us0004Tk0001Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Именованные проверки этого тикета; не менять production из тестов. |

Разрешены только эти 3 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

AcceptCargoCommand(offerId); CargoContract(id,itemTypeId,mass,quantity,origin,destination,deadline,payout,status); ReservedCargo entries доступны только контрактным операциям.

## Implementation steps

1. Добавить типизированные команды/immutable DTO раздела CargoContracts; использовать общие CommandId/ExpectedRevision и stable reason codes.
2. AcceptCargoCommand(offerId); CargoContract(id,itemTypeId,mass,quantity,origin,destination,deadline,payout,status); ReservedCargo entries доступны только контрактным операциям.
3. Числа, недоступность и варианты действий приходят из авторитетного snapshot; optional поля не превращают отсутствие данных в бесплатные ресурсы.

## Served acceptance criteria

- **AC-01:** Приём резервирует фактическую массу трюма и явно различает контрактный груз и товар игрока.
- **AC-02:** Груз нельзя продать как свободный товар; сдача на нужной станции освобождает трюм и оплачивается один раз.
- **AC-03:** Потеря, отмена и просрочка имеют видимые последствия без дублирования денег или груза.
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

- `DemoEp0034Us0004Tk0001Tests.Payload_RoundTrips` — проверяет шаг 1 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0034Us0004Tk0001Tests.LegacyPayload_UsesSafeDefaults` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0034Us0004Tk0001Tests.ReasonCodes_AreStable` — проверяет шаг 3 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.

Запуск из `D:/DeepSpaceSaga/DSS`; создать уникальный run, ограничить **каждый process tree 300000 ms**, не продолжать после timeout/failure. Пример PowerShell для именованного test corpus:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0034-US-0004-TK-0001-contracts/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0034Us0004Tk0001Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --artifacts-path <absolute-run>/build`. Format: `dotnet format src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --include src/DeepSpaceSaga.Contracts/DemoCampaign.cs src/DeepSpaceSaga.Contracts/CargoContractsContracts.cs tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0034Us0004Tk0001Tests.cs`; тот же timeout wrapper и абсолютные stdout/stderr. Не запускать format для content-only. Длительный corpus делить по указанным полным IDs, не увеличивать cap. Native/manual сценарий: AC-01: Приём резервирует фактическую массу трюма и явно различает контрактный груз и товар игрока. AC-02: Груз нельзя продать как свободный товар; сдача на нужной станции освобождает трюм и оплачивается один раз. AC-03: Потеря, отмена и просрочка имеют видимые последствия без дублирования денег или груза. AC-04: Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

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

Статус: implemented; **validation pending at story end** (каденция владельца: на тикет — сборка, story-тесты в конце US-0004). PASS истории здесь не заявляется.

Изменённые файлы (`files_touched` 3; `DemoCampaign.cs` правок не потребовал — `DemoTransportSnapshot` уже `partial`):

- `src/DeepSpaceSaga.Contracts/CargoContractsContracts.cs` — новый: `CargoHoldStates` (`aboard`/`overdue`/`delivered`/`return_pending`/`returned`/`lost`, `IsKnown`, `OccupiesHold`); `CargoContract(ContractId, OfferId, ItemTypeId, MassKg, Quantity, OriginStationId, DestinationStationId, AcceptedAtGameTimeMs, DeadlineGameTimeMs, Payout, Status, PostingId, HoldState, DeliveredAtGameTimeMs?, PaidCredits?, ReturnedAtGameTimeMs?, ReturnStationId?)`; `CargoContractsSnapshot(Contracts, ObservedAtGameTimeMs, ReservedMassKg, HoldCapacityKg, PlayerCargoMassKg)` с вычисляемыми `FreeHoldKg` (≥0) и `Active`; partial `DemoTransportSnapshot.CargoContracts`; `CargoContractReasons` — posting `cargo:{contractId}`, `HoldState(section, id)` (отсутствие данных/неизвестный код → `unavailable`).
- `tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0034Us0004Tk0001Tests.cs` — `Payload_RoundTrips`, `LegacyPayload_UsesSafeDefaults`, `ReasonCodes_AreStable`.

Проверки: red — сборка тестового проекта падала с CS0246 (`CargoContract` отсутствует) до реализации; после реализации быстрый прогон `dotnet test tests/DeepSpaceSaga.Contracts.Tests --filter FullyQualifiedName~DemoEp0034` — 15/15 (TRX `D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0004-tk0001/contracts.trx`). Доказательством остаётся story-прогон в конце US-0004.

Допущения:

- A-01 (technical): планируемая `AcceptCargoCommand(offerId)` не вводится — приём груза, как и пассажиров (US-0002), идёт единой командой доски `AcceptJobOfferCommand(offerId)`; клиент не передаёт товар, массу, цену или срок.
- A-02 (technical): «ReservedCargo entries» — это опечатанные записи контракта (`CargoContract` + персистентная запись engine-тикета), а не стеки модуля трюма. Поэтому обычная торговля физически не видит их как товар игрока (продать/израсходовать нельзя); масса учитывается в разбиении трюма `ReservedMassKg` / `PlayerCargoMassKg` / `FreeHoldKg`.
- A-03 (technical): отказы приёма переиспользуют `StationJobReasons` (`insufficient_cargo_space` и др.), отмена/просрочка/потеря — `TransportFailureReasons` (US-0003); собственных кодов отказа груз не вводит. Состояние `return_pending` — груз проваленного контракта ещё в трюме до возврата на человеческой станции.
