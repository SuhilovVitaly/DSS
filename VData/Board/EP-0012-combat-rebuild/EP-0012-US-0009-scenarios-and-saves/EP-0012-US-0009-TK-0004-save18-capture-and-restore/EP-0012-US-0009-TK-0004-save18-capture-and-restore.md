---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0009-scenarios-and-saves"
ticket: "EP-0012-US-0009-TK-0004-save18-capture-and-restore"
title: "save18-capture-and-restore"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0009-TK-0003-save-format-18"]
files_touched: 9
serves: ["US9-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0009-TK-0004-save18-capture-and-restore

## Why

[Сценарии и сохранения](../EP-0012-US-0009-scenarios-and-saves.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: save18-capture-and-restore; доказать US9-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (9 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs:7; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17; public sealed partial class SimulationEngine : IDisposable | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportPersistence.cs](../../../../../src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportPersistence.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportPersistence.cs:6; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs) | Существует: src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:11; public static class ScenarioLoader | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0004Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs` | Existing or bounded new runtime | Capture/stage/restore point continuation and minimal point-aware validation necessary for a real file roundtrip; legacy branch remains until cleanup. |
| `src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs` | Existing or bounded new runtime | Capture/stage/restore point continuation and minimal point-aware validation necessary for a real file roundtrip; legacy branch remains until cleanup. |
| `src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs` | Existing or bounded new runtime | Capture/stage/restore point continuation and minimal point-aware validation necessary for a real file roundtrip; legacy branch remains until cleanup. |
| `src/DeepSpaceSaga.Engine/Scenario/PointCombatSaveData.cs` | Existing or bounded new runtime | Capture/stage/restore point continuation and minimal point-aware validation necessary for a real file roundtrip; legacy branch remains until cleanup. |

## Public API after the change

GameStateData.PointCombatState is now populated/restored by real Engine Save18. EvasionSaveData adds nullable required IntegrationAnchor to preserve the original integration segment and exact saved continuation; runtime requires it for active evasion. Existing session/command APIs stay unchanged.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Подключить новые payload к CaptureSaveState, atomic Load и RNG init; сохранить прежние экономические manifests, motion quarter remainder и CombatReports marks.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Save18_midflight_resume_matches_uninterrupted**: одинаковый seed/command tape, сравнить итог, позиции, deadlines и RNG counters.
- **Save17_is_rejected_atomically_and_defeated_save_stays_forbidden**: старый мир/слот не заменён.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0009TK0004Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0009TK0004Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportPersistence.cs src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0004Tests.cs
git diff --check
```

## Definition of Done

- Served AC US9-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 16 named / 825 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0009-tk0004-results.json).

Actual file Save18 continuation PASS: own/enemy/counter, exact original integration anchors, evasion/side change/orbit, NPC roll/no-reroll, future scatter, Won/Idle/orphan flight, case normalization, report baseline, held-turn load reset and real defeated slot protection. 2330 non-budget Client PASS. Initial missing payload and rounding/cursor failures preserved. TK5 corruption hardening, canonical data, full Engine/strict budgets/native OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

Scope correction before implementation: the baseline actual Engine file test fails because PointCombatState is null. Real capture/restore necessarily also needs point-aware launcher/projectile/journal validation: Save17 rules reject zero turn rate, missing legacy countermissile ratings and area events without a target. Add those minimal valid-path branches here so the ticket can prove real continuation; TK5 retains corruption/reference/geometry hardening and the explicit no-interception dependency gate. New point state is staged and validated before the lock publishes any mutation. Preserve economic manifests, fractional time, command receipts, report high-water marks, no replay, captured weapons and named RNG. Held turn clears on load per existing contract.
