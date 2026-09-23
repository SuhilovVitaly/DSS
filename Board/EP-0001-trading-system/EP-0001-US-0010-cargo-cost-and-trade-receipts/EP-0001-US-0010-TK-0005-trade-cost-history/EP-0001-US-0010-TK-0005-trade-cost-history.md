---
epic: EP-0001-trading-system
story: EP-0001-US-0010-cargo-cost-and-trade-receipts
ticket: EP-0001-US-0010-TK-0005-trade-cost-history
title: Проверяемый результат в Trade history
stage: approved
layer: client
depends_on: [EP-0001-US-0010-TK-0001-cargo-result-contract, EP-0001-US-0010-TK-0003-weighted-cost-accounting, EP-0001-US-0010-TK-0004-cargo-result-texts, EP-0001-US-0003-TK-0005-confirmed-trade-history]
files_touched: 3
serves: [AC-02, AC-06, AC-07]
created: 2026-09-21T12:39:58Z
revision: 1
---

# Проверяемый результат в Trade history

## Why

Игроку нужен результат фактически исполненной продажи, а не оценка по текущей цене и не прибыль по непроданному остатку. End state: существующий Trade status/history показывает purchase cost или sale proceeds, realized cost и gross result из одного authoritative receipt; partial/replay/legacy unknown видны корректно.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. US-0003 TK-0005 заранее заменяет cached multiplication на receipt total и делает Track idempotent; этот тикет расширяет тот же formatter.

## Decisions

2026-09-21T12:39:58Z — пользователь запросил tickets US-0010. Отдельного запроса менять layout/Finance не было.

## Assumptions

Bounded per-session journal50 остаётся. Buy показывает `TotalCredits` как purchase cost без gross result; Refuel использует существующий result. Sell показывает gross только когда оба новые receipt fields присутствуют и согласованы. Null/mismatch — explicit unavailable, не fallback calculation.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs | :58–83 journal/entry; prerequisite US-0003 добавляет idempotent receipt binding | Проецировать immutable cargo-result fields, не хранить/считать mutable unit cost |
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.Render.cs | :165–173 сейчас quantity×unit price; prerequisite делает receipt-only formatter; :192–206 history list | Format Buy/Sell/partial/unknown из receipt и новых locale keys в существующем layout |
| tests/DeepSpaceSaga.Client.Tests/TradeScreenTests.cs | Existing screen/history fixtures; US-0003 добавляет real quote→receipt integration | Known/unknown/partial/loss/replay/reopen and no-unrealized-profit regressions |

## Public API after the change

No public API change. Используется internal formatter seam prerequisite US-0003 (`EntryMessage` internal или эквивалент в том же Render file). Source of truth:

- Buy: `receipt.ExecutedQuantity`, `receipt.TotalCredits` → localized purchase cost.
- Sell known: actual/requested, `TotalCredits` proceeds, `RealizedCargoCostCredits`, `GrossResultCredits`.
- Sell unknown: actual/requested, proceeds, localized unavailable; no numeric result.
- Refuel: existing receipt total/quantity message; cargo cost fields игнорируются.

Before formatting known Sell проверяется `gross == checked(total - cost)` и `cost>=0`; mismatch показывает unavailable/error state, но не вычисляет replacement gross.

## Implementation steps

1. Сохранить prerequisite binding checks по CommandId/type/item/quote/requested. Не сравнивать historical receipt с текущей station/quote/price.
2. Buy success показывает actual quantity и exact `TotalCredits` как purchase cost. Не отображать покупку как negative profit и не включать remaining inventory value.
3. Sell known full/partial выбирает соответствующий TK-0004 template и показывает actual/requested, proceeds, realized cost, signed gross. Partial использует только executed prefix receipt.
4. Sell null/invalid cost fields показывает proceeds и localized `CargoCostUnavailable`; не подставляет zero, cached `UnitPrice`, base price или current market price.
5. Refuel/rejection/pending сохраняют prerequisite behavior. New cargo result fields на неправильном command type считаются mismatched receipt и не показываются как финансовый success.
6. Closing/reopening использует общий handle journal; duplicate Track/result не добавляет строку и не меняет ранее сформированный message. Retention50 не расширять.
7. В существующем test file добавить pure formatter cases и один real Engine integration: две покупки с разными quoted totals → partial Sell → save/load/replay. Сверить UI числа с receipt/save remaining basis; не читать Engine internals в production Client.
8. Проверить, что unsold quantity и его remaining basis отсутствуют в sale gross line; последующая продажа показывает только новую realized долю.

## Out of scope

Finance screen, voyage net profit, unlimited history, graphs/tooltips/new screen, Engine arithmetic, locale edits вне TK-0004, изменение quote/market behavior.

## Invariants

- Journal bounded50 и refresh path: `TradeModel.cs:58–83`.
- Current incorrect cached multiplication находится в `TradeScreen.Render.cs:165–173`; prerequisite удаляет его, US-0010 не возвращает fallback.
- Client production не ссылается на Engine: `Documentation/00-Process/CLAUDE.md:22–56`.
- Нереализованный груз не является прибылью; route expenses принадлежат US-0011.

## Tests

- `Buy_history_labels_receipt_total_as_purchase_cost_without_profit` — AC-07.
- `Known_sell_history_shows_actual_requested_proceeds_realized_cost_and_signed_gross` — AC-02/07.
- `Partial_sell_excludes_remaining_quantity_and_basis_from_gross_result` — AC-02/07.
- `Unknown_or_mismatched_cost_receipt_shows_unavailable_and_never_zero_profit` — AC-07.
- `Duplicate_and_reopened_history_keep_one_immutable_cargo_result` — AC-06.
- `Real_two_price_purchases_partial_sell_save_reload_and_replay_match_receipt_and_remaining_basis` — AC-02/06/07.
- `Refuel_rejection_pending_and_existing_trade_navigation_are_unchanged` — regression.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TradeScreenTests|FullyQualifiedName~TradeUxTests|FullyQualifiedName~LocalizationTests"
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~CargoCostAccountingTests|FullyQualifiedName~QuotedTradeExecutionTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps выполнены только в трёх разрешённых files; prerequisite receipt/history APIs присутствуют.
- AC-02/06/07 покрыты named unit и real integration tests.
- Tests/build/format проходят либо конкретный baseline failure записан отдельно.
- Confirmed display использует только immutable receipt; unknown/mismatch не превращается в число.
- Existing Trade layout/navigation/retention сохранены; Finance/voyage scope не затронут.

## Self-containment check

Mapping command→fields→templates, validation, partial/unknown/replay behavior и integration fixture outcome заданы. Implementer не должен искать финансовую формулу или менять другие screens.
