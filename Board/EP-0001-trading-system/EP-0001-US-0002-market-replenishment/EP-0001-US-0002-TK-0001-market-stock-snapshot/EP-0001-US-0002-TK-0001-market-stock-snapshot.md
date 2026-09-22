---
epic: EP-0001-trading-system
story: EP-0001-US-0002-market-replenishment
ticket: EP-0001-US-0002-TK-0001-market-stock-snapshot
title: Состояние и вместимость рынка в snapshot
stage: approved
layer: contracts
depends_on: [EP-0001-US-0001-TK-0005-profile-trade-presentation]
files_touched: 2
serves: [AC-03, AC-05, AC-06]
created: 2026-09-21T08:59:06Z
revision: 1
---

# Состояние и вместимость рынка в snapshot

## Why

Trade должен получить состояние запаса и доступную вместимость от Engine. Контракт позволяет отображать дефицит и складской лимит без клиентской экономики. Этот тикет покрывает wire/data часть AC-03/05/06; вычисления и отображение добавляют TK-0003/0005.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj.
Пути Code context относительны D:/DeepSpaceSaga/DSS.

## Decisions

D-01, 2026-09-21T08:59:06Z: «сделай следующую стори». Пользователь разрешил продолжить planning; дополнительных технических ответов нет.

## Assumptions

- Новые trailing optional fields сохраняют исходные positional constructors и legacy JSON.
- Null означает отсутствие настроенной bounded economy, а не 0/Normal. Fuel и legacy rows имеют null economy-поля.
- Budget остаётся скрытым; MaxSellableQuantity представляет уже ограниченное Engine количество.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs | :23–40 item record без targets/state; :15–21 скрытый budget | Добавить enum и четыре optional поля в конец item record |
| tests/DeepSpaceSaga.Contracts.Tests/StationTradeSnapshotTests.cs | :15–48 roundtrip; :51–65 category default; :69 default arrays | Roundtrip новых полей и совместимость старого JSON/constructors |

## Public API after the change

В том же production-файле добавить:

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<StationMarketStockState>))]
public enum StationMarketStockState { Shortage, Normal, Surplus }
```

В конец StationInventoryItemSnapshot после UnitMassKg:

```csharp
long? TargetStock = null,
long? MaxStock = null,
long? FreeStockCapacity = null,
StationMarketStockState? StockState = null
```

Существующие ItemTypeId, StockQuantity, UnitPriceCredits, MaxSellableQuantity, Category, UnitMassKg и StationTradeSnapshot shape сохраняются. JSON использует существующий serializer naming convention, enum сериализуется строкой.

Producer contract для TK-0003: четыре поля либо все null, либо TargetStock>0, MaxStock=2×TargetStock, 0≤StockQuantity≤MaxStock, FreeStockCapacity=MaxStock−StockQuantity и корректный StockState. DTO не вычисляет state/price и не выполняет validation при конструировании. MaxSellableQuantity ограничен min(available market budget/unit price, FreeStockCapacity) для bounded cargo. Fuel не получает bounded cargo fields.

## Implementation steps

1. Добавить enum и trailing fields; задокументировать единицы (торговые единицы, не масса), null semantics и скрытый бюджет.
2. Уточнить XML comment MaxSellableQuantity: оно ограничивает продажу игрока бюджету и свободному складу; не публикует Credits.
3. Расширить существующие тесты. Не вводить дополнительные dependencies Contracts на Engine или Client.

## Out of scope

Расчёт порогов, Engine projection, UI, сохранения, профили JSON, новые команды/quote API.

## Invariants

Contracts не зависит от Engine/Client (Documentation/00-Process/CLAUDE.md:44–56). Budget не раскрывается (StationTradeSnapshot.cs:15–21). Единица количества отделена от массы (Documentation/01-Requirements/EngineRequirements.md:5128–5160).

## Tests

В StationTradeSnapshotTests:

- Market_stock_fields_roundtrip_without_losing_zero_capacity (AC-03, AC-06): target100/max200/stock200/free0/stateSurplus.
- Every_stock_state_serializes_as_named_string (AC-06): Shortage/Normal/Surplus.
- Legacy_trade_json_has_null_market_fields (AC-06): старый JSON без полей; прежние значения не меняются.
- Legacy_positional_constructor_remains_valid (AC-05/06).
- Snapshot_does_not_expose_station_budget (AC-05): сериализованный JSON не содержит station Credits/MarketBudgetCredits/MaxBudget.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter FullyQualifiedName~StationTradeSnapshotTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Contracts\DeepSpaceSaga.Contracts.csproj --no-restore
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

Даны точные optional signatures, enum, null semantics, JSON checks и два разрешённых файла. End state: новый snapshot roundtrip совместим с legacy consumers и готов передавать ограниченный рынок; production расчёт не требуется для проверки Contracts.
