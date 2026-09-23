---
epic: EP-0001-trading-system
story: EP-0001-US-0008-route-risk-and-alternatives
ticket: EP-0001-US-0008-TK-0004-route-choice-presentation
title: Риск и причины в Station screen
stage: approved
layer: client
depends_on: [EP-0001-US-0008-TK-0003-route-event-voyage-integration]
files_touched: 4
serves: [AC-01, AC-02, AC-05]
created: 2026-09-21T11:10:35Z
revision: 1
---

# Риск и причины в Station screen

## Why

Сделать authoritative различия маршрутов видимыми в существующем месте выбора рейса. End state: Station screen показывает route rows с risk/time/fuel/reason, блокирует только `Unavailable` и передаёт выбранный destination существующей команде US-0014.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj. Пути относительно D:/DeepSpaceSaga/DSS.

## Decisions

Пользователь не задавал отдельный UX. Применена продуктовая граница Concept:175–205: не создавать второй route/trade screen, расширить station data минимально.

## Assumptions

US-0006/0014 к моменту реализации заменяет Undock placeholder на destination rows/action в этих же Station files. Этот тикет расширяет тот control, не создаёт параллельный выбор. Client доверяет enum/effective values/reason и не вычисляет event expiry, path connectivity, fuel cost или цены.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/Station/StationRoutePresentation.cs | Новый файл; formatter route rows отсутствует | Pure mapping DTO → labels/colors/enabled state и duration/fuel formatting |
| src/DeepSpaceSaga.Client/UI/Screens/Station/StationLayout.cs | :22–59 фиксирует 1600x800 panel и body rows; :120–155 central hit-test | Geometry route list/rows и hit-test, без нового screen/modal |
| src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs | :29–81 получает SnapshotBuffer/session; :83–96 сейчас содержит Undock placeholder; :228–277 render читает latest snapshot | Отрисовать dependency destination control из `snapshot.TradingRoutes`, disabled/reason/selection, submit только available/restricted |
| tests/DeepSpaceSaga.Client.Tests/StationScreenTests.cs | :10–20 описывает текущий station shell; :45–53 render helper; существующие button tests защищают layout | Route formatting/render/hit-test/submit/no-client-calculation regressions |

## Public API after the change

No Contracts/session API change. Internal client model:

```csharp
internal sealed record StationRouteRow(
    string DestinationStationObjectId,
    string PrimaryText,
    string SecondaryText,
    string? ReasonText,
    bool IsEnabled,
    TradingRouteRisk Risk,
    TradingRouteAvailability Availability);

internal static class StationRoutePresentation
{
    internal static ImmutableArray<StationRouteRow> Build(
        ImmutableArray<TradingRouteSnapshot> routes);
    internal static string FormatGameDuration(long gameTimeMs);
    internal static string FormatFuelMultiplier(int permille);
}
```

Dependency UI seam: US-0006/0014 exposes one selected destination and a submit action through the existing `StationScreen`/`GameSessionHandle`; this ticket must use it unchanged. If it uses other files/API, return to review before touching additional files.

## Implementation steps

1. Build rows in received order (Engine already sorts). Primary text contains destination and `[SAFE]`/`[RISK]`; secondary contains `DistanceClass`, effective duration and `Fuel xN.NN`. When base/effective differ, append `base ...` compactly. Display reason and active-event context only from DTO; do not interpret IDs.
2. `Available` and `Restricted` are enabled; `Unavailable` disabled. Restricted uses warning color, elevated risk uses existing warning/accent palette, unavailable uses disabled style. Formatting is culture-invariant and read-only.
3. Extend Station layout in free panel space without moving Trade/Hire/Finance/Contracts hit rectangles. Provide deterministic row rects and scroll/clipping for more rows than fit; no overlay or new `IScreen`.
4. Integrate with US-0014 destination selection: render all `TradingRoutes`; preserve selection only while destination remains present/enabled; clicking disabled row does nothing. Submit uses selected destination through dependency action. Engine rejection remains shown by existing command-result path; Client does not optimistically start voyage.
5. When route array is empty, show one neutral `Маршруты недоступны` line and disable departure, preserving legacy scenarios. When one edge is unavailable but another is usable, both remain visible and the usable row can be selected.
6. Add tests using hand-built snapshots. Verify displayed strings/state through pure presentation seam and hit-test/selection through screen; no pixel-only assertion required. Existing four station buttons and modal close behavior must remain unchanged.

## Out of scope

New screen/modal, global route graph drawing, price/stock knowledge, pathfinding, event timing, authoritative validation, voyage/fuel implementation, localization framework and redesign of Station panel.

## Invariants

- Client reads only snapshot and submits through session boundary: EngineRequirements.md:379–419.
- Concept forbids a separate route/trade window: TradingSystemConcept.md:175–205.
- Existing Station modal pause/navigation remains generic: Documentation/00-Process/CLAUDE.md:150–156; StationScreen.cs:19–27.
- Existing buttons retain fixed non-overlapping rows: StationLayout.cs:30–59.
- Four allowed files, client layer, matching Client.Tests.

## Tests

Extend `StationScreenTests`:

- `Route_rows_show_safe_risk_effective_time_fuel_and_authoritative_reason` (AC-01/02/05).
- `Unavailable_route_is_visible_but_disabled_and_available_alternative_is_selectable` (AC-05).
- `Restricted_route_remains_selectable_with_warning_style` (AC-05).
- `Empty_routes_show_neutral_message_and_disable_departure` (AC-02/05).
- `Route_order_and_values_are_not_recomputed_by_client` (AC-02).
- `Route_panel_does_not_overlap_existing_station_buttons_or_close_hit_target` (AC-05).
- Existing `StationScreenTests` remain green.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~StationScreenTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps выполнены только в четырёх files; новый screen и Engine reference не добавлены.
- AC-01/02/05 покрыты named formatting/interaction/layout tests.
- Named tests/build/format проходят либо известное baseline asset failure записано отдельно; отсутствие `portrait-style.json` не маскируется как успех.
- Existing station buttons, modal pause, authoritative submit и out-of-scope соблюдены.
- Нет скрытых assumptions или незакрытых вопросов; результат наблюдаем в route rows и tests.

## Self-containment check

Row model, exact fields, state mapping, formatting, layout constraints, empty/error behavior и dependency seam заданы. Implementer не должен проектировать маршрутную механику или новый экран.
