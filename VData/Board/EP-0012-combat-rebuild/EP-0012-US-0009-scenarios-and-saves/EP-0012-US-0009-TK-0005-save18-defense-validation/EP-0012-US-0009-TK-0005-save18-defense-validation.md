---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0009-scenarios-and-saves"
ticket: "EP-0012-US-0009-TK-0005-save18-defense-validation"
title: "save18-defense-validation"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0009-TK-0004-save18-capture-and-restore"]
files_touched: 9
serves: ["US9-AC3", "US3-AC5"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0009-TK-0005-save18-defense-validation

## Why

[Сценарии и сохранения](../EP-0012-US-0009-scenarios-and-saves.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: save18-defense-validation; доказать US9-AC3, US3-AC5 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (9 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs) | Существует: src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs:8; public sealed record CountermeasureStateData( | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs) | Существует: src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs:9; public sealed record CombatStateData( | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs](../../../../../src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs) | Существует: src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:11; public static class ScenarioLoader | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0005Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Engine/Scenario/PointCombatSaveData.cs` | Existing or bounded new validator | Cross-payload point geometry/identities/RNG/encounter/evasion validation before publication; point-versus-legacy content guard and zero-reload state consistency. |
| `src/DeepSpaceSaga.Engine/Scenario/PointCombatContinuationValidation.cs` | Existing or bounded new validator | Cross-payload point geometry/identities/RNG/encounter/evasion validation before publication; point-versus-legacy content guard and zero-reload state consistency. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs` | Existing or bounded new validator | Cross-payload point geometry/identities/RNG/encounter/evasion validation before publication; point-versus-legacy content guard and zero-reload state consistency. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs` | Existing or bounded new validator | Cross-payload point geometry/identities/RNG/encounter/evasion validation before publication; point-versus-legacy content guard and zero-reload state consistency. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs` | Existing or bounded new validator | Cross-payload point geometry/identities/RNG/encounter/evasion validation before publication; point-versus-legacy content guard and zero-reload state consistency. |

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Перенести проверки сохранений с удаляемого InterceptionMath на captured point-projectile invariants; исключить зависимость новой схемы от старой встречи с целью.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Save18_has_no_interception_helper_dependency**: invalid координаты/времена/владение отвергаются с диагностикой; valid continuation проходит.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0009TK0005Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0009TK0005Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0005Tests.cs
git diff --check
```

## Definition of Done

- Served AC US9-AC3, US3-AC5 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 26 named / 851 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0009-tk0005-results.json).

Actual corrupt file matrices and world/file atomicity PASS; original9 accepted corrupt states and2 accepted wrong catalog-family states preserved as failing baselines. Shared point geometry, RNG/counters, evasion/NPC/encounter/launcher coherence hardened without legacy interception/guidance dependency. Zero-reload overlapping/zero-distance flight roundtrip PASS.2330 non-budget Client PASS; canonical data, full Engine/strict budgets/native OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

Scope correction before implementation: actual file-corruption baseline accepts9 invalid cases and rejects1 empty profile. Add independent point continuation validation using shared point geometry (no legacy guidance/interception) at the existing loader boundary, plus catalog-aware staging guards. Preserve captured in-flight weapons across catalog edits and genuine orphan-owner flights, original integration anchors, optional legacy continuation until cleanup and atomic failure. Counter reload0 is legal in schema and must project immediately Ready while preserving its active flights; fix that boundary in the real shared point launch executor.
