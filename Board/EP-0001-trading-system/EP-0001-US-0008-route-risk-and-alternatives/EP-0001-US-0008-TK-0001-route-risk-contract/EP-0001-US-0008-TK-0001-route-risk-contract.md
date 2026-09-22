---
epic: EP-0001-trading-system
story: EP-0001-US-0008-route-risk-and-alternatives
ticket: EP-0001-US-0008-TK-0001-route-risk-contract
title: Контракт риска и доступности маршрута
stage: approved
layer: contracts
depends_on: []
files_touched: 3
serves: [AC-02, AC-05]
created: 2026-09-21T11:10:35Z
revision: 1
---

# Контракт риска и доступности маршрута

## Why

Передать клиенту authoritative route choice без доступа к Engine и без клиентского пересчёта риска. End state: новый immutable DTO сериализуется, а legacy `AuthoritativeSnapshot` получает пустой массив по умолчанию.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj. Все пути относительно D:/DeepSpaceSaga/DSS.

## Decisions

Сообщение пользователя: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0008-route-risk-and-alternatives\EP-0001-US-0008-route-risk-and-alternatives.md»; зафиксировано 2026-09-21T11:10:35Z UTC. Других технических решений пользователь не добавлял.

## Assumptions

Snapshot публикует только исходящие направления docked station; Engine задаёт effective values/reason. `Restricted` остаётся выбираемым, `Unavailable` — нет. DTO не содержит price, stock, вероятности боя или физической траектории. Trailing field сохраняет source compatibility старых constructors/JSON fixtures.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Contracts/TradingRouteSnapshot.cs | Новый файл; экономического route DTO в Contracts нет, `ObjectMotionSnapshot` относится к физической motion projection (`ObjectMotionSnapshot.cs:3–11`) | Два string-enum и immutable route snapshot ниже |
| src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs | :11–53 positional record заканчивается `SimulationTimeMs`; массивы используют default-array converter :17–22,42–51 | Добавить trailing `TradingRoutes` с default converter, не менять существующие параметры |
| tests/DeepSpaceSaga.Contracts.Tests/TradingRouteSnapshotTests.cs | Новый файл; JSON/default pattern находится в `StationTradeSnapshotTests.cs:13–112` | Round-trip, enum/default/backward-compatibility tests |

## Public API after the change

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<TradingRouteAvailability>))]
public enum TradingRouteAvailability { Available, Restricted, Unavailable }

[JsonConverter(typeof(JsonStringEnumConverter<TradingRouteRisk>))]
public enum TradingRouteRisk { Safe, Elevated }

public sealed record TradingRouteSnapshot(
    string OriginStationObjectId,
    string DestinationStationObjectId,
    string DistanceClass,
    long BaseTravelEstimateGameTimeMs,
    long EffectiveTravelEstimateGameTimeMs,
    int BaseFuelMultiplierPermille,
    int EffectiveFuelMultiplierPermille,
    string RiskProfileId,
    TradingRouteRisk Risk,
    TradingRouteAvailability Availability,
    string? ReasonText = null,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))]
    ImmutableArray<string> ActiveEventIds = default);
```

Trailing field in `AuthoritativeSnapshot`:

```csharp
[property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<TradingRouteSnapshot>))]
ImmutableArray<TradingRouteSnapshot> TradingRoutes = default
```

## Implementation steps

1. Добавить DTO/enums ровно с указанными именами и string JSON enums. Все числа уже рассчитаны Engine; Contracts не содержит arithmetic или validation бизнес-правил.
2. Добавить `TradingRoutes` последним параметром snapshot. Не переставлять `RouteArrivalGameTimeMs`/`SimulationTimeMs`, чтобы positional callers продолжили компилироваться.
3. `ActiveEventIds` и `TradingRoutes` должны deserialize отсутствующее/null как default-or-empty существующим converter. `ReasonText=null` означает отсутствие активного ограничения, не неизвестную ошибку.
4. JSON round-trip обязан сохранять `Restricted`, `Elevated`, обе пары base/effective значений, reason и порядок event IDs. Добавить тест legacy JSON без нового поля.

## Out of scope

Map schema, event lifecycle, вычисление риска, alternative validation, voyage command, UI, content и сохранение Engine state.

## Invariants

- Contracts не зависит от Engine/Client: Documentation/00-Process/CLAUDE.md:44–54; существующий project boundary проверяется ArchitectureTests.
- UI получает данные только через публичный snapshot: EngineRequirements.md:379–419.
- `AuthoritativeSnapshot` остаётся immutable record и старые call sites используют defaults: AuthoritativeSnapshot.cs:6–11,52–57.
- Только три файла таблицы; production layer contracts, matching tests Contracts.Tests.

## Tests

`TradingRouteSnapshotTests`:

- `Route_snapshot_round_trips_base_effective_risk_availability_reason_and_events` (AC-02).
- `Legacy_authoritative_snapshot_defaults_routes_to_empty` (AC-02).
- `Null_arrays_deserialize_as_default_or_empty` (AC-02).
- `Route_enums_serialize_as_stable_strings` (AC-05).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter FullyQualifiedName~TradingRouteSnapshotTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Contracts\DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены только в трёх разрешённых файлах.
- AC-02/AC-05 покрыты named JSON/default tests; matching build/format проходят либо исходное падение записано отдельно.
- Публичные signatures выше соблюдены; existing snapshot constructors и архитектурная граница не сломаны.
- Нет незаписанных assumptions, блокирующих вопросов или скрытой работы вне `Code context`.
- Результат проверяется сериализацией, compile и test evidence.

## Self-containment check

Полные signatures, defaults, enum values, serialization и allowed files заданы. Implementer не должен искать UX или Engine-решения.
