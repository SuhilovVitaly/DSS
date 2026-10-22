---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0013-combat-state-and-feedback"
ticket: "EP-0012-US-0013-TK-0001-module-diagram"
title: "module-diagram"
stage: "implemented"
layer: "contracts-engine-client"
depends_on: ["EP-0012-US-0012-TK-0003-threat-alerts"]
files_touched: 11
serves: ["US13-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0013-TK-0001-module-diagram

## Why

[Состояние, панели, обратная связь](../EP-0012-US-0013-combat-state-and-feedback.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: module-diagram; доказать US13-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (11 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ModuleDiagram.cs` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:14; public sealed partial class GameSessionScreen : IScreen | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs:12; internal sealed class ObjectLabelRenderer | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Contracts/InstalledModuleSnapshot.cs` | Existing | Own critical projection, embedded diagram, or replacement of obsolete aggregate HP assertions. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Existing | Own critical projection, embedded diagram, or replacement of obsolete aggregate HP assertions. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs` | Existing | Own critical projection, embedded diagram, or replacement of obsolete aggregate HP assertions. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | Existing | Own critical projection, embedded diagram, or replacement of obsolete aggregate HP assertions. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs` | Existing | Own critical projection, embedded diagram, or replacement of obsolete aggregate HP assertions. |
| `tests/DeepSpaceSaga.Client.Tests/CombatMapVisualTests.cs` | Existing | Own critical projection, embedded diagram, or replacement of obsolete aggregate HP assertions. |

| `tests/DeepSpaceSaga.Client.Tests/CompactObjectLabelTests.cs` | Existing | Replace superseded aggregate HP test with compact label/no-HP assertion; regression found 2263 pass / 1 obsolete HP expectation. |

## Public API after the change

InstalledModuleSnapshot.IsCritical: optional false, own projection only. Engine computes it from configured criticalRoles; Client does not duplicate rules.

Scope correction before implementation: embed the schematic in the existing scrollable own-object row; remove HP-dependent label geometry/selection coupling while preserving known ship labels. Replace obsolete bar tests with absence/geometry checks. Cross-layer change is required because critical roles are configurable authoritative rules. No foreign module projection.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Production layers: Contracts -> Engine -> Client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Module_diagram_has_armor_structure_disable_and_critical_border**:9 модулей,3 threshold, own-hit blink/-damage; hull HP bar удалён.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0013TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0013TK0001Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ModuleDiagram.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US13-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 7 named / 2265 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0013-tk0001-results.json).

Own module schematic integrated in existing scrollable row; authoritative configurable IsCritical flag, armor/structure, disabled real countdown, destroyed N/3 and 125ms hit blink/800ms damage label. Aggregate map HP removed, ship-label selection retained. Contracts 256/256. Headless 100/150 percent and compact actual-screen PNG inspected; production catalog/native/product acceptance OPEN. Initial 5/6 viewport check and 2263/2264 obsolete HP expectation retained in evidence; assertions migrated to current contract. Strict legacy forecast budget remains OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
