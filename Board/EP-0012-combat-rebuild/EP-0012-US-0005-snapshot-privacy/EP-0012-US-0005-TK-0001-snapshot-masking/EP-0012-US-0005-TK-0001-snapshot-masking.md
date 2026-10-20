---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0005-snapshot-privacy"
ticket: "EP-0012-US-0005-TK-0001-snapshot-masking"
title: "snapshot-masking"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0017-TK-0002-engine-report-producers"]
files_touched: 5
serves: ["US5-AC1", "US5-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0005-TK-0001-snapshot-masking

## Why

[Сокрытие информации](../EP-0012-US-0005-snapshot-privacy.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: snapshot-masking; доказать US5-AC1, US5-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (5 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17; public sealed partial class SimulationEngine : IDisposable | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs:8; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs:4; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs:6; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0005TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

A-PRIVACY-01: preserve the explicit O12 HostileCombat boundary exactly; its existing AI payload/links are an acknowledged exception and are not used to infer missile endpoints. Foreign point rows carry only the eight observed fields and never retain legacy flight payloads. Sanitize point journal facts at append and point reports at emission: only own target module/numeric losses survive; enemy launch-module/chance/rating fields are omitted. All full/compact frames use the same projector. Actual Engine scenes plus clearly identified mixed-state/extra-metadata boundary probes verify this; point Save18 validation remains its owning gate.

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Проекция снапшота (`SimulationEngine.cs:683-760`) строится с точки зрения игрока для снарядов и модулей врагов. `HostileCombat` в `ObjectMotionSnapshot.cs:131,139` не трогается (O12). Наблюдаемые траектории EP-0011 (`Client/NpcMotionObservationHistory.cs`) должны работать без изменений.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Enemy_secrets_are_absent_from_full_and_compact_scene**: чужие точки/deadlines/module arrays не выходят в snapshot, frames, journal/reports.
- **Own_projectile_and_enemy_public_status_remain_available**: свои captured данные видны, HostileCombat сохраняется.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0005TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0005TK0001Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0005TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US5-AC1, US5-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 5 named / 781 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0005-tk0001-results.json).

Full/compact snapshots hide foreign projectile endpoints/deadlines and legacy flight payloads; point journal/report boundary removes enemy module and damage metadata. Two synthetic boundary probes fail on HEAD baseline and pass after fix; these are defense-in-depth probes, not claims of naturally produced invalid facts. Actual source scenarios cover own/foreign flights and public enemy status. O12 HostileCombat remains public, including its existing module links. Save18 roundtrip remains downstream OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
