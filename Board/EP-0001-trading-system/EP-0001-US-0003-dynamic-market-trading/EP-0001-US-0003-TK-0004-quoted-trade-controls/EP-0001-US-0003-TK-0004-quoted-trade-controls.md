---
epic: EP-0001-trading-system
story: EP-0001-US-0003-dynamic-market-trading
ticket: EP-0001-US-0003-TK-0004-quoted-trade-controls
title: Authoritative preview, Max и отправка
stage: approved
layer: client
depends_on: [EP-0001-US-0003-TK-0001-trade-execution-contract, EP-0001-US-0003-TK-0002-atomic-quote-execution, EP-0001-US-0003-TK-0003-trade-result-texts]
files_touched: 5
serves: [AC-03, AC-04, AC-05, AC-06]
created: 2026-09-21T09:33:31Z
revision: 1
---

# Authoritative preview, Max и отправка

## Why

Текущий preview умножает unit price, поэтому не может подтвердить последовательную котировку партии. End state: существующие controls показывают server quote, устаревший async response не меняет выбор, команда отправляет ровно показанные QuoteId/revision/quantity.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj. Пути от D:/DeepSpaceSaga/DSS. Prerequisite US-0015 обеспечивает quote DTO/async transport; не добавлять LocalClient файлы здесь.

## Decisions

2026-09-21T09:33:31Z — сообщение: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0003-dynamic-market-trading\EP-0001-US-0003-dynamic-market-trading.md». Других явных решений пользователя нет.

## Assumptions

Нет legacy price fallback в новом UI. Пока quote загружается/ошибочен, подтверждение отключено. Обновление quote не является отправкой сделки. Partial Sell показывает requested и executable отдельно, не уменьшает input молча. Journal остаётся per-session в GameSessionHandle.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/GameSessionHandle.cs | :87 Trades; :161–184 SendTradeCommand/fire-and-forget | Pass-through async quote request и quoted send overload; не менять остальные transports |
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs | :9–55 local price math; :61–89 journal; :92+ selection/cache | Quote state/projection/key, stop local total/Max calculation, binding metadata в pending Entry |
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.cs | :25 CanConfirm, :32 Refresh; :68 slider; :123–145 quantity/submit | Nonblocking request lifecycle, freshness guard, exact binding submit, stale refresh без resubmit |
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.Render.cs | :127–159 preview/confirm, :184–194 Reason | Preview executable quantity и authoritative total/Max, loading/partial/stale messages; history calculation оставлена TK-0005 |
| tests/DeepSpaceSaga.Client.Tests/TradeUxTests.cs | :94–126 Fixture/RecordingConnection; :128+ input tests | Controlled async quote responses, selection races, Max/preset/Enter/double-submit tests |

## Public API after the change

GameSessionHandle:
```csharp
public ValueTask<TradeQuoteSnapshot> GetTradeQuoteAsync(TradeQuoteRequest request,
    CancellationToken cancellationToken = default);
public string SendTradeCommand(string objectId, string moduleId, string commandType,
    string itemTypeId, long quantity, string quoteId, long marketRevision,
    CancellationToken cancellationToken = default);
```
Existing overload сохраняется для старых callers/tests, TradeScreen использует только новый. PlayerCommand fields QuoteId/MarketRevision — TK-0001.

Internal TradeModel методы `ApplyQuote(TradeQuoteSnapshot quote)`, `InvalidateQuote(string reasonKey)`, property `TradeQuoteSnapshot? AuthoritativeQuote`. Local TradeQuote сохраняет existing display fields Maximum/Total/CargoQuantity/AmountBefore/After/BalanceAfter/DisabledReason/LimitReason, добавляет `long ExecutableQuantity = 0` в конец. Maximum/Total/ExecutableQuantity берутся из server quote. Journal.Entry дополняется optional QuoteId/MarketRevision/QuotedTotalCredits, существующие constructor args остаются совместимыми.

Dependency DTO полный набор:
`TradeQuoteRequest(RequestId,ObjectId,ModuleId,CommandType,ItemTypeId,Quantity)`;
`TradeQuoteSnapshot(RequestId,QuoteId,MarketRevision,StationObjectId,ObjectId,ModuleId,CommandType,ItemTypeId,RequestedQuantity,ExecutableQuantity,MaximumQuantity,TotalCredits,Curve,DisabledReason,LimitReasons)`;
`IGameSessionConnection.GetTradeQuoteAsync(request,cancellationToken)`.
US-0015 также предоставляет optional `long? StationTradeSnapshot.MarketRevision` (null legacy snapshot) для обнаружения обновлённого рынка; это prerequisite seam, не изменение Contracts в этом тикете.

## Implementation steps

