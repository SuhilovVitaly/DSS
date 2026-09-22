---
epic: EP-0001-trading-system
story: EP-0001-US-0002-market-replenishment
ticket: EP-0001-US-0002-TK-0005-market-state-trade-ui
title: Обновление и состояния рынка в Trade
stage: approved
layer: client
depends_on: [EP-0001-US-0002-TK-0001-market-stock-snapshot, EP-0001-US-0002-TK-0003-hourly-market-simulation, EP-0001-US-0002-TK-0004-market-flow-content]
files_touched: 3
serves: [AC-03, AC-04, AC-05, AC-06]
created: 2026-09-21T08:59:06Z
revision: 1
---

# Обновление и состояния рынка в Trade

## Why

Игрок должен различать дефицит/норму/избыток, видеть обновлённый запас после ожидания и понимать, почему станция принимает лишь часть груза. UI использует готовые значения Engine; открытый Trade не становится источником экономических tick.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Пути ниже относительны D:/DeepSpaceSaga/DSS. Сначала US-0001 и dependencies этой истории.

## Decisions

D-01, 2026-09-21T08:59:06Z: «сделай следующую стори». Новых требований к стилю/экранам пользователь не добавлял.

## Assumptions

- Не добавлять кнопку ожидания в Trade: обычный ход требует закрытия всех модальных окон; разрешённый ручной TravelStation существует отдельно.
- Состояние берётся из snapshot.StockState, thresholds/flows в Client не дублируются.
- Legacy/Fuel строки с null fields показываются без market badge, target и max.
- При одинаковом ограничении по бюджету и складу UI предпочитает складскую причину; это presentation tie-break, не правило исполнения.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.Render.cs | :57–73 rows, :110–159 detail/preview, :165–173 journal, :184 Reason; US-0001 добавляет units | State/targets в существующих местах, capacity reason и explicit remaining quantity |
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs | :26–31 Sell maximum и generic budget reason; :124–135 existing refresh/filter | Уточнить только источник ограничения Sell по authoritative FreeStockCapacity |
| tests/DeepSpaceSaga.Client.Tests/TradeUxTests.cs | :68–98 fuel/refresh cases; :100 и далее render/recording connection fixtures; расширен US-0001 | Tests rendering/state/refresh/partial results/pause boundary |

## Public API after the change

No API change. Использовать поля TK-0001: TargetStock/MaxStock/FreeStockCapacity/StockState, все nullable; enum Shortage/Normal/Surplus. MaxSellableQuantity уже включает Budget/Storage bound.

Новые internal presentation helpers допускаются только внутри TradeScreen.Render.cs, например:
- internal static string StockStateLabel(StationMarketStockState state);
- internal static string MarketStockSummary(StationInventoryItemSnapshot item).

TK-0004 locale keys:
TradeUX.StockShortage / StockNormal / StockSurplus;
TradeUX.MarketStockSummary "{0} · target/цель {1} · max/максимум {2}";
TradeUX.StationStorageLimit;
TradeUX.PartialWithRemaining "{0}: {1} of/из {3} ... {2} tokens ... remaining/остаток {4}".

Unit formatting берётся из US-0001 TradeItemPresentation.FormatQuantity(itemId,quantity). Новая rejection string Engine "station_stock_full" отображается через L("StationStorageLimit") в существующем Reason switch.

## Implementation steps

1. В row/detail показать короткий localized badge по StockState и строку target/max через MarketStockSummary. Сохранить существующие таблицу, toolbar и места interaction; badge разместить второй строкой имени в текущем 50px row либо в detail-area, не вводить колонку с экономическими расчётами.
2. Всегда оставлять stock актуальным из snapshot; state не вычислять из stockRatio на Client. Цвет может дополнять текст, но не заменять Shortage/Normal/Surplus. Nullable state/targets → блок не показывается; не писать «Норма» для legacy/Fuel.
3. В TradeQuote.Calculate Sell сохранять maximum=min(cargo,item.MaxSellableQuantity) и existing player balance overflow limit. Если cargo<=maxSellable, причина CargoLimit; иначе если FreeStockCapacity.HasValue и FreeStockCapacity.Value<=MaxSellableQuantity, причина StationStorageLimit; иначе StationBudgetLimit. Отрицательные malformed capacity обрабатывать как InvalidData, не расширять maximum.
4. Сохранить существующую отправку/подтверждение Buy/Sell/Refuel, integer step и authoritative completion. Не вводить client cap, отличный от переданного MaxSellableQuantity, и не менять цены.
5. В EntryMessage для executed<requested выводить PartialWithRemaining: remaining=max(0,requested−executed), qty/requested/remaining с unit, total по существующему journal behavior. Не вычислять executed по разнице двух snapshots. Полный success использует прежний ключ; mapped "station_stock_full" объясняет полный склад.
6. На refreshed snapshot обновлять badge/targets/stock и доступный Max; selection/quantity/выбранный cargo module сохраняются по текущим правилам. После replenishment ранее disabled Buy снова может быть подтверждён при достаточных средствах/месте.
7. Прогнать tests и manual smoke обоих языков. Pausing реализован existing SkiaWindow modal flow, production pause code в этом тикете не меняется. Проверить весь сценарий закрытия Trade и Station, ожидания одного часа и повторного открытия.

