---
epic: EP-0001-trading-system
story: EP-0001-US-0006-repeatable-trading-voyage
ticket: EP-0001-US-0006-TK-0002-trade-visit-context
title: Котировка и выбор в контексте текущей стоянки
stage: approved
layer: client
depends_on: [EP-0001-US-0006-TK-0001-round-trip-engine-proof]
files_touched: 3
serves: [AC-02, AC-03, AC-05]
created: 2026-09-21T11:05:34Z
revision: 1
---

# Котировка и выбор в контексте текущей стоянки

## Why

При A → B → A совпадения StationObjectId недостаточно для принятия запоздавшего quote A. Окно прежней стоянки должно стать недействительным; новый Trade получает свежие cargo/баланс и требует нового подтверждения, сохраняя session history.

## Decisions

Отдельных решений пользователя нет; точный исходный запрос находится в story. Новый UI и перерасчёт цены не запрошены.

## Assumptions

- US-0003/TK-0004 уже вводит request generation/cancellation; расширить или переиспользовать её, не создавать второй параллельный quote controller. Если требуемое поведение уже реализовано dependency, production diff минимален, основным результатом становятся regressions.
- Один TradeScreen привязан к одной стоянке. После её потери он блокируется до удаления TK-0003; прибытие открывает новый экземпляр.
- Query/filter/sort допустимо сохранять. Selection/quantity/quote и transient controls не переходят на новый рынок. История подтверждённых операций остаётся в GameSessionHandle.Trades.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs | Refresh:108–142 проецирует DockedStationTrade; journal:59–81. Prerequisite US-0003/TK-0004 добавляет authoritative quote state | Вычисление валидной station identity, visit epoch, сброс выбора/quote; сохранять journal semantics |
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.cs | Refresh:32–42, OnActivated/OnDeactivated:72–78, Submit:138–145 | Связь lifetime окна с контекстом, отмена quote, проверка перед отправкой, сброс transient controls |
| tests/DeepSpaceSaga.Client.Tests/TradeVisitContextTests.cs | Новый файл; существующие fixtures в TradeScreenTests/TradeUxTests, GameSessionHandle.Trades:87 | Управляемые async quote responses, A/null/B/A, late receipts и session replacement |

Matching test project: `D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj`.

## Public API after the change

No public API change. В существующих файлах добавить internal consumer seam:
- `static string? TradeModel.ResolveLocalStationId(AuthoritativeSnapshot? snapshot)`;
- `long TradeModel.VisitEpoch { get; }`, `string? TradeModel.LocalStationObjectId { get; }`;
- `string? TradeScreen.OpenedForStationObjectId { get; }` и `bool TradeScreen.HasValidVisit { get; }` для TK-0003.

ResolveLocalStationId возвращает ID только если найден живой player по PlayerShipObjectId, он IsDocked, его DockedStationObjectId непустой и равен DockedStationTrade.StationObjectId, ActiveVoyage отсутствует либо State=Docked. Иначе null. ActiveVoyage — prerequisite US-0014 из story, не правка Contracts этим тикетом.

Quote API из US-0003/0015: GetTradeQuoteAsync(TradeQuoteRequest) → TradeQuoteSnapshot с RequestId,QuoteId,MarketRevision,StationObjectId,ObjectId,ModuleId,CommandType,ItemTypeId,RequestedQuantity. Receipt: CommandResult.TradeReceipt с StationObjectId/ItemTypeId/ExecutedQuantity/TotalCredits. Client не изменяет это API.

## Implementation steps