1. Добавить pass-through GetTradeQuoteAsync без синхронного ожидания/пересчёта. Quote request failure возвращается вызывающему UI; не превращать каждую отмену старого quote в фатальный Fail всей сессии. Новый SendTradeCommand overload генерирует один CommandId, сохраняет quote fields и передаёт существующему SendTradeAsync. Не генерировать новый ID при транспортном повторе уже отправленного command.
2. TradeModel перестаёт выводить Max через money/unitPrice и Total через quantity*unitPrice. Display amount/balance можно вычислять checked из snapshot + authoritative executable/total, не применять свою цену или дополнительный clamp. Ошибка переполнения/несогласованный DTO отключает confirm InvalidData/ValueOverflow. Fuel tab остаётся Refuel-only, cargo tabs исключают item.fuel.
3. В TradeScreen держать monotonic request generation + selection key (station ID/revision, ship,module,mode,item,quantity,player credits, выбранный cargo quantity/free capacity или tank amount/capacity, module availability). При изменении key отменить прежний request, убрать его quote и отправить один новый; одинаковые render/refresh без смены key не создают запросов. RequestId уникален на generation. Null station/module/item => no request, confirm disabled. Quote с RequestedQuantity, binding или RequestId не от текущего key игнорируется.
4. Асинхронное завершение не мутирует Model/collections с фонового потока. Хранить task/result и применять завершённый текущий результат в существующем Refresh на UI path; не вызывать .Wait/.Result для незавершённой Task в render loop. Cancellation/закрытие/смена станции делает старую generation неактивной. Ошибка показывает QuoteUnavailable, не отправляет trade. Повторный осознанный выбор/quantity edit или переоткрытие запускает новый quote; infinite retry per-frame запрещён.
5. Max/slider/presets используют последний валидный MaximumQuantity. Изменение количества инвалидирует предыдущий quote и требует нового exact-quantity response перед подтверждением. Сохранять step1, текстовый ввод, search/filter/sort и module selector. Если UI получает zero/negative user quantity, confirm отключён; выбор item начинает с1, значит Max доступен после первой quote. Quote Request с invalid quantity может вернуть DisabledReason, но не исполняется.
6. Перед Submit снова Refresh и полная проверка binding/freshness. Отправить Model.Quantity как requested и показанный QuoteId/revision. Записать pending entry ровно один раз; existing IsPending и Enter key-repeat guard исключают повторный submit. Не отправлять ExecutableQuantity вместо requested: server receipt обязан помнить исходный объём.
7. Preview/confirm: Total=quote.TotalCredits, Max=quote.MaximumQuantity, BalanceAfter и cargo/tank after основаны на ExecutableQuantity. Partial Sell оставляет requested input и показывает TradeUX.PartialPreview(actual,requested,total), confirm указывает actual. Buy/Refuel с executable!=requested запрещены. Reason mapping: station_budget→StationBudgetLimit, station_capacity→StationCapacityLimit, money/stock/cargo_capacity/tank_capacity/cargo/balance_overflow→существующие MoneyLimit/StockLimit/CapacityLimit/TankLimit/CargoLimit/BalanceLimit. New result codes QuoteRequired/StaleQuote/InvalidQuote/FuelTradeForbidden используют TK-0003 keys.
8. При StaleQuote terminal result clear pending, invalidate quote, запросить свежий один раз по текущему выбору и показать QuoteStale. Не resubmit; новый клик необходим. При выборе другой станции очистить selection quote. Закрытие экрана не удаляет session journal; начатый send завершается через Handle, а quote request можно отменить. Modal pause/CloseTrade/Station toolbar flow не менять.

## Out of scope

Цена/spread, transport implementation в LocalClient, Contracts edits, locale JSON (TK-0003), Finance ledger, новый экран, Station navigation/pause mechanics. Success/history receipt rendering — TK-0005.

## Invariants

- Client использует async boundary, render не обращается к Engine: Documentation/00-Process/CLAUDE.md:22–56.
- Existing nonblocking send: GameSessionHandle.cs:161–184.
- Input/close paths сохранены: TradeScreen.cs:84–145. Модальная пауза — EngineRequirements.md:1121–1133.
- Hidden Credits не нужны: StationTradeSnapshot.cs:17–23.

## Tests

В TradeUxTests обновить прежние local-price ожидания на server quote fixtures; fake DTO имеет curve totals, отличные от first-price*quantity:
- `Preview_and_max_use_server_quote_without_local_price_multiplication` — AC-05.
- `Partial_sell_preview_uses_actual_quantity_and_total_but_sends_requested` — AC-05/06.
- `Changing_quantity_module_mode_or_station_invalidates_quote` и `Out_of_order_quote_response_is_ignored` — AC-04/05.
- `Max_slider_presets_and_typed_quantity_require_matching_quote_before_submit` — AC-05/06.
- `Loading_failed_cancelled_and_stale_quotes_cannot_confirm` — AC-04/05.
- `Stale_result_refreshes_once_without_automatic_resubmit` — AC-04/05.
- `Enter_repeat_and_double_click_send_one_command_with_shown_binding` — AC-04.
- `Fuel_tab_only_sends_refuel_and_uses_tank_quantity` — AC-03.
- `Closing_screen_cancels_quote_without_erasing_pending_trade` — AC-06.
Не использовать sleeps для race: управляемые TaskCompletionSource, fake snapshot через SnapshotBuffer.Update(AuthoritativeSnapshot).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TradeUxTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps реализованы в пяти файлах; каждый served AC покрыт перечисленными tests.
- Нет local price/Max fallback; exact binding между preview и отправкой доказан, stale не resubmits.
- Named tests/build/format проходят либо исходные failures явно записаны.
- API, modal/input invariants и scope сохранены; нет скрытых dependency changes или незаписанных assumptions.
- Existing Trade можно проверить controlled async test; receipt display заканчивается отдельным TK-0005.

## Self-containment check

Полные quote поля, request/selection lifecycle, async safety, new helper signatures, reason mapping и sender semantics приведены. Quote transport и market revision property — явно названный prerequisite US-0015, не работа за пределами разрешённого layer.
