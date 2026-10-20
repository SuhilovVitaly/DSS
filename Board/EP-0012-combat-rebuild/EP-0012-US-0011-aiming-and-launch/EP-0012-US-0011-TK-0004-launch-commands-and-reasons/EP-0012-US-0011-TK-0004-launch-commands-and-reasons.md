---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0011-aiming-and-launch"
ticket: "EP-0012-US-0011-TK-0004-launch-commands-and-reasons"
title: "launch-commands-and-reasons"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0011-TK-0003-countermissile-aim-preview"]
files_touched: 7
serves: ["US11-AC4"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0011-TK-0004-launch-commands-and-reasons

## Why

[Прицеливание и пуск](../EP-0012-US-0011-aiming-and-launch.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: launch-commands-and-reasons; доказать US11-AC4 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Bounded scope (7 files), updated before implementation. GameSessionHandle already sends exact point/profile/module through actual transport in TK0001; TK0004 validates it and completes state/reason handling, without rewriting the bridge. A-FIXTURE-RENDER: related regression exposed a real bootstrap race in test helpers: packet could arrive after Render but before the wait predicate, leaving camera at initial (10000,10000) and no rendered player. Helpers must wait for the actual rendered player, retaining every command/result assertion. This is render setup evidence, not a relaxed launch result.

| File | Allowed change |
|---|---|
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0001Tests.cs` | Bootstrap helper waits for actual rendered player |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0002Tests.cs` | Same proven render/bootstrap helper race |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0003Tests.cs` | Same actual transport/bootstrap readiness |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs` | Pending/accepted/admitted state and localized reason fallback; retain authoritative submission from TK0001 |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Point-launch reasons and feedback strings |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Matching Russian reason/feedback strings |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0004Tests.cs` | NEW real transport command binding, immediate feedback, scene admission and rejection tests |

A-LAUNCH-FEEDBACK: queue acceptance and execution receipts bypass delayed scene. A merely newer unrelated scene revision cannot prove launch admission. Pending feedback must remain until an admitted own launch fact/capability is visible; no speculative projectile or automatic retry. Locale edits for served AC4 belong here; full epic localization stays in US16.

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Validate actual point submission and finish localized feedback/receipt-to-scene admission state.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Launch_sends_explicit_point_profile_and_selected_module**: stale selection не меняет CommandId binding.
- **Click_feedback_under_100ms_and_projectile_awaits_scene**: D-USER-02; no speculative authoritative projectile.
- **Rejected_launch_shows_localized_reason_and_no_queued_retry**: все codes локализованы.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0011TK0004Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0011TK0004Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/GameSessionHandle.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0004Tests.cs
git diff --check
```

## Definition of Done

- Served AC US11-AC4 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 7 named / 2215 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0011-tk0004-results.json).

Point commands retain exact point/profile/module binding across UI context changes; click/feedback <=100 ms with no speculative projectile. Pending accepted state survives unrelated newer scene revisions; negative control restoring the original guard fails expected Accepted vs actual Ready. Server reload refusal is localized and not retried, and clears after admitted readiness. Russian/English source/deployed launch-reason coverage and generic fallback PASS. Related bootstrap helpers now wait for rendered player after proven default-camera race; original assertions retained, input regression 77/77 PASS. Strict legacy ForecastBudget, native/product and final catalog integration remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
