---
epic: EP-0001-trading-system
story: EP-0001-US-0003-dynamic-market-trading
ticket: EP-0001-US-0003-TK-0001-trade-execution-contract
title: Quote binding и результат сделки
stage: approved
layer: contracts
depends_on: []
files_touched: 3
serves: [AC-02, AC-04, AC-06]
created: 2026-09-21T09:33:31Z
revision: 1
---

# Quote binding и результат сделки

## Why

Команда должна ссылаться на конкретную котировку, а клиент — получать точную выполненную сумму, которую невозможно восстановить умножением одной цены. End state: команды и receipts JSON-roundtrip с backward-compatible optional fields.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj. Пути от D:/DeepSpaceSaga/DSS. Внешняя story dependency: US-0015, integration contract в родительской story. У неё ещё нет ticket IDs, поэтому фиктивные depends_on не вводятся.

## Decisions

2026-09-21T09:33:31Z — зафиксировано сообщение: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0003-dynamic-market-trading\EP-0001-US-0003-dynamic-market-trading.md». Других технических решений пользователя нет.

## Assumptions

Optional fields дописываются в конец records. Existing ExecutedQuantity сохраняет смысл partial-only для старых consumers; новый receipt явно содержит actual quantity при полном/частичном исполнении. Quote issuer и curve не реализуются здесь. Если US-0015 уже добавила идентичные binding fields/reason constants, переиспользовать их, не дублировать.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Contracts/PlayerCommand.cs | :6–50 record заканчивается Quantity | Дописать optional QuoteId/MarketRevision с XML contract |
| src/DeepSpaceSaga.Contracts/CommandResult.cs | :56–73 record с ExecutedQuantity; далее CommandReasonCodes | Optional TradeReceipt, record TradeExecutionReceipt, новые reason constants, комментарии |
| tests/DeepSpaceSaga.Contracts.Tests/CommandResultTests.cs | :10–30 проверяет legacy construction; использует System.Text.Json | Legacy/quoted command/result JSON roundtrip и immutable-array defaults |

## Public API after the change

Namespace DeepSpaceSaga.Contracts:
```csharp
// В конце PlayerCommand:
string? QuoteId = null,
long? MarketRevision = null
// В конце CommandResult:
TradeExecutionReceipt? TradeReceipt = null

public sealed record TradeExecutionReceipt(
    string? StationObjectId, string? ItemTypeId, string? QuoteId,
    long? QuotedMarketRevision, long? ResultMarketRevision,
    long? RequestedQuantity, long ExecutedQuantity, long TotalCredits,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))]
    ImmutableArray<string> LimitReasons = default);
```
CommandReasonCodes additions: QuoteRequired="quote_required", StaleQuote="stale_quote", InvalidQuote="invalid_quote", StationBudgetExceeded="station_budget_exceeded", StationCapacityExceeded="station_capacity_exceeded", FuelTradeForbidden="fuel_trade_forbidden". Existing value_overflow string не переименовывать; существующие reasons остаются.

Receipt nullable поля позволяют сохранить отказ до разрешения станции/невалидный raw quantity. На success все IDs/revisions/requested присутствуют, requested>0, 0<executed<=requested, total>=0; result revision > quoted revision. На rejection executed=total=0; known result revision не меняется. Item/quote/requested — исходные данные команды, station/revision — известный authoritative context, не выдуманные IDs. TotalCredits — абсолютная сумма; направление задано CommandResult.CommandType. PartialReason не смешивается с CommandResult.ReasonCode: success имеет ReasonCode=null, ограничения — в LimitReasons.

## Implementation steps

1. Добавить fields без изменения существующих positional параметров/defaults или JSON casing. DTO ничего не рассчитывают и не валидируют игровое состояние.
2. Определить immutable receipt в CommandResult.cs, добавить using ImmutableArray/JsonSerialization и existing converter. Пустые массивы сериализуются/читаются штатно.
3. Задокументировать binding к station/object/module/item/type/quantity: QuoteId непрозрачный, client не может придумывать цену. Nullable fields нужны только для legacy/rejection; Engine enforcement выполняет TK-0002.
4. Добавить named tests для старого JSON без fields, full/partial/rejected receipts, ulong ClientSequence, long revisions/quantities. Не менять enum numeric values CommandResultStatus.

## Out of scope

Quote calculator/API transport (US-0015), Engine validation/commit, UI, schema migration, финансовый ledger.

## Invariants

- Contracts не зависит от Engine/Client: Documentation/00-Process/CLAUDE.md:45–56.
- Legacy CommandResult без TradeReceipt валиден; CommandResult.cs:56–73.
- TradeQuantity целое; денежное округление принадлежит Engine: EngineRequirements.md:5263–5265.

## Tests

В CommandResultTests:
- `Legacy_command_and_result_json_keep_null_quote_and_receipt` — AC-04/06 compatibility.
- `Quoted_command_roundtrips_quote_id_revision_and_quantity` — AC-04.
- `Full_partial_and_rejected_trade_receipts_roundtrip_exact_totals` — AC-02/06.
- `Receipt_default_limit_reasons_roundtrip_as_empty` — AC-06.
- `Existing_command_status_values_and_executed_quantity_are_unchanged` — AC-02/04.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Contracts\DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены в трёх разрешённых файлах.
- Served criteria покрыты named tests для transport части, runtime enforcement явно отнесён к TK-0002.
- Matching tests/build/format проходят либо конкретное исходное падение записано отдельно.
- API/defaults/JSON и invariants соблюдены; нет hidden work, незаписанных assumptions или блокирующих вопросов.
- Command и receipt можно сериализовать и прочитать без Engine или Client.

## Self-containment check

Полные сигнатуры, null semantics, codes и допустимые совместимые формы приведены здесь. Для создания DTO не требуется искать формулу или решать API котировок.
