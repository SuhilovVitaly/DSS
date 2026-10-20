---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0014-balance-and-acceptance"
ticket: "EP-0012-US-0014-TK-0001-production-balance-harness"
title: "production-balance-harness"
stage: "draft"
layer: "engine"
depends_on: ["EP-0012-US-0018-TK-0005-module-combat-regressions"]
files_touched: 4
serves: ["US14-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0014-TK-0001-production-balance-harness

## Why

[Приёмка и баланс](../EP-0012-US-0014-balance-and-acceptance.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: production-balance-harness; доказать US14-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs:5; public sealed class CombatBalanceReport | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs:9; public sealed class ModuleCombatEndToEndTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs:9; public sealed class TorpedoImpactTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0014TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. `tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs`, `ModuleCombatEndToEndTests.cs`. `LegacyWeaponRegistry` (`TorpedoImpactTests.cs:18-58`) удаляется или остаётся только для регрессий, не для баланса.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Production_balance_1000_seeds_reports_corridors**: seeds3001..4000, dodge72–88%, static<=10%, P90<=5.5real min.
- **Production_bot_uses_public_commands_and_production_registry**: no LegacyWeaponRegistry для balance; raw seeds/outcomes сохраняются.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0014TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0014TK0001Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0014TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US14-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.

2026-10-20 execution prerequisite correction: legacy removal follows Save18, forecast and Client migrations; see [current DAG](../../Tickets.md#migration-prerequisite-correction). AC/deletion requirements remain unchanged.
