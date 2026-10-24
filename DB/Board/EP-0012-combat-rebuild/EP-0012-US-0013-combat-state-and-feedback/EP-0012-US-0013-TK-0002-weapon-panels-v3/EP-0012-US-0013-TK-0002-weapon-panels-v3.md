---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0013-combat-state-and-feedback"
ticket: "EP-0012-US-0013-TK-0002-weapon-panels-v3"
title: "weapon-panels-v3"
stage: "implemented"
layer: "contracts-engine-client"
depends_on: ["EP-0012-US-0013-TK-0001-module-diagram"]
files_touched: 9
serves: ["US13-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0013-TK-0002-weapon-panels-v3

## Why

[Состояние, панели, обратная связь](../EP-0012-US-0013-combat-state-and-feedback.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: weapon-panels-v3; доказать US13-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (9 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs:15; public sealed class CommandsPanel | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/WeaponPanelInstance.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/WeaponPanelInstance.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/WeaponPanelInstance.cs:4; public sealed record WeaponPanelInstance(string OwnerObjectId, string ModuleId, string Caption) | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:8; public sealed partial class GameSessionScreen | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs:6; public sealed partial class GameSessionScreen | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs` | Existing | Actual point-panel callback integration or own effective scatter projection. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Existing | Actual point-panel callback integration or own effective scatter projection. |
| `src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs` | Existing | Actual point-panel callback integration or own effective scatter projection. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs` | Existing | Actual point-panel callback integration or own effective scatter projection. |

## Public API after the change

DefenseSnapshot.EffectiveScatterPercent optional own field; CommandsPanel optional label and physical multiplier callbacks, appended geometry fields for reload ring and real countdown.

Scope correction before code: callbacks live in Aiming.cs and are wired by GameSessionScreen.cs. Effective counter scatter must be projected by Engine using current operator/configured rules, matching torpedo projection; Client must not duplicate this formula. Ring progress uses confirmed readyAt and displayed physical interpolation, never optimistic launch state.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. `Controls/CommandsPanel.cs:60-79, 681-717`, `GameSessionScreen.Combat.cs:345-425`.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Production layers Contracts -> Engine -> Client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Weapon_panels_show_profile_reload_and_operator_without_rolls**: reload error<=.1с, ring toggle и manual detonate.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0013TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0013TK0002Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/WeaponPanelInstance.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US13-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 8 named / 2273 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0013-tk0002-results.json).

Point weapon panels: circular confirmed reload progress with 0.1 real-second boundary, profile selection, operator accuracy, base/effective scatter, state, configured hotkeys, per-module range toggle, own-flight detonation. Counter sigma is an optional own Engine projection with nondefault-rule producer evidence. Real mouse selection -> panel command -> LocalClient -> Engine ManuallyDetonated and scene removal PASS. Contracts 256/256; actual-screen PNG inspected. Initial fixture cardinality and premature overlap selection failures retained, no assertion weakened. Canonical data/native/product and strict legacy budget OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
