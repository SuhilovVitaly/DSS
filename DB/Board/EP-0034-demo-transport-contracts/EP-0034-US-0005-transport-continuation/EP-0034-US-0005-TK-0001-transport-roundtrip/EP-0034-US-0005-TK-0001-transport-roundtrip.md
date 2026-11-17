---
epic: EP-0034-demo-transport-contracts
story: EP-0034-US-0005-transport-continuation
ticket: EP-0034-US-0005-TK-0001-transport-roundtrip
title: Сохранённый смешанный рейс
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0034-US-0003-TK-0003-client, EP-0034-US-0004-TK-0003-client, EP-0032-US-0003-TK-0004-terminal-save-policy]
files_touched: 2
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 1
---

# Сохранённый смешанный рейс

## Why

Продолжение рейса со всеми обязательствами. Тикет 1/2 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0034-US-0005-transport-continuation.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0034-US-0003-TK-0003-client, EP-0034-US-0004-TK-0003-client, EP-0032-US-0003-TK-0004-terminal-save-policy. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.PassengerContracts.cs` | Планируемый файл; создаётся в `EP-0034-US-0002-TK-0002-engine`, не текущая реализация. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CargoContracts.cs` | Планируемый файл; создаётся в `EP-0034-US-0004-TK-0002-engine`, не текущая реализация. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs` | Планируемый файл; создаётся в `EP-0032-US-0007-TK-0002-campaign-state`, не текущая реализация. | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0034Us0005Tk0001Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Именованные проверки этого тикета; не менять production из тестов. |

Разрешены только эти 4 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

No API change.

## Implementation steps

1. Построить fixture ordinary+urgent пассажиров, sealed cargo и свободного товара; checkpoint до принятия, в полёте, перед deadline, после выплаты и после cancellation.
2. Проверить equality contract IDs/deadlines/cabins/mass/receipt/ledger до и после Load; повтор docking/open board не даёт второй payout.
3. Исправлять только сериализацию/валидацию указанного состояния, не баланс или UI. Rejected malformed save сохраняет предыдущий мир.

## Served acceptance criteria

- **AC-01:** Сверить до/после загрузки активные и завершённые заказы, сроки, выплаты, каюты и контрактный груз.
- **AC-02:** Повторное прибытие или открытие доски не создаёт вторую выплату.
- **AC-03:** Обычная и срочная перевозки вместе с торговым товаром проходят сквозной мирный рейс.
- **AC-04:** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Вклад этого тикета ограничен слоем `engine`; полный AC принимается после всей карты истории, включая реальный пользовательский путь. DTO/test scaffold сам по себе не закрывает AC.

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

- `DemoEp0034Us0005Tk0001Tests.MixedVoyage_RoundTripsEveryBoundary` — проверяет шаг 1 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0034Us0005Tk0001Tests.ArrivalReplay_DoesNotPayAgain` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0034Us0005Tk0001Tests.ReservedCargo_IsNotTradable` — проверяет шаг 3 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.

Запуск из `D:/DeepSpaceSaga/DSS`; создать уникальный run, ограничить **каждый process tree 300000 ms**, не продолжать после timeout/failure. Пример PowerShell для именованного test corpus:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0034-US-0005-TK-0001-transport-roundtrip/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0034Us0005Tk0001Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build`. Format: `dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.PassengerContracts.cs src/DeepSpaceSaga.Engine/SimulationEngine.CargoContracts.cs src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0034Us0005Tk0001Tests.cs`; тот же timeout wrapper и абсолютные stdout/stderr. Не запускать format для content-only. Длительный corpus делить по указанным полным IDs, не увеличивать cap. Native/manual сценарий: AC-01: Сверить до/после загрузки активные и завершённые заказы, сроки, выплаты, каюты и контрактный груз. AC-02: Повторное прибытие или открытие доски не создаёт вторую выплату. AC-03: Обычная и срочная перевозки вместе с торговым товаром проходят сквозной мирный рейс. AC-04: Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

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

Статус: implemented; **validation pending at story end** (каденция владельца: на тикет — сборка и целевые тесты, story-тесты в конце US-0005). PASS истории здесь не заявляется.

Изменённые файлы (`files_touched` 4 → 2: `SimulationEngine.CargoContracts.cs` и `DemoCampaignData.cs` изменений не потребовали — сериализация и валидация всех секций уже переживают Save/Load, что доказано тестами ниже):

- `src/DeepSpaceSaga.Engine/SimulationEngine.PassengerContracts.cs` — `DeliverPassengerContracts` теперь увеличивает `Revision`/`CommitSequence` кампании, как доставка груза (US-0004) и переходы отказов (US-0003). Выявленный дефект: выплата только за пассажиров не была коммитом кампании, поэтому автосохранение (наблюдает `Revision`) не срабатывало после выплаты, и слот продолжения мог хранить состояние стыковки до выплаты (A-01).
- `tests/DeepSpaceSaga.Engine.Tests/Demo/DemoEp0034Us0005Tk0001Tests.cs` — новый: `MixedVoyage_RoundTripsEveryBoundary`, `ArrivalReplay_DoesNotPayAgain`, `ReservedCargo_IsNotTradable`.

