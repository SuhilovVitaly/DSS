---
epic: EP-0001-trading-system
story: EP-0001-US-0003-dynamic-market-trading
title: Атомарное исполнение сделки в существующем Trade
stage: approved
dependencies: [EP-0001-US-0001-station-market-profiles, EP-0001-US-0002-market-replenishment, EP-0001-US-0015-authoritative-market-quotes]
created: 2026-09-21T09:33:31Z
source_request: "сделай тикеты для D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0003-dynamic-market-trading\\EP-0001-US-0003-dynamic-market-trading.md"
current_review: complete
revision: 1
---

# Атомарное исполнение сделки в существующем Trade

## Входное техническое задание

Точное сообщение пользователя: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0003-dynamic-market-trading\EP-0001-US-0003-dynamic-market-trading.md». Время фиксации в grounding: 2026-09-21T09:33:31Z UTC, не утверждение о времени отправки. Выбор US-0003 явный; незавершённая US-0004 не продолжается.

Исходные ссылки сохранены: Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md:98–121,175–205; TradingSystemMvpStories.md:189–225,255–290,451–457,483–487 в той же папке. Продуктовые решения: ../Documentation.md:93–100,141,153. Округление: Documentation/01-Requirements/EngineRequirements.md:5263–5265. Все относительные paths ниже — от D:/DeepSpaceSaga/DSS.

## User story

Как игрок, я хочу выполнить покупку, продажу или заправку в существующем Trade, не потеряв деньги или груз из-за частично ошибочной операции. Экран использует authoritative котировку и Max, ограниченный деньгами, вместимостью, stock, budget и свободным складом. Продажа может вернуть фактически исполненное целое количество; покупка и Refuel выполняются полностью либо отклоняются без списаний. Preview, команда и подтверждённый результат связаны QuoteId/MarketRevision. Устаревшая котировка требует обновления и нового подтверждения, а повтор команды не создаёт вторую сделку. Fuel остаётся услугой заправки без продажи топлива из бака.

## Acceptance criteria

- AC-01: Buy по свежей котировке переносит весь запрошенный груз и точную сумму curve; нехватка денег/stock/capacity, ошибка ссылки или overflow оставляют деньги, груз, склад и revision неизменными.
- AC-02: Sell исполняет максимальный целый префикс requested quantity, который допускают budget и stock capacity. Cargo должен содержать весь запрос. Receipt показывает requested/executed/total и причины ограничения; при нулевом fill нет изменений.
- AC-03: Refuel — all-or-nothing, item.fuel только в бак указанного engine module, не cargo; Buy/Sell Fuel и Refuel другого item отклоняются. Топливо из бака не продаётся.
- AC-04: Quote bound к станции/кораблю/модулю/item/направлению/количеству/revision. Stale/подмена/повтор не меняют состояние; два запроса по одной revision не могут исполниться оба. Дубликат CommandId в журнале возвращает исходный receipt, включая после Save/Load; старый quote не исполняется после вытеснения receipt.
- AC-05: Existing Trade preview, Max и confirm используют authoritative quote, включая точную сумму и ожидаемый partial fill. Ответ на старую selection/quantity не подменяет текущий. После stale данные обновляются, повторного исполнения без нового подтверждения нет.
- AC-06: History/status выводят фактическую сумму и quantity из receipt, сохраняются при закрытии/повторном открытии окна. Сохраняются quantity controls, поиск, фильтры, модальная пауза и возврат на Station; RU/EN сообщения объясняют stale, partial и лимиты.
- AC-07: Интеграционные проверки используют реальные quote calculator и execution: сумма receipt равна quote prefix, немедленная Buy→Sell той же партии не увеличивает баланс, reload/retry не дублирует эффекты. Profile snapshot не раскрывает скрытые Credits станции.

## Non-goals

Расчёт price formula/spread/curve и выдача котировок — US-0015; профиль/почасовая экономика — US-0001/0002. Не добавлять новые окна, события, удалённые цены, рейсы, расход voyage fuel, cost-basis/Finance ledger (US-0010/0011), полный архив сделок или schema migrations. StoryBuilder создаёт документы, не реализует тикеты.

## Dependencies

US-0001 предоставляет профильный рынок и Fuel storage semantics. US-0002 предоставляет MarketBudgetCredits, max stock/free capacity и безопасное сохранение этих величин. US-0015 предоставляет issuer/cache/validation котировок, immutable market revision, curve, authoritative Max и транспорт IGameSessionConnection/LocalClient. Сначала реализуются эти зависимости; approved planning не означает готовый production API.

### Integration contract required from US-0015 (assumption A-01)

US-0015 сейчас draft и не имеет конкретного API. Следующий точный consumer contract — записанное допущение US-0003, не утверждение об уже реализованном API и не изменение story US-0015. Перед исполнением тикетов prerequisite должен ему соответствовать; несовпадение возвращается в review, не устраняется скрытыми файлами US-0003.

