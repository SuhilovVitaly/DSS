---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0011-aiming-and-launch"
ticket: "EP-0012-US-0011-TK-0003-countermissile-aim-preview"
title: "countermissile-aim-preview"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0011-TK-0002-torpedo-aim-preview"]
files_touched: 6
serves: ["US11-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0011-TK-0003-countermissile-aim-preview

## Why

[Прицеливание и пуск](../EP-0012-US-0011-aiming-and-launch.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: countermissile-aim-preview; доказать US11-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Bounded scope (6 files), updated before code under EpicExecutionPrompt authority. Actual native Alt state and snapped launch point must reach the existing authoritative submission path; separate pure rendering would not satisfy AC3.

| File | Allowed change |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs` | Actual click uses snapped point; modifier state and preview rendering |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CounterAimPreview.cs` | NEW public-observation snapping/threat/arrival/salvo/own-torpedo geometry and render |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Actual Alt down/up and deactivate handling |
| `src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs` | Alt edges precede actions, focus resync preserves held modifier |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | Actual native Alt synchronization before keyboard and launch click |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0003Tests.cs` | NEW analytic geometry, actual input and real transport checks |

A-COUNTER-PREVIEW: threat deadlines/danger segments derive only from BlastThreatMath and displayed positions/velocities. Snapping chooses the nearest forward observed torpedo ray within 12 physical screen pixels, ties ordinal ID. Arrival and salvo are conditional projections, not confirmed interceptions. Own in-flight countermissile prediction may use its own captured fuse; foreign fuses are never read. Missing deadlines remain unknown.

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Заменяет `.ManualDefense.cs:23-61`.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Countermissile_preview_snaps_within_12px_and_alt_disables**: finite solution, late/unreachable; выводятся запас/×N/own-warning.
- **Interception_enroute_badge_is_prediction_not_confirmed_kill**: скрытые enemy detonation inputs не читаются.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0011TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0011TK0003Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualDefense.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US11-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 10 named / 2208 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0011-tk0003-results.json).

Actual counter aim snaps to the nearest forward observed torpedo ray within 12 physical pixels; Alt disables snap through native synchronization and real input. Rendered reticle and Engine-submitted point agree. Shared BlastThreatMath supplies danger/deadline, arrival/spare and late/unreachable labels; projected salvo and own-torpedo warning use current displayed motion. Enroute badge is explicitly conditional and reads only own captured CM fuse plus foreign observation. Input regression 70/70 PASS; native, strict legacy ForecastBudget and final catalog integration remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
