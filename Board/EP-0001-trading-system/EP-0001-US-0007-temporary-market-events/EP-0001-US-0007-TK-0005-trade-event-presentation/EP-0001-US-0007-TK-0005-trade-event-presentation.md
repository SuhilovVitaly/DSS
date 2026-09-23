---
epic: EP-0001-trading-system
story: EP-0001-US-0007-temporary-market-events
ticket: EP-0001-US-0007-TK-0005-trade-event-presentation
title: Причина и длительность события в Trade
stage: approved
layer: client
depends_on: [EP-0001-US-0007-TK-0001-market-event-contract, EP-0001-US-0007-TK-0003-market-event-content, EP-0001-US-0007-TK-0004-market-event-lifecycle, EP-0001-US-0003-TK-0004-quoted-trade-controls]
files_touched: 5
serves: [AC-05]
created: 2026-09-21T11:08:58Z
revision: 1
---

# Причина и длительность события в Trade

## Why

Игрок должен понимать, почему локальный рынок временно изменился, не покидая существующий Trade flow. Компактный badge сообщает число активных событий, а hover tooltip раскрывает имя, причину, эффект и оставшееся игровое время для максимум двух authoritative events.

Matching test project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`.

## Decisions

D-01, 2026-09-21T11:08:58Z: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0007-temporary-market-events\EP-0001-US-0007-temporary-market-events.md».

## Assumptions

- Badge/tooltip используют свободную верхнюю строку Trade body; каталожные строки и action controls не перемещаются.
- Tooltip read-only, не получает click/focus и не влияет на current input/scroll/history.
- Remaining duration берётся из authoritative `RemainingGameTimeMs`; Client только форматирует ceil часов. Permanent legacy event показывает localized `EventPermanent`.
- При неизвестном/пустом localization key Client безопасно использует LegacyDisplayName/LegacyDescription, затем DefinitionId/EventId.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs | :85–127 snapshot cache/rows, active events не хранятся | Кэшировать sorted ActiveEvents и дать pure presentation helpers без market math |
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeLayout.cs | :17–42 fixed geometry; top status line y80–126 | Добавить `EventBadge` и bounded `EventTooltip`; не двигать существующие rects |
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.cs | :15–26 interaction state; :158–172 hover routing | `_eventsHovered`, activation reset и hover detection только для badge |
| src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.Render.cs | :13–45 main render; :100–182 details/status | Нарисовать badge/tooltip from Model.ActiveEvents; preserve details/status/control rendering |
| tests/DeepSpaceSaga.Client.Tests/TradeScreenTests.cs | Existing render/input/snapshot tests | Добавить 0/1/2 event model/render/hover/localization regression tests |

## Public API after the change

No public API change. Internal model additions:

```csharp
internal StationMarketEventSnapshot[] ActiveEvents { get; private set; } = [];
internal static string EventText(string key, string? legacyFallback, string id);
internal static string EventRemaining(StationMarketEventSnapshot evt);
```

`Refresh` copies `snapshot.DockedStationTrade?.ActiveEvents`, filters impossible already-ended (`RemainingGameTimeMs<=0`, except `long.MaxValue`), sorts by `StartedGameTimeMs` then `EventId` ordinal and caps defensive display at2. Engine remains responsible for semantic max-two validation.

Layout:

```csharp
EventBadge   = (700, 84) .. (984, 126)
EventTooltip = (650, 130) .. (984, 330)
```

Paused/not-docked text is narrowed to `(450,84)..(690,126)`; Market/Fuel tabs, Catalog, Detail and all controls keep exact existing rectangles.

## Implementation steps

1. Cache the active array whenever snapshot reference changes. Do not associate event with selected item and do not recompute price/stock.
2. Add pure localization resolution: accept full key or `TradeUX.` suffix consistently with TK-0003; treat missing lookup returning key/empty as failure and use fallback chain. EffectSummary has no legacy string, so fallback is localized generic event label plus DefinitionId.
3. `EventRemaining`: `long.MaxValue`→EventPermanent; otherwise ceil positive `TimeSpan.FromMilliseconds(RemainingGameTimeMs).TotalHours`, minimum1, format `EventRemainingHours`. Never subtract wall clock or `Stopwatch`.
4. Render badge only when docked and count>0: warning/accent border, localized `ActiveEvents` with count, no flashing/animation. Existing paused label remains visible in its narrowed rect.
5. Hovering badge sets selected cursor through existing interactive return path and draws tooltip above body content after normal panels, clipped to `EventTooltip`. Tooltip receives up to two sections: Name + remaining, Description, Effect; ellipsize/paragraph clip, never overflow Catalog controls.
6. Tooltip mentions route limitation exactly as content text. It does not derive affected edge, alternative, price percentage or profit.
7. Badge does not respond to click/mouse-down and does not steal keyboard focus. Mouse leave/deactivate resets hover. History/Fuel modes show same station event badge.
8. With zero events, render output and hit-testing remain baseline except narrower paused text rect must not truncate at supported scales. Add render-coordinate regression for 1600×800 panel.
9. With legacy event keys empty, show fallbacks without raw localization exception. With malformed third event, display first two and no overflow; Engine test remains responsible for rejection.
10. Tests use authoritative snapshot fixtures; do not construct Engine or duplicate scheduler/content rules.

## Out of scope

Engine/Contracts/content edits, clickable event details, new screen/modal, per-item event badges, countdown timers between snapshots, price formula/route logic, new images/icons, changes to Buy/Sell/Refuel controls, history persistence or modal pause.

## Invariants

- Existing Trade flow/controls/modal pause remain unchanged: TradingSystemMvpStories.md:255–290.
- Client renders authoritative data and does not duplicate economic rules: Documentation/00-Process/CLAUDE.md:35–56.
- Render loop reads client-side snapshot only: CLAUDE.md:88–112.
- Fixed Trade layout baseline is current `TradeLayout.cs:17–42`; event UI must fit without repacking controls.

## Tests

В `TradeScreenTests.cs` добавить:

- `Trade_model_orders_and_exposes_authoritative_active_events` (AC-05): snapshot order shuffled, output start/ID sorted; no price mutation.
- `No_event_keeps_trade_controls_and_hit_targets_unchanged` (AC-05): Market/Fuel/Buy/Sell/Confirm/History rects and interactions.
- `One_event_renders_badge_and_hover_explanation` (AC-05): localized name/description/effect/ceil remaining hours.
- `Two_events_render_in_stable_order_inside_bounded_tooltip` (AC-05): capture draw calls or approved render probe; both visible, clip respected.
- `Event_badge_is_read_only_and_does_not_change_selection_focus_or_mode` (AC-05).
- `Fuel_and_history_views_keep_same_station_event_badge` (AC-05).
- `Permanent_and_legacy_event_use_localized_duration_and_fallback_text` (AC-05).
- `Unknown_localization_key_falls_back_without_throwing` (AC-05).
- `Modal_pause_snapshot_does_not_decrease_remaining_event_time` (AC-05): two renders of same snapshot, identical label; no wall-clock dependency.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TradeScreenTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Пять разрешённых files реализуют badge/tooltip без изменения production layers outside client.
- AC-05 покрыт named render/interaction/localization tests.
- Client tests/build/format проходят; known unrelated portrait asset failures записаны отдельно и не скрыты.
- Existing controls, modal behavior, history and authoritative boundary сохранены.
- Нет client-side market/route calculations or new screen.
- End state проверяется rendered text, bounds and hit-testing.

## Self-containment check

Даны exact geometry, state, ordering, fallback, duration formatting and tests. Implementer не ищет UX placement или event meaning. End state: Trade при 1–2 событиях показывает понятную localized причину/эффект по hover, а без событий работает как прежде.
