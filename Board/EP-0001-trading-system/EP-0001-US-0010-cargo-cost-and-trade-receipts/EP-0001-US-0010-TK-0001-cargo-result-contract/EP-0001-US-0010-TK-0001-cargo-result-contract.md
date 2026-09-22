---
epic: EP-0001-trading-system
story: EP-0001-US-0010-cargo-cost-and-trade-receipts
ticket: EP-0001-US-0010-TK-0001-cargo-result-contract
title: Себестоимость в authoritative receipt
stage: approved
layer: contracts
depends_on: [EP-0001-US-0003-TK-0001-trade-execution-contract]
files_touched: 2
serves: [AC-02, AC-06, AC-07]
created: 2026-09-21T12:39:58Z
revision: 1
---

# Себестоимость в authoritative receipt

## Why

Client не может доказуемо вычислить себестоимость из текущей цены или cached unit price. End state: immutable trade receipt переносит nullable realized cargo cost и gross result продажи через существующую Contracts boundary и JSON без нарушения старых payloads.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`. Пути ниже — от `D:/DeepSpaceSaga/DSS`. Prerequisite US-0003 создаёт сам `TradeExecutionReceipt`; этот тикет его только совместимо расширяет.

## Decisions

2026-09-21T12:39:58Z — пользователь запросил: «создай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0010-cargo-cost-and-trade-receipts\EP-0001-US-0010-cargo-cost-and-trade-receipts.md». Других пользовательских API-решений нет.

## Assumptions

`TotalCredits` остаётся абсолютной фактической суммой: purchase cost для Buy, sale proceeds для Sell и refuel cost для Refuel. Новые поля нужны только для Sell. Null означает «не применяется или себестоимость недоказуема», а не zero.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Contracts/CommandResult.cs | :56–71 содержит текущий result; prerequisite US-0003 добавляет `TradeExecutionReceipt` с actual quantity/total | Добавить два optional поля в конец receipt и XML semantics, не менять прежние параметры/enum values |
| tests/DeepSpaceSaga.Contracts.Tests/CommandResultTests.cs | :10–30 legacy construction, :72–99 quantity JSON roundtrip; prerequisite добавляет receipt cases | JSON/constructor regressions для positive, negative, zero и null cargo result |

## Public API after the change

В конец prerequisite record из US-0003:

```csharp
public sealed record TradeExecutionReceipt(
    string? StationObjectId,
    string? ItemTypeId,
    string? QuoteId,
    long? QuotedMarketRevision,
    long? ResultMarketRevision,
    long? RequestedQuantity,
    long ExecutedQuantity,
    long TotalCredits,
    ImmutableArray<string> LimitReasons = default,
    long? RealizedCargoCostCredits = null,
    long? GrossResultCredits = null);
```

Для known successful Sell: `RealizedCargoCostCredits >= 0` и `GrossResultCredits == checked(TotalCredits - RealizedCargoCostCredits)`; gross может быть отрицательным. Для Buy/Refuel/rejection оба null. Для successful Sell с `legacy-unknown` оба null: это явная недоступность, не прибыль с zero basis. Existing JSON без полей десериализуется с null.

## Implementation steps

1. Добавить оба positional optional поля только в конец prerequisite receipt и документировать command-type semantics.
2. Не добавлять вычисления, validation игрового состояния или Engine dependencies в Contracts.
3. Сохранить casing и существующий `ImmutableArray` converter; старый JSON и source construction должны компилироваться без новых arguments.
4. Добавить named tests для known profit/loss/break-even Sell, unknown legacy Sell, Buy/Refuel и rejected receipt, включая JSON roundtrip отрицательного gross.

## Out of scope

Cost-basis storage/arithmetic, save version, Engine commit, locale/UI, Finance/voyage ledger, новые reason codes.

## Invariants

- Contracts не зависит от Engine/Client: `Documentation/00-Process/CLAUDE.md:45–56`.
- Current optional append compatibility: `CommandResult.cs:56–71`; prerequisite US-0003 следует тому же правилу.
- Money остаётся `long`; никакого float/double: `EngineRequirements.md:5247–5265`.

## Tests

- `Trade_receipt_roundtrips_realized_cost_and_positive_negative_or_zero_gross_result` — AC-02/07.
- `Legacy_unknown_sell_roundtrips_null_cost_and_result_without_turning_them_into_zero` — AC-07.
- `Buy_refuel_and_rejected_receipts_keep_cargo_result_null` — AC-02.
- `Legacy_receipt_json_without_cargo_result_fields_remains_compatible` — AC-06.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~CommandResultTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Contracts\DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps выполнены только в двух разрешённых файлах.
- AC-02/06/07 transport semantics покрыты named tests; runtime arithmetic явно остаётся TK-0003.
- Tests/build/format проходят либо исходное падение записано отдельно.
- Старые constructors/JSON, nullable semantics, invariants и out-of-scope соблюдены.
- Known Sell receipt однозначно проверяется равенством proceeds − cost = gross; null никогда не интерпретируется как zero.

## Self-containment check

Точная сигнатура, nullable semantics и допустимые значения приведены. Implementer не должен искать формулу cost basis или менять Engine.

