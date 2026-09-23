---
epic: EP-0001-trading-system
story: EP-0001-US-0014-voyage-lifecycle
ticket: EP-0001-US-0014-TK-0003-station-route-departure
title: Выбор назначения и отправление со Station
stage: approved
layer: client
depends_on: [EP-0001-US-0014-TK-0001-voyage-contract, EP-0001-US-0014-TK-0002-authoritative-voyage-lifecycle]
files_touched: 5
serves: [AC-01, AC-06, AC-08]
created: 2026-09-21T14:55:44Z
revision: 1
---

# Выбор назначения и отправление со Station

## Why

Превратить уже рабочую кнопку Undock в явный выбор authoritative route option. End state: игрок видит соседние destinations и blockers, выбирает одну, а Client отправляет её как `TargetObjectId` через существующую async boundary без локального расчёта доступности.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Пути относительно `D:/DeepSpaceSaga/DSS`.

## Decisions

Точное пользовательское сообщение приведено в story. Отдельных UI/naming решений пользователь не добавлял.

## Assumptions

Список умещается в station panel для MVP из максимум четырёх соседей пятиузлового графа. Первый доступный option выбирается по умолчанию; недоступный можно выделить для чтения причины, но нельзя отправить. После отправки сохраняется существующее закрытие Station modal; authoritative status/rejection виден в GameSession по TK-0004 и при повторном открытии станции.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/Station/StationLayout.cs | :23–64 panel/button geometry; :125–171 Undock rect/hit test | Добавить до четырёх route-row rects/hit test без перекрытия существующих controls |
| src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs | :77–81 live buffer/session; :112–145 click; Undock уже event | Render/select authoritative options, expose selected destination, disable/send semantics и readable blocker |
| src/DeepSpaceSaga.Client/GameSessionHandle.cs | :89–130 generic command и untargeted SendUndockCommand | Изменить helper на destination-aware send через существующий `SendCommandAsync(..., targetObjectId)` |
| src/DeepSpaceSaga.Client/UI/SkiaWindow.cs | :643–661 Station event dispatch | Передать selection текущего StationScreen в handle; не читать Engine/save types |
| tests/DeepSpaceSaga.Client.Tests/StationScreenTests.cs | :237–285 basic Undock/hit geometry; screen fixtures выше | Route selection/render/disabled/send-target regressions с fake connection |

## Public API after the change

Client-internal surface:

```csharp
// StationScreen
internal string? SelectedVoyageDestinationObjectId { get; }

// GameSessionHandle
public void SendUndockCommand(string? destinationStationObjectId = null)
```

`ScreenEvent.Undock` остаётся прежним. Для map-backed snapshot `SkiaWindow` при этом event читает `SelectedVoyageDestinationObjectId`; при null ничего не отправляет и modal не закрывает. Для legacy snapshot с `Voyage == null` parameterless call сохраняет прежний untargeted Undock. No Contracts API beyond TK-0001.

## Implementation steps

1. Добавить справа от существующего hub-column список максимум четырёх `Voyage.RouteOptions`, отсортированный в уже authoritative order. Каждая строка показывает destination name, distance class, ETA и available/blocked state; никаких цен, stock, fuel formula или route reconstruction.
2. При активации/новом snapshot сохранить selection, если ID ещё существует; иначе выбрать первый available. Click по row выделяет и unavailable option для просмотра reason. Keyboard/navigational redesign не входит.
3. Undock button enabled только при `Voyage.Phase == Docked`, выбранном `IsAvailable` и null blocker. При disabled состоянии клик возвращает `None`; рядом показывается понятный mapping для шести TK-0001 codes с fallback `Departure unavailable (<code>)`. Не скрывать unknown authoritative code.
4. `GameSessionHandle.SendUndockCommand(destination)` находит существующий NavigationComputer module как сейчас и вызывает generic `SendCommandAsync` с `NavigationComputerCommandTypes.Undock` и optional target. Не добавлять direct Engine call, optimistic snapshot mutation или отдельный connection method.
5. Для map-backed snapshot `SkiaWindow` перед отправкой захватывает non-null selection из текущего `StationScreen`, отправляет один раз и только затем закрывает modal; null/disabled path не закрывает экран. Existing mapless legacy screen с `Voyage == null` вызывает тот же helper без target и закрывает modal, чтобы базовые сценарии не сломались до US-0004 rollout.
6. Тесты используют synthetic `VoyageSnapshot` и recording `IGameSessionConnection`: проверяют exact target, single send, selection stability, no-send unavailable/null, labels и отсутствие overlap. Не запускать локальный Engine из Client unit ticket.

## Out of scope

GameSession status, Engine validation, actual fuel/event calculation, new route screen, exact remote prices, mouse-wheel scrolling, localization catalog, изменение Trade/Hire/Finance/Contracts и Station district travel.

## Invariants

- Render/input работает только с SnapshotBuffer, не вызывает Engine synchronously: `CLAUDE.md:72–103`; `StationScreen.cs:31–32,77–81`.
- Generic command уже переносит explicit target: `GameSessionHandle.cs:89–114`.
- Existing event dispatch закрывает modal после Undock: `SkiaWindow.cs:653–661`; изменение сохраняет async boundary и modal pause rule.
- Current Undock button/rows не перекрываются: `StationLayout.cs:31–64,125–171`; regressions — `StationScreenTests.cs:237–285`.
- Client не вычисляет availability/progress/fuel; только отображает TK-0001 DTO.

## Tests

В `StationScreenTests`:

- `Route_rows_render_authoritative_destination_class_and_eta` (AC-01/08).
- `First_available_route_is_selected_and_click_changes_selection` (AC-08).
- `Blocked_route_shows_reason_and_cannot_emit_undock` (Theory для debt/fuel/unavailable; AC-06).
- `Undock_sends_exactly_one_command_with_selected_destination_target` (AC-08).
- `Selection_survives_new_snapshot_and_falls_back_when_route_disappears` (AC-01/08).
- `Route_rows_do_not_overlap_existing_station_controls`.
- `Legacy_snapshot_without_voyage_keeps_untargeted_undock` (AC-09 dependency regression).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~StationScreenTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены в пяти разрешённых файлах.
- AC-01/06/08 покрыты named UI/transport tests; Client не дублирует Engine rules.
- Named tests, matching layer build и format проходят либо baseline failure записан отдельно.
- Existing modal/event/legacy Undock behavior и соседние Station controls сохранены.
- Нет hidden files, direct Engine dependency или незаписанных assumptions; destination проверяем recording connection command.

## Self-containment check

Layout capacity, selection/fallback, enabled rule, reason mapping boundary, exact send path и legacy branch заданы. Реальная карта/Engine не нужны для unit реализации и проверки.
