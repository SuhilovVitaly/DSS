---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0005-snapshot-privacy"
ticket: "EP-0012-US-0005-TK-0002-hidden-fields-leak-test"
title: "hidden-fields-leak-test"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0005-TK-0001-snapshot-masking"]
files_touched: 2
serves: ["US5-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0005-TK-0002-hidden-fields-leak-test

## Why

[Сокрытие информации](../EP-0012-US-0005-snapshot-privacy.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: hidden-fields-leak-test; доказать US5-AC3 в границах этой карточки.

## Decisions / Assumptions

A-JSON-PRIVACY: validate actual serialized full snapshots and compact frames by foreign object subtree while proving own endpoints remain available. Use known/unknown enemy scenarios with private module sentinels; preserve O12 HostileCombat exception covered by TK0001. Reload_reset means actual LoadScenario session reset, bootstrap and following delta; Save18 save/load roundtrip remains a separate US9 integration gate and is not claimed here. Existing masking already implements production behavior, so no gratuitous Presentation change is required.


D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (2 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs:8; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0005TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Тест US5-AC3.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Serialized_payload_does_not_leak_enemy_fields**: active own/enemy projectiles; поиск секретов по ID/поддереву, а не глобальный запрет detonation-полей своей торпеды.
- **Reload_reset_and_delta_have_same_privacy**: после Load/first frame/compact update masking сохраняется.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0005TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0005TK0002Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0005TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US5-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 3 named / 785 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0005-tk0002-results.json).

Serialized actual known/unknown enemy and own/foreign active flights. Foreign rows have exactly eight observed fields, no private module sentinels, endpoints or deadlines; own endpoint remains present. Actual LoadScenario creates a new session/bootstrap without old flights, then compact updates preserve masking. Intermediate pre-command frames may precede own launch; final scene explicitly requires it. Unknown enemy status remains undisclosed. Save18 roundtrip is separate OPEN integration, not claimed by scenario-reset tests. O12 remains as covered in TK0001; no redundant production change.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