Contracts namespace DeepSpaceSaga.Contracts:
```csharp
public sealed record TradeQuoteRequest(string RequestId, string ObjectId,
    string ModuleId, string CommandType, string ItemTypeId, long Quantity);
public sealed record TradePriceStep(long Quantity, long UnitPriceCredits);
public sealed record TradeQuoteSnapshot(string RequestId, string QuoteId,
    long MarketRevision, string StationObjectId, string ObjectId, string ModuleId,
    string CommandType, string ItemTypeId, long RequestedQuantity,
    long ExecutableQuantity, long MaximumQuantity, long TotalCredits,
    ImmutableArray<TradePriceStep> Curve, string? DisabledReason,
    ImmutableArray<string> LimitReasons);
ValueTask<TradeQuoteSnapshot> IGameSessionConnection.GetTradeQuoteAsync(
    TradeQuoteRequest request, CancellationToken cancellationToken = default);
```
Curve — последовательные run-length segments для исполнимого префикса, Quantity>0, UnitPriceCredits>=0; сумма Quantity=ExecutableQuantity, checked sum(quantity*unitPrice)=TotalCredits. MaximumQuantity — наибольший выполнимый объём по всем ограничениям для выбранных модуля/направления, независимо от requested quantity. Invalid request имеет DisabledReason, executable=0,total=0; при допустимом partial Sell DisabledReason=null, LimitReasons объясняют остаток. LimitReason values: money, stock, cargo_capacity, tank_capacity, station_budget, station_capacity, cargo, balance_overflow. Никаких скрытых Credits в DTO.

В Engine partial SimulationEngine US-0015 предоставляет `public TradeQuoteSnapshot GetTradeQuote(TradeQuoteRequest request)` и `private bool TryValidateTradeQuote(PlayerCommand command, out TradeQuoteSnapshot quote, out string reasonCode)`. Validator не мутирует мир, проверяет issued token и весь binding/current player/module state/revision; не доверяет сумме от клиента. `private long NextMarketRevision(string stationObjectId)` возвращает checked следующий revision без мутации; `private void CommitMarketRevision(string stationObjectId, long nextRevision)` присваивает подготовленное значение и инвалидирует выданные котировки, не выполняя новый fallible расчёт. Эти вызовы выполняются под существующей Engine world lock вместе со сделкой. Изменения US-0002 stock/budget/event/interval также инвалидируют quotes, как требует US-0015.

US-0015 предоставляет optional long? StationTradeSnapshot.MarketRevision для client freshness key. US-0015 обязана хранить revision при save, очищать quote cache на загрузке (незавершённые котировки становятся stale) и не переиспользовать QuoteId между сессиями. Legacy station без profile тоже поддерживает explicit quote через constant-price curve; новое UI всегда использует quote.

## Grounding

- Current PlayerCommand ещё без QuoteId/revision: src/DeepSpaceSaga.Contracts/PlayerCommand.cs:6–50. CommandResult имеет только optional ExecutedQuantity, без денежного receipt: CommandResult.cs:56–73.
- Engine уже stage-before-commit: SimulationEngine.cs:1881–2048. Но суммы пока unitPrice*qty (:1940,:1993,:2026), Sell ограничен средствами (:1988–1998). Item storage/revision guard ещё требует подключения к quote dependency.
- CommandId dedupe до queue: SimulationEngine.cs:130–143. Journal сохраняет целый CommandResult и bounded4096 receipts: SimulationEngine.CommandJournal.cs:8–48. CaptureSaveState переносит receipts: SimulationEngine.cs:811–815.
- Client preview сам вычисляет максимум и unitPrice*quantity: UI/Screens/Trade/TradeModel.cs:9–55; TradeJournal находится в ЭТОМ ЖЕ файле :61–89. TradeScreen.cs:138–145 отправляет команду/запоминает unit price; TradeScreen.Render.cs:164–173 снова умножает цену при показе результата.
- Client send helper: src/DeepSpaceSaga.Client/GameSessionHandle.cs:161–184; transport SendCommandAsync уже общий. Quote transport пока отсутствует в IGameSessionConnection.cs; принадлежит US-0015.
- RU/EN existing result keys: src/DeepSpaceSaga.Client/Data/Locale/Russian.json:76–78 и English.json:76–78. Localization.LoadLocaleFile(string) возвращает словарь; tests/DeepSpaceSaga.Client.Tests/LocalizationTests.cs:14–61.
- Matching engine fixtures: TradeCommandTests.cs:28–36 (commands), :38–76 (CreateEngine); client fixtures — TradeUxTests.cs и TradeScreenTests.cs. Tests не запускались в planning.

## Invariants and assumptions

