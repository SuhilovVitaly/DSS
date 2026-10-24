---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0017-event-and-command-integration"
ticket: "EP-0012-US-0017-TK-0004-turn-and-summary-command-contracts"
title: "turn-and-summary-command-contracts"
stage: "implemented"
layer: "contracts"
depends_on: ["EP-0012-US-0017-TK-0001-combat-event-contracts"]
files_touched: 6
serves: ["US17-AC4"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0017-TK-0004-turn-and-summary-command-contracts

## Why

[Контракты событий, серверные доклады и доставка](../EP-0012-US-0017-event-and-command-integration.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: turn-and-summary-command-contracts; доказать US17-AC4 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Актуальный allowlist (6 файлов, включая tests); расширение обосновано ниже. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Contracts/PlayerCommand.cs](../../../../../src/DeepSpaceSaga.Contracts/PlayerCommand.cs) | Существует: src/DeepSpaceSaga.Contracts/PlayerCommand.cs:6; public sealed record PlayerCommand( | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Contracts/ShipEngineCommandTypes.cs](../../../../../src/DeepSpaceSaga.Contracts/ShipEngineCommandTypes.cs) | Существует: src/DeepSpaceSaga.Contracts/ShipEngineCommandTypes.cs:4; public static class ShipEngineCommandTypes | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Contracts/CommandResult.cs](../../../../../src/DeepSpaceSaga.Contracts/CommandResult.cs) | Существует: src/DeepSpaceSaga.Contracts/CommandResult.cs:23; public enum CommandResultStatus | Только изменения для scope этой карточки и served AC. |
| `src/DeepSpaceSaga.Contracts/CombatEncounterSnapshot.cs` | Exists after US1-TK3 | Add encounter command constants. |
| `tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0017TK0004Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |
| `src/DeepSpaceSaga.Contracts/CombatPresentationUpdate.cs` | Existing SimulationControlAcknowledgement | Optional ReasonCode for structured speed rejection. |

## Public API after the change

Proposed PlayerCommand: TurnDirection? TurnDirection = null, bool? Active = null; enum TurnDirection { Left, Right }; engine.turnHold использует оба поля, combat.acknowledgeResults подтверждает текущие итоги. combat_speed_locked — CommandReasonCodes.

### Уточнение исполнения — 2026-10-20

Dependency US17-TK1 опубликован: `7abd93815e6765586f5cc684752cec887ca18c4e`, remote SHA проверен. После проверки `SetPresentationSpeedAsync` обнаружено, что control acknowledgement не имеет поля причины отказа. Scope расширен на один существующий Contracts файл: nullable ReasonCode передаёт `combat_speed_locked`, сохраняя подтверждённые speed/time/session; null сохраняет прежний success payload. Реальное заполнение отказа принадлежит Engine/LocalClient тикетам, не этому DTO шагу.

Active=null отличается от Active=false (release). Отсутствующие direction/active не превращаются в манёвр. Дополнительный `invalid_turn_payload` задаёт стабильную причину отсутствующего/некорректного direction/active; actual rejection и отсутствие side effects проверяются серверными тестами US-0006. Contracts только переносит payload, в том числе неизвестный enum value для authoritative отказа.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Добавить constants/payload continuous turn и ack итогов, reason combat_speed_locked; rejected payload не считается выполненным.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: contracts; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

План проверок; фактические результаты исполнения приведены в Status / Evidence.

- **Turn_hold_and_summary_ack_roundtrip_with_explicit_direction**: неизвестные значения отклоняются Engine; defaults не запускают манёвр.

Matching project: `tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`; proposed test class `PointCombatUS0017TK0004Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0017TK0004Tests"
dotnet build tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/PlayerCommand.cs src/DeepSpaceSaga.Contracts/ShipEngineCommandTypes.cs src/DeepSpaceSaga.Contracts/CommandResult.cs src/DeepSpaceSaga.Contracts/CombatEncounterSnapshot.cs tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0017TK0004Tests.cs
git diff --check
```

## Definition of Done

- Served AC US17-AC4 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: Contracts implementation и self-review выполнены. Named tests PASS 6/6; полный Contracts PASS 251/251 (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Общая сборка всех consumers PASS, 0 warnings/errors, 16.31s. Процессы hard cap 300000 ms.

[Команды, stdout, solution build](../../evidence/us0017-tk0004-results.json). Self-review: null active не равен false/release; обе стороны и оба active значения roundtrip; unknown enum не получает implicit direction; ack не запускает hold; код отказа speed-control сохраняет подтверждённый speed, не requested. Реальная Engine validation/lock и LocalClient заполнение причины — последующие тикеты, не скрытая логика Contracts. Review исполнителя, не независимое; native/product/balance OPEN. SHA проверяется после отдельного commit/push.
