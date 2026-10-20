---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0003-point-aim-torpedoes"
ticket: "EP-0012-US-0003-TK-0004-manual-detonation"
title: "manual-detonation"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0003-TK-0003-torpedo-area-detonation"]
files_touched: 4
serves: ["US3-AC4"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0003-TK-0004-manual-detonation

## Why

[Торпеда в точку](../EP-0012-US-0003-point-aim-torpedoes.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: manual-detonation; доказать US3-AC4 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Актуальный allowlist 4 файлов: point partial вместо legacy Combat.cs, dispatcher и два matching test файла. Общий point fixture дополняется настоящим command definition/module command binding для torpedo.detonate; это необходимая integration fixture, без обхода content validation.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs) | Shared area resolver | Manual trigger uses same damage path at current physical position. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17; public sealed partial class SimulationEngine : IDisposable | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0004Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |
| [tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0002Tests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0002Tests.cs) | Point registry fixture | Add real torpedo.detonate definition/binding for all point integration cases. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

Execution refinement: TargetObjectId — explicit projectile ID, ObjectId/ModuleId должны соответствовать captured owner/launcher. Новая команда требует binding на launcher type, но не сбрасывает reload и не требует готовности/питания аппарата для уже выпущенного снаряда. Один event ManuallyDetonated обозначает фактический blast, без второго duplicate TorpedoDetonated. Shared resolver получает выбранный projectile отдельно, без изменения captured endpoint/deadline. Legacy selfDestruct остаётся до cleanup.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Команда `torpedo.detonate` по US3-AC4. Заменяет `selfDestruct`.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Manual_detonation_uses_current_position**: своя торпеда даёт один blast на текущей физической позиции.
- **Foreign_or_finished_projectile_rejected_without_effect**: not_own_projectile для чужой, завершённая не взрывается повторно.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0003TK0004Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0003TK0004Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0004Tests.cs
git diff --check
```

## Definition of Done

- Served AC US3-AC4 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 4 named / 289 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0003-tk0004-results.json).

Настоящий command dispatch использует тот же area resolver в текущей позиции, без изменения captured endpoint/deadline, reload или launch RNG. Один ManuallyDetonated fact; duplicate receipt не повторяет blast; foreign/wrong launcher и stale ID дают отказ без mutation. Проверен реальный hit по enemy в текущей позиции и отсутствие hit в старой конечной точке. Manual self-damage сохраняет SessionDefeated; обнаруженная clock projection устранена: BuildSnapshot после command-triggered death сразу использует DefeatedClock/Speed0. Действующие Defeat/command replay/legacy combat regressions включены в bounded289. Общие Engine/Client gates остаются OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
