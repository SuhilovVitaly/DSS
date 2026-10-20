---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0012-maneuver-and-threat-ui"
ticket: "EP-0012-US-0012-TK-0003-threat-alerts"
title: "threat-alerts"
stage: "draft"
layer: "client"
depends_on: ["EP-0012-US-0012-TK-0002-threat-panel"]
files_touched: 5
serves: ["US12-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0012-TK-0003-threat-alerts

## Why

[Манёвр и угрозы в UI](../EP-0012-US-0012-maneuver-and-threat-ui.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: threat-alerts; доказать US12-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (5 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatAlerts.cs` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:14; public sealed partial class GameSessionScreen : IScreen | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json](../../../../src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json) | Существует: src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json:1; { | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs](../../../../src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs) | Существует: src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs:8; public sealed record CombatVisualSettings( | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Threat_alerts_are_rate_limited_and_accessible**: один type/1.5real sec; urgent<=3real sec; толщина/штрих различают классы.
- **Offscreen_hostile_launch_has_edge_indicator**: звук/flash/edge по принятому событию, Load не replay old alerts.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0012TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0012TK0003Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatAlerts.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US12-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
