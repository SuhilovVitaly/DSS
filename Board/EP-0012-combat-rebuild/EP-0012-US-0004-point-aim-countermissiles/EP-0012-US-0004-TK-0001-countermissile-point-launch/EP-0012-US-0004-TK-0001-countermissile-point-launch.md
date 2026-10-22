---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0004-point-aim-countermissiles"
ticket: "EP-0012-US-0004-TK-0001-countermissile-point-launch"
title: "countermissile-point-launch"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0003-TK-0004-manual-detonation"]
files_touched: 4
serves: ["US4-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0004-TK-0001-countermissile-point-launch

## Why

[Противоракеты](../EP-0012-US-0004-point-aim-countermissiles.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: countermissile-point-launch; доказать US4-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Актуальный allowlist остаётся 4 файла: shared executor уже находится в point partial, поэтому legacy ManualDefense.cs заменён этим файлом. Countermeasures.cs адаптирует реальный state builder/projection к profiles, Engine dispatcher/own module projection подключают новую команду. Противоракетный blast выполняется следующим тикетом.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs:8; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs) | Shared point-launch executor | Countermissile kind uses same validation/scatter/capture/motion path with defense readiness. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17; public sealed partial class SimulationEngine : IDisposable | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

Execution refinement: point-defense starts AutoEnabled=false, no burst/legacy forecast; legacy enable/disable rejected on point type. ReadyAt/ReloadDue capture12500ms at launch, independent of live flights; active IDs preserve concurrent flights. Countermissile captures damage/AP=0, common profiles/scatter and fractional endpoint deadline. Old countermeasure path remains only for legacy catalog until explicit cleanup. No separate RNG or countermeasure-only geometry introduced.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Использовать исполнитель пуска из US3-TK2 с видом `Countermissile`.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Countermissile_launch_uses_shared_executor**: 30км принимается, >30 отклоняется; captured profiles и reload12500.
- **Countermissile_rejects_disabled_destroyed_or_unstaffed_launcher**: отказ до RNG/перезарядки.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0004TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0004TK0001Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs src/DeepSpaceSaga.Engine/SimulationEngine.ManualDefense.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US4-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 11 named / 341 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0004-tk0001-results.json).

Countermissile использует общий validation/scatter/capture/motion executor, тот же named stream и отдельную ID sequence. Range30 inclusive проверяется до RNG; profiles wide15/R1 и fast25/R0.5 захвачены, damage/AP0, scatter0.08, ready12500/25000 на boundary. Snapshot profiles/operator доступны владельцу; legacy forecast/burst/auto отсутствуют у point defense. Legacy enable rejected. Invalid/operator/off/disabled/destroyed не меняют RNG/state, replay не повторяет launch. Проверены общий RNG state после torpedo+counter и counter+counter, physical motion/pause. Собственно counter area detonation и удаление legacy механизмов остаются следующими тикетами; общие Engine/Client gates OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
