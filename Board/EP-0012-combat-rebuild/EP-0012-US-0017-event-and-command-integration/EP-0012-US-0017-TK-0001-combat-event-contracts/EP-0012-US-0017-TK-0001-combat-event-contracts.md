---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0017-event-and-command-integration"
ticket: "EP-0012-US-0017-TK-0001-combat-event-contracts"
title: "combat-event-contracts"
stage: "draft"
layer: "contracts"
depends_on: ["EP-0012-US-0001-TK-0005-launcher-profile-schema"]
files_touched: 4
serves: ["US17-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0017-TK-0001-combat-event-contracts

## Why

[Контракты событий, серверные доклады и доставка](../EP-0012-US-0017-event-and-command-integration.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: combat-event-contracts; доказать US17-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs](../../../../src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs) | Существует: src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs:3; public enum CombatEventType | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs](../../../../src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs) | Существует: src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs:7; public enum BattleReportKind | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Contracts/CombatPresentationUpdate.cs](../../../../src/DeepSpaceSaga.Contracts/CombatPresentationUpdate.cs) | Существует: src/DeepSpaceSaga.Contracts/CombatPresentationUpdate.cs:12; public sealed record CombatPresentationUpdate( | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0017TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

Proposed CombatEventType добавляет TorpedoDetonated, CountermissileDetonated, ShotDown, ManuallyDetonated; event payload position+projectile IDs+публичные итоги. При own damage можно передавать module детали; enemy damage numbers запрещены.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Расширить события для TorpedoDetonated и CountermissileDetonated по US3/US4; каждый hit/destroyed элемент имеет object identity и public result. Приватный Engine hit payload не включать в enemy-visible DTO.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: contracts; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Combat_events_roundtrip_and_redact_enemy_details**: новый event envelope сериализуется; чужие module/damage поля отсутствуют.

Matching project: `tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`; proposed test class `PointCombatUS0017TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0017TK0001Tests"
dotnet build tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs src/DeepSpaceSaga.Contracts/CombatPresentationUpdate.cs tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0017TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US17-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