- A-01: отсутствующий API prerequisite задан выше как explicit interface assumption; формула US-0015 не дублируется в US-0003.
- A-02: New profile markets требуют QuoteId/revision всегда. Старый unquoted путь только у станций без MarketProfileId сохраняется для legacy callers; явные quoted команды, включая legacy station, идут через новый executor. Частично заданная quote metadata никогда не включает fallback. Новый Client fallback не использует.
- A-03: Partial допускается только Sell и только из-за station budget/free stock capacity; запрос больше cargo, invalid quote/module, overflow — полное отклонение. Изменившиеся player resources invalidates quote как stale, не молчаливый иной fill.
- A-04: Новая revision присваивается ровно один раз при successful commit; rejected/stale/replayed не увеличивают revision. 4096 journal retention сохраняется: после eviction старый QuoteId отклоняется stale, исходный receipt уже не гарантирован. Безлимитный ledger — backlog US-0010/0012.
- A-05: Полный immutable execution receipt прикрепляется к CommandResult для quoted success/rejection; non-trade и legacy старые results могут иметь null. Current history bound50 остаётся; client не восстанавливает сумму старых receipts из текущей цены.
- A-06: Все денежные/количественные операции checked long/decimal, midpoint AwayFromZero внутри prerequisite calculator (EngineRequirements:5263–5265). Исполнитель не округляет quote повторно.
- A-07: Нет автоматического resubmit после stale/connection loss. Refresh quote безопасен; исполнение требует нового клика. Повтор транспортной доставки сохраняет исходный CommandId.
- A-08: Client знает Max/LimitReasons, не hidden Credits/budget totals. Новые локализации content-data отделены от client logic.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0003-TK-0001-trade-execution-contract | Quote binding и результат сделки | contracts | US-0015 integration contract | AC-02, AC-04, AC-06 |
| EP-0001-US-0003-TK-0002-atomic-quote-execution | Атомарное исполнение котировки | engine | TK-0001; US-0001/0002/0015 | AC-01, AC-02, AC-03, AC-04, AC-07 |
| EP-0001-US-0003-TK-0003-trade-result-texts | Сообщения котировки и ограничений | content-data | TK-0001 | AC-05, AC-06 |
| EP-0001-US-0003-TK-0004-quoted-trade-controls | Authoritative preview, Max и отправка | client | TK-0001, TK-0002, TK-0003; US-0015 | AC-03, AC-04, AC-05, AC-06 |
| EP-0001-US-0003-TK-0005-confirmed-trade-history | Подтверждённые суммы и partial receipt | client | TK-0004 | AC-02, AC-04, AC-06, AC-07 |

Implementation files: 3 / 5 / 3 / 5 / 3. Безопасный порядок TK-0001 → TK-0002 → TK-0003 → TK-0004 → TK-0005. Каждый тикет имеет matching project и полный bounded Code context.

## Gaps and backlog

- G-01: US-0015 пока draft; concrete quote API и его production files не существуют. Integration contract — prerequisite gate, не скрытая реализация внутри этих пяти тикетов. Несоответствие API требует review, не изменения requirements или соседней story.
- G-02: US-0001/0002 не считать реализованными лишь по approved tickets; учитывать их фактическую готовность перед исполнением.
- G-03: Receipt retention4096 / UI history50 сохранены. Долговременный финансовый ledger и миграции — US-0010/0012.
- G-04: Anti-arbitrage проверяется реальным calculator в TK-0002; если baseline US-0015 даёт прибыль в roundtrip, исправление формулы относится к US-0015, а этот acceptance остаётся непройденным.
- G-05: requirements-engineer отсутствует в доступных/local skills; применяется предоставленный DSS-StoryBuilder workflow.
- Нет блокирующих продуктовых вопросов. Это готовая декомпозиция с явными implementation dependencies, не заявление о готовности quote subsystem.

## Decision and review log

- 2026-09-21T09:33:31Z: точное сообщение и путь записаны выше. Пользователь явно выбрал US-0003; US-0004 оставлена без завершения.
- Grounding: IDs epic/story/file/folder совпадают, ticket-папок нет, первый номер0001. Обязательные требования и архитектура прочитаны, текущий код проверен path:line.
- Plan-review: пять тикетов, A-01…A-08 приняты автоматическим workflow; отдельное подтверждение не требуется.
- Создан TK-0001: binding/receipt, contracts, 3 файла. Следующий TK-0002.
- Создан TK-0002: atomic execution, engine, 5 файлов; prerequisites и retention limits явные. Следующий TK-0003.
- Создан TK-0003: RU/EN quote/receipt messages, content-data, 3 файла. Следующий TK-0004.
- Создан TK-0004: authoritative controls/preview, client, 5 файлов. Добавлен явный prerequisite MarketRevision в snapshot. Следующий TK-0005.
- Создан TK-0005: receipt-only history и интеграционные проверки, client, 3 файла. Все пять тикетов созданы; идёт проверка artifacts.
- Artifact review: исключены неисполняемые варианты fixture и неподдержанный SubmittedQuote; invalid raw command fields допустимы в rejected receipt, поэтому отказ не повреждает последующий Save/Load.
- 2026-09-21T09:49:17Z — validation passed: пять canonical ticket folders/files, metadata IDs и headings; один layer на тикет; file counts 3/5/3/5/3; AC-01…AC-07 покрыты; dependency closure из 13 существующих тикетов без циклов/неразрешённых IDs; whitespace и git diff --check без ошибок.
- Workflow complete: созданы только planning artifacts US-0003. Production-код, epic и requirements этим заданием не изменялись; dotnet build/test/format не запускались, команды указаны для исполнителей. US-0015 остаётся явной внешней prerequisite story без тикетов. Блокирующих вопросов нет.