1. В TradeModel.Refresh определить локальный контекст до обработки rows. При изменении station ID или PortFees.FirstPortFeeGameTimeMs, включая null, увеличить checked VisitEpoch и инвалидировать активную quote generation; сбросить SelectedItemId, Quantity=1 и предыдущий selected module. При null очистить rows/quote/доступность действий. Cargo и PlayerCredits не кэшировать как результат предыдущей поездки: брать новый snapshot. Повтор того же snapshot/изменение stock не считается новой стоянкой, но revision всё равно инвалидирует quote по US-0003.
2. При создании TradeScreen сначала Refresh Model, затем зафиксировать handle identity, OpenedForStationObjectId, timestamp PortFees.FirstPortFeeGameTimeMs и initial epoch. HasValidVisit читает latest snapshot и требует того же handle, живого локального контекста, неизменного timestamp начала стоянки и initial epoch; после потери стоянки старый экземпляр навсегда invalid. OnDeactivated инвалидирует pending quote activation generation; OnActivated может запросить новую quote только при всё ещё валидной исходной стоянке. Никакой ответ старого экземпляра не меняет новый экран.
3. Freshness tuple включает handle identity, timestamp начала стоянки, visit epoch, activation/request generation, station ID, revision и selection(module/item/direction/quantity). Captured tuple сравнить с актуальным в Refresh на UI path, прежде применения результата. Не блокировать render loop ожиданием Task. Late A response отклоняется после null/B/A даже если station ID и revision снова совпали. Cancel/dispose не удаляет запись отправленной сделки: receipt всё равно обрабатывается session journal.
4. При invalid visit немедленно очистить quantity text/selection/dropdown/drag/focus и блокировать Confirm/Max/request quote; использовать существующий TradeUX.NotDocked. Старые строки не показывать как активный рынок. Перед Submit заново Refresh и HasValidVisit; no optimistic cargo/credits. Новый экран B начинает с пустого выбора и quantity1, новый ответ quote сам ничего не исполняет.
5. Историю не очищать при уходе/прибытии. Запоздалый результат подтверждённой команды A связывать только по CommandId с существующей entry, используя её receipt; не менять current selection/quote B и не переносить число из старой UnitPrice. Pending entry завершается даже если её исходный экран закрыт. Ограничение50 entries и dedupe из US-0003 сохранить.
6. Добавить управляемый IGameSessionConnection fake внутри test file: TaskCompletionSource для quote, snapshot stream, список отправленных команд. Проверить A quote request → departure → B → return A → completion старого request; число отправленных commands не растёт без нового confirm. Проверить новый GameSessionHandle с теми же object IDs: ответы старой сессии не попадают в новый экран/journal.

## Out of scope

GameSessionHandle/transport/Contracts edits, quote math, authoritative trade validation, новый ledger, Render/layout/locale files, Undock state machine, сохранение UI history между разными sessions. Закрытие окон и pause/resume — TK-0003.

## Invariants

- Engine authoritative pricing/receipts: US-0003 story Required consumer contract и TK-0001-trade-execution-contract; запрещён fallback unitPrice*quantity.
- Journal на session handle: GameSessionHandle.cs:87; lifetime окна не является lifetime сделки.
- Snapshot-only Client: Documentation/00-Process/CLAUDE.md:93–101. Render не вызывает Engine напрямую.
- NotDocked строка уже существует: Data/Locale/English.json:31; не нужна новая локализация.

## Tests

TradeVisitContextTests:
- `Departure_clears_market_selection_quote_and_confirm` — AC-02/05.
- `Destination_uses_current_cargo_credits_and_requires_new_confirmation` — AC-03/05.
- `Old_A_quote_cannot_become_current_after_A_B_A` — AC-02, те же station/revision значения намеренно; вариант без промежуточных B snapshots различает визиты по FirstPortFeeGameTimeMs.
- `Deactivated_screen_and_old_session_cannot_apply_quote_completion` — AC-02/05.
- `Late_origin_receipt_updates_history_once_without_replacing_destination_quote` — AC-03/05.
- `Submit_rechecks_visit_after_snapshot_changes` — AC-02.
- `Same_station_refresh_preserves_filters_without_starting_duplicate_requests` — AC-05.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TradeVisitContextTests|FullyQualifiedName~TradeScreenTests|FullyQualifiedName~TradeUxTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Steps выполнены только в трёх разрешённых файлах; US-0003 freshness переиспользована, не заменена локальным расчётом.
- Все served criteria покрыты named tests, включая A/null/B/A и late receipt.
- Tests/build/format проходят либо конкретные исходные failures записаны отдельно.
- API/invariants/out-of-scope соблюдены; нет скрытых files, optimistic balances или незаписанных assumptions.
- Result проверяется по disabled controls, отсутствию лишних commands и сохранённой history; блокирующих вопросов нет.

## Self-containment check

Exact consumer properties, validity predicate, epoch/lifetime правила, reset semantics и async races заданы. Внешний quote API уже определён dependencies; отдельный transport/helper файл не требуется. Window-level transition использует два internal properties в TK-0003.
