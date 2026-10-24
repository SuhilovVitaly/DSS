---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0017-event-and-command-integration"
ticket: "EP-0012-US-0017-TK-0003-local-client-delivery"
title: "local-client-delivery"
stage: "implemented"
layer: "local-client"
depends_on: ["EP-0012-US-0005-TK-0002-hidden-fields-leak-test"]
files_touched: 4
serves: ["US17-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0017-TK-0003-local-client-delivery

## Why

[Контракты событий, серверные доклады и доставка](../EP-0012-US-0017-event-and-command-integration.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: local-client-delivery; доказать US17-AC3 в границах этой карточки.

## Decisions / Assumptions

A-TRANSPORT: add reusable real Engine point-catalog/scenario fixture in Client.Tests, since production catalog activation is intentionally downstream. Fixture changes registry weapon fields but does not mock execution, transport, events or removal. Read actual local connection updates and acknowledgements; deliberately ignore coarse full snapshots while verifying every ordered frame and explicit removal. Existing generic transport already carries new DTOs, so change production bridge only if an observed defect requires it. Native rendering and Save18 remain separate gates.


D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs](../../../../../src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs) | Существует: src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs:13; public sealed class LocalGameSessionConnection : IGameSessionConnection, ICombatPresentationConnection | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine.LocalClient/SnapshotMailbox.cs](../../../../../src/DeepSpaceSaga.Engine.LocalClient/SnapshotMailbox.cs) | Существует: src/DeepSpaceSaga.Engine.LocalClient/SnapshotMailbox.cs:8; internal sealed class SnapshotMailbox | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0017TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `tests/DeepSpaceSaga.Client.Tests/PointCombatSessionFixture.cs` | NEW reusable fixture with real Engine/content and deterministic real clock | Point catalog setup and actual local transport integration. |

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Провести новые snapshots/events через существующий in-process bridge; не менять sync Engine/Client boundary и приоритет command acceptance.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: local-client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Point_payload_survives_local_session_and_explicit_removal**: integration create/send/read даёт own/observed/encounter; skipped full snapshots не теряют факт удаления.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0017TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0017TK0003Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs src/DeepSpaceSaga.Engine.LocalClient/SnapshotMailbox.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0017TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US17-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 1 named / 2124 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0017-tk0003-results.json).

Real point Engine/LocalClient create-submit-read proves own/observed/encounter, strictly ordered update sequences, actual reports, immediate combat_speed_locked acknowledgement, paused manual detonation, explicit compact removal without coarse snapshots, replay receipt without repeated physical fact, and mailbox coalescing without resurrection/lost receipt/report. Generic bridge already transports new immutable DTOs and required no production rewrite. Client bounded set excludes separately OPEN ForecastBudget gate; this is not full-Client acceptance. Initial extra test wrongly expected cumulative history in a delta; actual failed run and correction are retained. Point catalog fixture prepares downstream activation; Save18/native remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
