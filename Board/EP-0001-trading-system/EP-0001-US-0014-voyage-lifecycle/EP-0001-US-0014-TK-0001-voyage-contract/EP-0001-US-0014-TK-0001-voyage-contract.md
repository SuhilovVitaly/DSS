---
epic: EP-0001-trading-system
story: EP-0001-US-0014-voyage-lifecycle
ticket: EP-0001-US-0014-TK-0001-voyage-contract
title: Snapshot-контракт жизненного цикла рейса
stage: approved
layer: contracts
depends_on: [EP-0001-US-0004-TK-0001-trading-map-schema]
files_touched: 4
serves: [AC-01, AC-06, AC-08]
created: 2026-09-21T14:55:44Z
revision: 1
---

# Snapshot-контракт жизненного цикла рейса

## Why

Client и будущий network adapter должны видеть одинаковые route options, phase, destination, progress и blocker без доступа к Engine/save DTO. End state: JSON-совместимый immutable snapshot различает docked selection и один active voyage, а reason codes стабильны для UI и downstream US-0008/US-0009.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`. Все относительные пути — от `D:/DeepSpaceSaga/DSS`.

## Decisions

Единственное сообщение пользователя: «сделай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0014-voyage-lifecycle\EP-0001-US-0014-voyage-lifecycle.md»; зафиксировано 2026-09-21T14:55:44Z UTC. Технических решений пользователь не добавлял.

## Assumptions

Команда остаётся `navigation.undock`, destination передаётся существующим `PlayerCommand.TargetObjectId` (`PlayerCommand` уже сериализует explicit target: `tests/DeepSpaceSaga.Contracts.Tests/SmokeTests.cs:23–60`). Snapshot nullable для backward-compatible callers; обновлённый Engine публикует non-null `Voyage`.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Contracts/VoyageSnapshot.cs | Новый файл; voyage DTO/phase vocabulary отсутствуют | Добавить только immutable JSON-safe DTO и стабильные phase constants |
| src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs | :11–53 — trailing snapshot fields, docking trade и timestamps; voyage отсутствует | Добавить один trailing optional `VoyageSnapshot? Voyage = null` |
| src/DeepSpaceSaga.Contracts/CommandResult.cs | :73–142 — централизованные snake_case reason codes, voyage codes отсутствуют | Добавить только перечисленные voyage reason constants |
| tests/DeepSpaceSaga.Contracts.Tests/VoyageSnapshotTests.cs | Новый файл; serialization pattern — SmokeTests.cs:102–169 | Проверить defaults, JSON round-trip, constants и permille payload |

## Public API after the change

```csharp
public static class VoyagePhases
{
    public const string Docked = "Docked";
    public const string Undocking = "Undocking";
    public const string InTransit = "InTransit";
    public const string Docking = "Docking";
}

public sealed record VoyageRouteOptionSnapshot(
    string DestinationStationObjectId,
    string DestinationDisplayName,
    long TravelEstimateGameTimeMs,
    string DistanceClass,
    bool IsAvailable = true,
    string? BlockReasonCode = null);

public sealed record VoyageSnapshot(
    string Phase,
    string? VoyageId = null,
    string? OriginStationObjectId = null,
    string? DestinationStationObjectId = null,
    string? DestinationDisplayName = null,
    int ProgressPermille = 0,
    string? BlockReasonCode = null,
    ImmutableArray<VoyageRouteOptionSnapshot> RouteOptions = default);
```

Trailing parameter `AuthoritativeSnapshot(..., VoyageSnapshot? Voyage = null)`.

Новые `CommandReasonCodes`: `VoyageDestinationRequired = "voyage_destination_required"`, `VoyageDestinationUnavailable = "voyage_destination_unavailable"`, `VoyageAlreadyActive = "voyage_already_active"`, `VoyageWrongDestination = "voyage_wrong_destination"`, `VoyageOutstandingDebt = "voyage_outstanding_debt"`, `VoyageInsufficientFuel = "voyage_insufficient_fuel"`.

## Implementation steps

1. Добавить DTO без Engine/Client references, graphics types, mutable collections или локализованного текста. `RouteOptions` использует тот же default-safe ImmutableArray converter pattern, что snapshot collections.
2. Зафиксировать case-sensitive phases и snake_case reason codes ровно как выше. Не вводить enum JSON converter и не переиспользовать `StationDistrict`/`StationTravelCommand`.
3. Добавить `Voyage` последним optional параметром, сохранив source compatibility всех существующих positional/named constructors.
4. Contract допускает `null Voyage` только для legacy producer. Semantic validation (`phase`, IDs, range `0..1000`, active-vs-docked shape) принадлежит Engine TK-0002; DTO не вычисляет availability/progress.
5. `VoyageInsufficientFuel` — контракт integration result, а не реализация fuel formula. US-0009 использует этот же code без изменения DTO.

## Out of scope

Scenario/save DTO, Engine state machine, route lookup, price/stock, fuel calculation, adapter method, Client rendering и localization resources.

## Invariants

- Contracts не зависит ни от одного project и переносит только сериализуемые domain DTO: `Documentation/00-Process/CLAUDE.md:44–54,96–103,172–180`.
- Snapshot immutable и backward-compatible trailing defaults: `AuthoritativeSnapshot.cs:6–17,50–57`.
- Command outcomes уже передаются snapshot stream, новый synchronous reply не нужен: `CommandResult.cs:3–13,38–63`.
- `navigation.undock` и `PlayerCommand.TargetObjectId` уже стабильны: `NavigationComputerCommandTypes.cs:11–15`; `SmokeTests.cs:23–60`.

## Tests

Класс `VoyageSnapshotTests`:

- `Legacy_snapshot_defaults_voyage_to_null`.
- `Voyage_snapshot_roundtrip_preserves_all_phases_route_options_progress_and_blocker` (Theory по четырём phases; empty/default ImmutableArray тоже не падает).
- `Voyage_reason_codes_are_stable_snake_case_values`.
- `Undock_player_command_roundtrip_preserves_destination_target`.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~VoyageSnapshotTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Contracts\DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены только в четырёх разрешённых файлах.
- AC-01/06/08 представлены transport-neutral API и покрыты named serialization/constant tests.
- Named tests, matching layer build и format проходят либо конкретное исходное падение записано отдельно.
- Существующие snapshot constructors и `PlayerCommand` совместимы; нет Engine/Client dependency, скрытой логики или незаписанных assumptions.
- Результат проверяем JSON round-trip и public signatures.

## Self-containment check

Имена типов, параметры, значения phases/reasons, defaults и test cases заданы полностью. Implementer не ищет продуктовые формулировки или transport design за пределами четырёх файлов.
