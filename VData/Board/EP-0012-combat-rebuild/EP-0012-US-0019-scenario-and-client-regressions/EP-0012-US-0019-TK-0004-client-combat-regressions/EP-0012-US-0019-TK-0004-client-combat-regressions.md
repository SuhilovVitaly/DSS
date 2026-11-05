---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0019-scenario-and-client-regressions"
ticket: "EP-0012-US-0019-TK-0004-client-combat-regressions"
title: "client-combat-regressions"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0019-TK-0002-scenario-engine-fixtures"]
files_touched: 8
serves: ["US19-AC1", "US19-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0019-TK-0004-client-combat-regressions

## Why

[Совместимость сценариев и старых клиентских поверхностей](../EP-0012-US-0019-scenario-and-client-regressions.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: client-combat-regressions; доказать US19-AC1, US19-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (8 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [tests/DeepSpaceSaga.Client.Tests/BasicCombatUiFlowTests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/BasicCombatUiFlowTests.cs) | Существует: tests/DeepSpaceSaga.Client.Tests/BasicCombatUiFlowTests.cs:15; public sealed class BasicCombatUiFlowTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Client.Tests/PhaseThreeCombatUiFlowTests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/PhaseThreeCombatUiFlowTests.cs) | Существует: tests/DeepSpaceSaga.Client.Tests/PhaseThreeCombatUiFlowTests.cs:13; public class PhaseThreeCombatUiFlowTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs) | Существует: tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs:8; public sealed class CombatScenariosContentTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Client.Tests/CombatForecastCacheTests.cs](../../../../../tests/DeepSpaceSaga.Client.Tests/CombatForecastCacheTests.cs) | Существует: tests/DeepSpaceSaga.Client.Tests/CombatForecastCacheTests.cs:13; public sealed class CombatForecastCacheTests(ITestOutputHelper output) | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0004Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `tests/DeepSpaceSaga.Client.Tests/CountermeasureUiFlowTests.cs` | Shares the old Basic UI fixture and asserts retired auto toggles | Migrate to real point-launch/detonate/reload UI and launcher isolation. |

| `tests/DeepSpaceSaga.Client.Tests/PointCombatUiFixture.cs` | NEW | Real current source catalog/session plus rendered UI, controlled public poses and injected clocks; no legacy catalog. |

| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | OnMouseDown admits only Left/Right although bindings validate Middle | Admit Middle and cover the actual window handler through the existing headless constructor; native acceptance remains NOT RUN. |

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Перевести старые UI tests с chance/fire/mode/auto-PRO на point-aim inputs, privacy и new command reasons; не ослаблять синхронизацию.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Combat_ui_regression_uses_new_commands_and_preserves_scene_time**: user-visible значения, receipts, pause/load и masking покрыты.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0019TK0004Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0019TK0004Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/BasicCombatUiFlowTests.cs tests/DeepSpaceSaga.Client.Tests/PhaseThreeCombatUiFlowTests.cs tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs tests/DeepSpaceSaga.Client.Tests/CombatForecastCacheTests.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0004Tests.cs
git diff --check
```

## Definition of Done

- Served AC US19-AC1, US19-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-21: implementation и self-review выполнены. PASS 1 named / 2363 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0019-tk0004-results.json).

Current canonical catalog UI: actual point launch/reload/blast, captured endpoint unaffected by selection/owner turn, coarse masked hit facts, two independent counters, double-click guard, stationary visible wreck and real-time effect lifetime across viewport/scale variants. Real window-handler Middle binding failure reproduced before the one-line admission fix; final native-entry headless check PASS. Client2363/2363 non-budget; point-budget3/3 with p99 .2606/.1716/.0784ms <=2ms. Legacy synthetic planner characterization and old ManualDefensePreview budget remain pending US18 cleanup; no all-budget/full Engine/native/balance acceptance claim.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

2026-10-20 consumer prerequisite correction: point consumers precede production data activation; actual integration gates remain OPEN until activation/Save18. See [DAG](../../Tickets.md#consumer-prerequisite-correction).

Execution scope correction2026-10-21: current UI scenarios use the canonical active catalog. Old target-binding/homing/chance/auto-toggle assertions are retired with those contracts; preserve receipt/scene separation, pause, reload, no retarget, two-launcher independence, viewport/scale and effect lifetime in point equivalents. Cache changing-target budget uses8point torpedoes plus4point counters at the unchanged2ms p99 gate; failed old homing measurements remain historical evidence. Remaining synthetic legacy planner/cache cases stay explicit characterization until US18-TK2/US3-TK5 removal. Existing US19-TK3 point invalidation cases remain in the regression set for endpoints/session/options/removal. Point captured paths intentionally reuse a still-valid future across unrelated observed NPC changes; a cache hit need not rebase its start timestamp.
