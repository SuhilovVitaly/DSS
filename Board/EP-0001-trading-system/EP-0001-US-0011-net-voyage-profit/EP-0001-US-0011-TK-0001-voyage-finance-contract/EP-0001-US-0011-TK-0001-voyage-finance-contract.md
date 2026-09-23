---
epic: EP-0001-trading-system
story: EP-0001-US-0011-net-voyage-profit
ticket: EP-0001-US-0011-TK-0001-voyage-finance-contract
title: Контракт финансового результата рейса
stage: approved
layer: contracts
depends_on: [EP-0001-US-0009-TK-0001-voyage-fuel-contract, EP-0001-US-0010-TK-0001-cargo-result-contract, EP-0001-US-0014-voyage-lifecycle]
files_touched: 3
serves: [AC-01, AC-03, AC-04]
created: 2026-09-21T12:56:24Z
revision: 1
---

# Контракт финансового результата рейса

## Why

Finance и Trade history нужен один immutable authoritative DTO с компонентами прибыли, состоянием реализации, долгом и непроданным cargo. Контракт должен передавать данные, но не рассчитывать gameplay или форматировать UI.

## Decisions

Пользователь указал canonical story path сообщением из `source_request`; дополнительных решений по API не давал. Формула, nullable unknown COGS, assessed-vs-paid fee и двухфазный статус зафиксированы AC/A-01…A-08 story.

## Assumptions

Тикет исполняется после contracts-частей US-0009/0010/0014. Их actual types должны содержать stable `VoyageId`, fuel settlement и trade receipt, указанные в dependency ticket docs; если signatures отличаются, этот тикет возвращается в review, а не создаёт дубликаты типов.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/VoyageFinanceSnapshot.cs` | Новый файл; voyage finance DTO отсутствует (`AuthoritativeSnapshot.cs:11–60`) | Добавить records и state constants с exact fields ниже, без методов расчёта |
| `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs` | Record заканчивается `SimulationTimeMs` (`:11–53`); arrays используют default converter (`:17–22,42–51`) | Добавить последним `VoyageFinances` с immutable-array converter/default |
| `tests/DeepSpaceSaga.Contracts.Tests/VoyageFinanceContractTests.cs` | Новый файл; JSON patterns есть в `CommandResultTests.cs:103–193` и `StationTradeSnapshotTests.cs:74–105` | JSON/default/order/null/negative-profit regressions |

## Public API after the change

```csharp
public static class VoyageFinanceStates
{
    public const string InTransit = "in_transit";
    public const string AwaitingRealization = "awaiting_realization";
    public const string Finalized = "finalized";
    public const string Interrupted = "interrupted";
}

public sealed record VoyageCargoRemainderSnapshot(
    string ItemTypeId,
    long Quantity,
    long? CostBasisCredits);

public sealed record VoyageFinanceSnapshot(
    string VoyageId,
    string OriginStationObjectId,
    string? DestinationStationObjectId,
    long StartedGameTimeMs,
    long? CompletedGameTimeMs,
    string State,
    long GrossSalesCredits,
    long? CostOfGoodsSoldCredits,
    bool HasUnknownCostOfGoodsSold,
    long RouteFuelCostCredits,
    long PortFeesAssessedCredits,
    long PortFeesPaidCredits,
    long OutstandingPortFeeDebtCredits,
    long EventCostsCredits,
    long PassengerPayoutCredits,
    long PassengerPenaltyCredits,
    long? NetProfitCredits,
    ImmutableArray<VoyageCargoRemainderSnapshot> UnsoldCargo = default);

// Last trailing AuthoritativeSnapshot parameter:
[property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<VoyageFinanceSnapshot>))]
ImmutableArray<VoyageFinanceSnapshot> VoyageFinances = default
```

`VoyageFinances` упорядочен oldest-first/newest-last и содержит максимум 50 entries. `NetProfitCredits` и `CostOfGoodsSoldCredits` могут быть отрицательным/null только по указанным semantics: COGS неотрицателен, net может быть отрицательным, unknown COGS делает оба nullable результата unavailable. Contracts не выполняет validation и arithmetic.

## Implementation steps

1. Создать records/constants без ссылок на Engine/Client и без вычислительных методов.
2. Добавить `VoyageFinances` строго последним positional parameter `AuthoritativeSnapshot`; старые constructors/JSON получают empty/default, а не synthesized report.
3. Применить существующий `ImmutableArrayDefaultJsonConverter`, чтобы default и empty безопасно round-trip и legacy JSON не создавал null collection exception.
4. XML-doc зафиксировать формулу, значения `State`, nullable COGS/net, semantics assessed/paid/debt и запрет включать unsold basis в net.
5. Tests проверяют exact state constants, полный positive/loss/unknown snapshot, default/legacy construction, порядок entries, empty/default unsold cargo и `long` extrema без арифметики.

## Out of scope

Engine state, формула/округление, persistence, trade/fuel/fee hooks, passenger/event mechanics, локализация и rendering.

## Invariants

- Contracts не зависит от Engine/Client: `Documentation/00-Process/CLAUDE.md:44–54`.
- Boundary immutable и JSON-serializable: `AuthoritativeSnapshot.cs:6–11`.
- Money — `long`, без float/double: `Documentation/01-Requirements/EngineRequirements.md:5247–5265`.
- Новый field trailing/default; существующие constructors и legacy snapshots сохраняют совместимость.
- Только три файла таблицы; matching project — `tests/DeepSpaceSaga.Contracts.Tests`.

## Tests

Named tests:

- `Voyage_finance_round_trips_all_components_and_unsold_cargo` (AC-01/03/04).
- `Voyage_finance_round_trips_negative_net_profit` (AC-01).
- `Unknown_cogs_keeps_sales_but_nulls_cogs_and_net` (AC-03).
- `Legacy_snapshot_defaults_voyage_finances_to_empty` (AC-04).
- `Voyage_finance_states_have_exact_wire_values` (AC-04).

Commands:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~VoyageFinanceContractTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Contracts\DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены в разрешённых файлах.
- Каждый пункт acceptance criteria, указанный в `serves`, покрыт изменением и named tests.
- Именованные тесты тикета проходят; указаны точные команды проверки.
- Build/lint соответствующего layer проходят либо конкретное исходное падение записано отдельно и не скрыто.
- Публичные API, invariants и out-of-scope ограничения соблюдены.
- Нет незаписанных assumptions, незакрытых блокирующих вопросов или скрытой работы вне `Code context`.
- Результат можно проверить по команде, тесту, diff evidence или наблюдаемому поведению.

## Self-containment check

Exact DTO names, fields, order, wire constants, null semantics, compatibility rule, allowed files и named tests заданы. Implementer проверяет только фактическую готовность dependency contracts; новых решений или поиска других файлов не требуется.
