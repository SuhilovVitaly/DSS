---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0018-legacy-combat-cleanup"
ticket: "EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests"
title: "migrate-mode-runtime-tests"
stage: "draft"
layer: "engine"
depends_on: ["EP-0012-US-0018-TK-0001-remove-interception-helper"]
files_touched: 4
serves: ["US18-AC1", "US18-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests

## Why

[Удаление старой боевой модели и регрессии](../EP-0012-US-0018-legacy-combat-cleanup.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: migrate-mode-runtime-tests; доказать US18-AC1, US18-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs](../../../../src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs) | Существует: src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs:9; internal sealed class GameDataRegistry | Только изменения для scope этой карточки и served AC. |
| [tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs:7; public sealed class FireModeRuntimeTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs:7; public sealed class SetFireModeTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Заменить старые mode/setMode tests на отрицательные проверки legacy commands и новые профили; обновить runtime/registry defaults без сохранения режима standard.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Legacy_commands_are_rejected_and_profiles_are_captured**: никакого mode bonus/breakdown RNG в новой модели.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0018TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0018TK0002Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US18-AC1, US18-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
