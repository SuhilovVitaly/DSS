---
epic: EP-0034-demo-transport-contracts
story: EP-0034-US-0005-transport-continuation
ticket: EP-0034-US-0005-TK-0002-transport-flow
title: Рейс через пользовательские экраны
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0034-US-0005-TK-0001-transport-roundtrip]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-16T20:32:46.441Z
revision: 2
---

# Рейс через пользовательские экраны

## Why

Продолжение рейса со всеми обязательствами. Тикет 2/2 даёт проверяемый результат своего слоя. [Story и полный пользовательский результат](../EP-0034-US-0005-transport-continuation.md).

## Decisions

Поручение владельца: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Время записи 2026-11-16T20:32:46.441Z, не доказанное время отправки. Разрешены тикеты и необходимые дополнительные истории. Human approval технических деталей/баланса не заявляется; stage approved означает автоматическую готовность workflow планирования. Исполнение и публикация требуют отдельного поручения.

## Assumptions

- Используется сокращённая демо на 60–120 минут, один кластер, готовый Tetrarch; backlog полного продукта не включается.
- Технические имена/новые API ниже — планируемые, не утверждения о текущем коде. Численные A-решения в шагах — baseline для owning плейтеста, не доказанный баланс.
- Сначала prerequisites: EP-0034-US-0005-TK-0001-transport-roundtrip. При drift исходников пересмотреть allowlist в Board до расширения diff.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs:22` — public sealed class ContractsScreen : IScreen | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Client/UI/Screens/Finance/FinanceScreen.cs` | `src/DeepSpaceSaga.Client/UI/Screens/Finance/FinanceScreen.cs:20` — public sealed class FinanceScreen : IScreen | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:35` — public sealed class SkiaWindow; экраны и их typed transport создаются в `Open*Async` (например `OpenShipAsync` :1540, `OpenContractsAsync` :1649) и маршрутизируются по `ScreenEvent` | Только изменения, перечисленные в Implementation steps; прочие функции сохраняются. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0034Us0005Tk0002Tests.cs` | Новый файл этого тикета; на момент grounding отсутствует. | Именованные проверки этого тикета; не менять production из тестов. |

Разрешены только эти 4 implementation/documentation files. Board evidence/status этого тикета обновляются отдельно как каноническая история исполнения; не дают разрешения править произвольный production. Read-only архитектурные источники: `DB/Documentation/00-Process/CLAUDE.md`, `DB/Documentation/01-Requirements/EngineRequirements.md` и [общая карта](../../../EP-0041-demo-validation-and-release/TicketPlan.md).

## Public API after the change

No API change.

## Implementation steps

1. Проверить реальный путь принять → корабль → выбрать маршрут → прибыть → итог → сохранить/загрузить с обычным и срочным заказом, своим товаром и sealed cargo.
2. Показать конкретные причины переполнения/expiry/stale offer и фактический баланс; убрать только выявленные ошибки wiring.
3. Записать отдельный native checklist в Board истории: steps, expected/actual, язык, checkpoint. Наличие mocked UI теста не закрывает ручную приёмку.

## Served acceptance criteria

- **AC-01:** Сверить до/после загрузки активные и завершённые заказы, сроки, выплаты, каюты и контрактный груз.
- **AC-02:** Повторное прибытие или открытие доски не создаёт вторую выплату.
- **AC-03:** Обычная и срочная перевозки вместе с торговым товаром проходят сквозной мирный рейс.
- **AC-04:** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

Вклад этого тикета ограничен слоем `client`; полный AC принимается после всей карты истории, включая реальный пользовательский путь. DTO/test scaffold сам по себе не закрывает AC.

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