## Out of scope

Engine/Contracts/content/locales, layout-файлы и новые экраны, клиентская симуляция, изменение clock/модальных правил, цены/quote curve, показ station Budget/Credits. Никаких wall-clock counters в TradeModel.

## Invariants

Engine authority — Documentation/00-Process/CLAUDE.md:22–56. Modal pause — EngineRequirements.md:1121–1133. Trade единицы отдельны от массы — :5128–5160. Existing Render() не должен менять budget/stock, snapshot buffers остаются единственным источником рынка. US-0001 Fuel separation сохраняется (TradeUxTests.cs:68–73).

## Tests

Расширить TradeUxTests существующими fixtures, без дополнительных файлов:

- Market_badges_display_authoritative_states_instead_of_recalculating_ratio (AC-06): все три enum; null→нет badge; намеренно несовпадающий с локально вычисленным ratio fixture доказывает, что клиент показывает переданный state.
- Bounded_market_targets_use_item_units_and_legacy_rows_stay_plain (AC-03/06): rations/cells/blocks, targets/max отдельно от kg.
- Sell_limit_distinguishes_storage_from_budget (AC-03/05): cargo10/maxSellable3/free3→storage; cargo10/maxSellable2/free3→budget; cargo1→cargo; full free0→storage.
- Partial_sell_result_shows_executed_and_remaining_units (AC-05): command requested5, result executed3, журнал явно показывает 3/5 и остаток2, не считает его по stock.
- Replenished_snapshot_reenables_buy_without_changing_selection (AC-06): stock0→positive authoritative snapshot, выбранный ItemId/module сохраняются, Buy отправляет правильные quantity/id.
- Trade_render_does_not_advance_paused_market (AC-04): session-control Speed0 и injected/fake time fixture, серия Render/Refresh без новых authoritative changes сохраняет stock/state. Реальная проверка Engine pause уже в TK-0003; не выдавать UI mock за полную проверку clock.
- Market_reopen_uses_latest_snapshot_after_one_hour (AC-04/06): дать fixture новую authoritative state после явного часа, OnActivated/Render показывает новую строку; no local replay.
- Market_stock_full_rejection_is_localized (AC-03/05).
- Existing Fuel, modal и quantity tests остаются.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TradeUxTests|FullyQualifiedName~ModalPauseTests|FullyQualifiedName~ModalTransitionTests|FullyQualifiedName~MarketFlowContentTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

Manual smoke: Five Station Markets → Mining → Dock dialogue → Trade; записать ice/water stock и state; дождаться real time при открытом Trade — состояния не меняются; закрыть Trade и Station, включить обычную скорость до одного игрового часа, открыть снова — ice162→180 и water36→32 при исходном складе/без сделок, состояния Surplus/Shortage. Повторить с ускорением на новом старте до той же границы. Выполнить Buy одной единицы пополненного товара. Оба языка, badges не перекрывают controls. UI smoke не считается выполненным только потому, что tests/build прошли.

Финальная проверка всей истории после пяти тикетов:

```text
dotnet test D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --no-restore
dotnet build D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены только в разрешённых Code context файлах; лимит files_touched соблюдён.
- Каждый criterion из serves покрыт указанными named tests в пределах вклада тикета.
- Named tests и build/lint соответствующего layer проходят; исходные failures записаны с точной командой и сообщением.
- Public API, invariants, out-of-scope и prerequisite contracts соблюдены.
- Нет незаписанных assumptions, блокирующих вопросов и скрытых изменений за allowlist.
- End state проверяется тестом, diff или описанным наблюдаемым поведением; planning status не означает выполненный production-код.

## Self-containment check

Три файла, DTO/locale/unit contracts, точный tie-break причин, remaining calculation и наблюдаемый smoke описаны. End state: существующий Trade показывает изменившийся рынок и ограничение склада, не продвигая экономику самостоятельно.
