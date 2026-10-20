---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0001-contracts-and-schema"
ticket: "EP-0012-US-0001-TK-0002-command-contracts"
title: "command-contracts"
stage: "draft"
layer: "contracts"
depends_on: ["EP-0012-US-0001-TK-0001-projectile-contracts"]
files_touched: 5
serves: ["US1-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0001-TK-0002-command-contracts

## Why

[Контракты и схема данных](../EP-0012-US-0001-contracts-and-schema.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: command-contracts; доказать US1-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (5 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Contracts/PlayerCommand.cs](../../../../src/DeepSpaceSaga.Contracts/PlayerCommand.cs) | Существует: src/DeepSpaceSaga.Contracts/PlayerCommand.cs:6; public sealed record PlayerCommand( | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Contracts/CombatSnapshot.cs](../../../../src/DeepSpaceSaga.Contracts/CombatSnapshot.cs) | Существует: src/DeepSpaceSaga.Contracts/CombatSnapshot.cs:7; public static class CombatCommandTypes | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs](../../../../src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs) | Существует: src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs:6; public enum CountermeasurePhase { Guiding, MissedCoast } | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Contracts/CommandResult.cs](../../../../src/DeepSpaceSaga.Contracts/CommandResult.cs) | Существует: src/DeepSpaceSaga.Contracts/CommandResult.cs:23; public enum CommandResultStatus | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0001TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

Proposed: PlayerCommand дополняется string? ProfileId = null, string? ThreatProjectileId = null, EvadeSide? EvadeSide = null. enum EvadeSide { Left, Right }. TargetWorldX/Y переиспользуются; конечные координаты обязательны. Константы новых команд и причин из US1-AC2; лишние старые поля убираются после consumer migration.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. **Задача.**
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: contracts; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Commands_roundtrip_preserves_explicit_point_profile_and_threat**: координаты, ProfileId, ThreatProjectileId и EvadeSide передаются без изменения.
- **Reason_codes_are_unique_and_stable**: все перечисленные коды уникальны, отклонённый пуск не создаёт receipt успеха.

Matching project: `tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`; proposed test class `PointCombatUS0001TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0001TK0002Tests"
dotnet build tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/PlayerCommand.cs src/DeepSpaceSaga.Contracts/CombatSnapshot.cs src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs src/DeepSpaceSaga.Contracts/CommandResult.cs tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0001TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US1-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