Что проверяют тесты (production-путь без mock: New Game через `CreateEngineFromScenarioFile`, каждый Save/Load — реальный JSON-файл через `ScenarioLoader.Serialize` → `EngineContentLoader.CreateEngineFromSaveFile`; полёты — реальные команды Undock/Accelerate/Approach/Synchronization/Dock и диалог стыковки; торговля — quoted trade):

- `MixedVoyage_RoundTripsEveryBoundary` — у SPC-0002 приняты срочные и обычные пассажиры (все 4 свободные каюты), куплен свой товар. Сверка «до/после загрузки» (деньги, ревизия и commit sequence кампании, вся транспортная проекция: доска, журнал, каюты, пассажирские и грузовые контракты, отказы и долг; люди на борту, стеки трюма, voyage ledger) на checkpoints: до приёма, после приёма у станции, в полёте, после выплаты (у SPC-0005 с новыми грузовыми обязательствами), после отмены в полёте (все виды обязательств одновременно: обычные пассажиры, опечатанный груз, отменённый груз в `return_pending` с долгом, свой товар, завершённый срочный контракт), по окончании рейса. Дважды (каждое плечо) продолжение после загрузки в полёте заканчивается ровно в том же состоянии, что и непрерывный рейс (эквивалентность разбиения). Срочные пассажиры доставлены до срока у SPC-0005 (одна выплата, posting в ledger прибытия, commit кампании); свой товар продан там (gross sales). Обычные пассажиры и опечатанный груз доставлены у SPC-0008 до срока; выплаты сначала гасят долг, отменённый груз сдан без оплаты; итог: долг 0, людей на борту 0, резерв 0; журнал содержит каждый исход один раз; сумма выплат в ledger = сумме трёх контрактов, штраф проведён один раз; свой товар того же типа, что и опечатанный, продаётся ровно в своём количестве (3, а не 4).
- `ArrivalReplay_DoesNotPayAgain` — сохранение при открытом диалоге стыковки (до подтверждения ничего не выплачено); загруженное продолжение и непрерывное подтверждение дают одинаковое состояние; повторные снапшоты/чтение доски, дубликат команды (тот же receipt), новая команда (`offer_already_accepted`), повторная загрузка пристыкованной игры (повтор тика прибытия) и реальное повторное прибытие на ту же станцию (рейс туда и обратно) не дают второй выплаты и второго posting.
- `ReservedCargo_IsNotTradable` — опечатанный груз и свой товар того же типа: продаётся только своё (до и после Load), разбиение трюма сходится; повреждённый трюм ниже резерва загружается, резерв сохранён, места для торговли нет, груз доставляется и оплачивается один раз; повреждённое сохранение (подменённый posting груза, обязательства без записи контракта) отклоняется и `CreateEngineFromSaveFile`, и `LoadScenario` работающей игры — состояние работающей игры не меняется.

Проверки (артефакты `D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0005-tk0001/`):

- red — до исправления `--filter FullyQualifiedName~DemoEp0034Us0005Tk0001Tests` → 2 passed / 1 failed: `MixedVoyage_RoundTripsEveryBoundary` «passenger payout must be a campaign commit» (`engine-red.log`, `engine-red.trx`).
- green — тот же фильтр → 3/3 passed (`engine-green.log`, `engine-green.trx`).
- быстрая регрессия `FullyQualifiedName~DemoEp0034|FullyQualifiedName~DemoEp0032` (Engine) → 45/45 passed (`engine-ep0034.log`, `engine-ep0034.trx`).
- `dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release` → 0 warnings / 0 errors (`build-engine.log`); `dotnet format ... --verify-no-changes --include` двух файлов → exit 0 (`format.log`); `git diff --check` чист.

Допущения:

- A-01 (technical): доставка пассажиров — коммит кампании (`Revision`/`CommitSequence` + 1) для автосохранения, как остальные транспортные переходы. Следствие: команда доски/отмены, отправленная с ревизией до выплаты, получает `stale_revision`; поведение клиента при этом проверяет TK-0002.
- A-02 (technical, план рейса): маршрут выбран из детерминированной первой публикации: SPC-0002 (пассажиры) → SPC-0005 (грузы) → SPC-0008. Наблюдение для баланса (не исправлялось — тикет не меняет баланс): трёхимпульсный полёт SPC-0005 → SPC-0002 занял ≈ 9 ч календаря при сроке обычного заказа 2,5 ч (обратное направление укладывается ≈ 1,2 ч, US-0002 A-04) — срок от ETA максимальной скорости не учитывает геометрию разворота; передано в плейтест EP-0041.
- A-03 (save format): bump нет; новых секций нет; A-P2 не задействован.

Открытые проблемы US-0004 (решение в рамках US-0005):

- Выдача груза диалогом (`DialogueEffectTransaction`, `AddCargoItem`) без учёта резерва — **оставлена ограничением**: ни один демо-контент не использует `AddCargoItem` (поиск по `src/DeepSpaceSaga.Client/Data` и `Scenarios` без `bin` пуст), поэтому мирный рейс продолжения затронуть её не может.
- Повреждение трюма ниже резерва — **проверено, не исправлялось**: такое состояние загружается, резерв и доставка сохраняются, единственная выплата не нарушается, торговля не получает места (`ReservedCargo_IsNotTradable`). Правило уничтожения опечатанного груза при повреждении остаётся вне scope (бой — EP-0035/EP-0036).
