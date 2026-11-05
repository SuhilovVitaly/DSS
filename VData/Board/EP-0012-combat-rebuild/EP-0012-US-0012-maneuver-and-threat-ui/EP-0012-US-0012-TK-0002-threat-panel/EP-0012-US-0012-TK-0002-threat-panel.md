---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0012-maneuver-and-threat-ui"
ticket: "EP-0012-US-0012-TK-0002-threat-panel"
title: "threat-panel"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0012-TK-0001-evade-input-and-threat-select"]
files_touched: 7
serves: ["US12-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0012-TK-0002-threat-panel

## Why

[Манёвр и угрозы в UI](../EP-0012-US-0012-maneuver-and-threat-ui.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: threat-panel; доказать US12-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Bounded scope (7 files), updated before code. Panel must participate in real mouse/scroll and available-map geometry; actions reuse actual authoritative command paths, and interception badges share the public-only predictor with aiming.

| File | Allowed change |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ThreatPanel.cs` | NEW logical-coordinate, scrollable observed threat rows with select/CM/evade actions |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Actual panel draw, mouse/scroll interception and hit-test integration |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatPanel.cs` | NEW current displayed threat data/action wiring and responsive panel bounds |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs` | Exclude threat panel from available map/fit bounds |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Maneuver.cs` | Share actual evade submission between key release and panel actions |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CounterAimPreview.cs` | Share same public-only conditional interception badge predictor between aim and panel |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0002Tests.cs` | NEW sorting/filtering/layout/actual action and admitted-row latency checks |

A-THREAT-PANEL: include only displayed foreign torpedoes whose observed closest distance is <= R+3 km; sort by observed entry time with ordinal ties, with unknown entry shown as unknown. Rows update in the same rendered admitted scene. Long lists scroll; compact rows wrap their actions. CM button selects that threat and enters point aim without auto-launch; arrow buttons execute one explicit evade for that row. Interception glyph is a conditional own-CM/observed-ray projection, never a confirmed kill.

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Новый `Controls/ThreatPanel.cs`.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Threat_panel_sorts_and_filters_observed_traces**: farther than R+3км excluded; profile/entry/interception/actions видны.
- **Threat_row_appears_within_200ms_of_admitted_launch**: граница измерения — display-admitted event, не скрытое Engine время.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0012TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0012TK0002Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ThreatPanel.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US12-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 9 named / 2236 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0012-tk0002-results.json).

Observed threat panel filters closest distance <=R+3 km, sorts entry time/ordinal IDs, shows profile/real deadline and shared conditional interception badge. Actual CM/left/right actions reach existing Engine submission paths; selection highlights the chosen object. Current scene admission to first row satisfies <=200ms; no row from unadmitted packet. Scroll/panel hit testing blocks zoom and objects behind UI, RMB still cancels aim; available-map/Fit excludes panel. Layout/action bounds pass at 100/150 percent; headless render inspected. Related input 98/98 PASS; strict legacy ForecastBudget, final catalog and native/product remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