- `DemoEp0034Us0005Tk0002Tests.ContractJournal_SurvivesScreenChanges` — проверяет шаг 1 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0034Us0005Tk0002Tests.Settlement_ShowsOnePayout` — проверяет шаг 2 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.
- `DemoEp0034Us0005Tk0002Tests.LoadedVoyage_RestoresVisibleDeadline` — проверяет шаг 3 и соответствующие AC; fixture использует реальные typed receipts/state, не копию алгоритма в тесте.

Запуск из `D:/DeepSpaceSaga/DSS`; создать уникальный run, ограничить **каждый process tree 300000 ms**, не продолжать после timeout/failure. Пример PowerShell для именованного test corpus:

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/EP-0034-US-0005-TK-0002-transport-flow/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
New-Item -ItemType Directory -Force -Path $run | Out-Null
$p = Start-Process dotnet -ArgumentList @('test','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','-c','Release','--filter','FullyQualifiedName~DemoEp0034Us0005Tk0002Tests','--results-directory',($run + '/results'),'--artifacts-path',($run + '/build')) -PassThru -WindowStyle Hidden -RedirectStandardOutput ($run + '/test.log') -RedirectStandardError ($run + '/test.err')
if (-not $p.WaitForExit(300000)) { taskkill /PID $p.Id /T /F; throw 'test process-tree timeout' }
if ($p.ExitCode -ne 0) { throw 'focused tests failed' }
```

Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build`. Format: `dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs src/DeepSpaceSaga.Client/UI/Screens/Finance/FinanceScreen.cs src/DeepSpaceSaga.Client/UI/SkiaWindow.cs tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0034Us0005Tk0002Tests.cs`; тот же timeout wrapper и абсолютные stdout/stderr. Не запускать format для content-only. Длительный corpus делить по указанным полным IDs, не увеличивать cap. Native/manual сценарий: AC-01: Сверить до/после загрузки активные и завершённые заказы, сроки, выплаты, каюты и контрактный груз. AC-02: Повторное прибытие или открытие доски не создаёт вторую выплату. AC-03: Обычная и срочная перевозки вместе с торговым товаром проходят сквозной мирный рейс. AC-04: Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

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

## Revision log

- 2026-11-17 — ревизия 2 по ревью декомпозиции: `src/DeepSpaceSaga.Client/Program.cs` заменён в allowlist на `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs`. `Program.cs` только создаёт `SkiaWindow` и главное меню; открытие экранов, передача transport-делегатов и обработка `ScreenEvent` находятся в `SkiaWindow.Open*Async`. Число файлов и шаги по существу не изменились.

## Execution result (2026-11-17)

Статус: implemented; **validation pending at story end** (каденция владельца). PASS истории здесь не заявляется.

Изменённые файлы (`files_touched` 4 → 3, расширение allowlist — A-01):

- `src/DeepSpaceSaga.Client/Data/Locale/English.json`, `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` — значения `Finance.PassengerPayout`/`Finance.PassengerPenalty`: «Contract payouts»/«Contract penalties», «Выплаты по контрактам»/«Штрафы по контрактам». Выявленная ошибка представления: колонка voyage ledger содержит выплаты и штрафы всех транспортных контрактов (пассажиры и груз, US-0004 A-04), а отчёт рейса называл её «пассажирской». Ключи не менялись (ключи — контракт локализации, `LocalizationTests`).
- `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0034Us0005Tk0002Tests.cs` — новый: `ContractJournal_SurvivesScreenChanges`, `Settlement_ShowsOnePayout`, `LoadedVoyage_RestoresVisibleDeadline` (каждый RU и EN).
- `ContractsScreen.cs`, `FinanceScreen.cs`, `SkiaWindow.cs` — **без изменений**: проверено, что wiring (`OpenContractsAsync`, `OpenFinanceAsync`, Load/Continue через `CreateSessionFromSave`) уже передаёт подтверждённое состояние; ошибок wiring не найдено.

Что проверяют тесты (без mock транспорта: каждая сессия открыта из файла слота через `LocalGameSessionConnection.CreateFromSaveFile` — тот же путь, что `Program.LocalGameSessionFactory.CreateSessionFromSave`; сохранение — `GameSessionHandle.SaveAsync` в тот же слот; экраны собраны как в `SkiaWindow`; полёт — команды движка между слотами):

- `ContractJournal_SurvivesScreenChanges` — срочный и обычный заказы приняты кнопками экрана (2 команды, подтверждённая посадка из snapshot); уход на Finance и повторное открытие Contracts показывают тот же журнал, каюты и условия без повторной отправки; после Save в слот и Load — то же отображение, 0 команд, принятые предложения не возвращаются на доску.
- `Settlement_ShowsOnePayout` — слот сохранён с открытым диалогом стыковки; игрок завершает диалог через сессию, выплата приходит тиковым переходом (новая ревизия). Подтверждённая на устаревшем виде отмена получает `stale_revision`: показан локализованный текст (без кода), ничего не списано и не отменено, экран свободен; с актуальным snapshot игрок сразу принимает заказ станции. Результат доставки, строка журнала и строка отчёта рейса («Выплаты по контрактам») показывают одну выплату; повторные открытия экранов и новые snapshot не меняют баланс (toolbar = engine); после Save/Load — тот же баланс, результат, журнал и строка финансов.
- `LoadedVoyage_RestoresVisibleDeadline` — игра, загруженная в полёте: доски нет (`InFlight`), журнал и строки пассажиров совпадают с engine (сроки `Time(deadline)`, состояния), у обоих заказов есть условия отмены, баланс engine. После срока срочного заказа загруженная игра показывает исход «просрочен», штраф и что пассажиры остаются до стыковки; отменить можно только обычный заказ.

Проверки (артефакты `D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0005-tk0002/`):

- red — `--filter FullyQualifiedName~DemoEp0034Us0005Tk0002Tests` → 4 passed / 2 failed: `Settlement_ShowsOnePayout` RU/EN, подпись «Пассажирские выплаты»/«Passenger payout» вместо контрактной (`client-red.log`, `client-red.trx`).
- green — тот же фильтр → 6/6 passed (`client-green.log`, `client-green.trx`).
- быстрая регрессия `FullyQualifiedName~DemoEp0034|~Localization|~Finance` (Client) → 112 passed / 3 failed; 3 — `LocalizationCoverageTests` из известного baseline (входят в `us0004-20261117/story/client-failed.txt`) (`client-ep0034.log`, `client-ep0034.trx`).
- `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release` → 0 warnings / 0 errors (`build-client.log`); `dotnet format ... --verify-no-changes --include` теста → exit 0 (`format.log`); `git diff --check` чист.

Допущения:

- A-01 (allowlist): добавлены два файла локализации (значения двух строк в каждом, ключи и форматы не менялись; общий файл с EP-0033 — правка точечная). Экранные файлы allowlist не изменялись.
- A-02 (technical): опечатанный груз в клиентском пути этого тикета — принятие заказа станции после выплаты; доставка/возврат груза на экранах проверены в US-0004 TK-0003, сквозной рейс с грузом и Save/Load — в TK-0001 (engine). Свой товар — стартовый груз трюма (строка трюма на экране).
- A-03 (stale_revision, риск US-0003): переходы в тике (теперь и выплата пассажирам, TK-0001 A-01) меняют ревизию; команда с прежней ревизией получает `stale_revision`. Клиент не застревает: ответ снимает ожидание, текст `Demo.Jobs.Reason.stale_revision` / `Demo.TransportFailures.Reason.stale_revision` объясняет, что делать, каждый клик — новая идентичность команды, повтор с актуальным snapshot проходит. Автоповтор не добавлялся (решение принимает игрок, повтор не должен скрыто списывать штраф).
- Native checklist записан в карточке истории; **NOT RUN** (нет нативной сессии с проверкой layout на меньшем мониторе).
