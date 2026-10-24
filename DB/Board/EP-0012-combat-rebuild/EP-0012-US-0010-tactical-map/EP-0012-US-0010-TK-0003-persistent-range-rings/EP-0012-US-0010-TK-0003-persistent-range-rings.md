---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0010-tactical-map"
ticket: "EP-0012-US-0010-TK-0003-persistent-range-rings"
title: "persistent-range-rings"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0010-TK-0002-projectiles-and-blasts-render"]
files_touched: 5
serves: ["US10-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0010-TK-0003-persistent-range-rings

## Why

[Тактическая карта](../EP-0012-US-0010-tactical-map.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: persistent-range-rings; доказать US10-AC2 в границах этой карточки.

## Decisions / Assumptions

A-RANGE-STATE: actual checkbox visibility state lives in CommandsPanel.cs, so include it in bounded scope. Initialize point-profile launcher ranges once per module/session; clicks override defaults even under hover and hidden panels. Use authoritative captured RangeKm (production70/30), never hardcoded UI weapon constants. Same session module updates preserve choice; new presentation session or new screen resets defaults. Legacy hover/pin behavior remains until cleanup. Actual Engine payload and UI checkbox interactions required.


D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (5 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs:6; public sealed partial class GameSessionScreen | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:8; public sealed partial class GameSessionScreen | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/WeaponPanelInstance.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/WeaponPanelInstance.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/WeaponPanelInstance.cs:4; public sealed record WeaponPanelInstance(string OwnerObjectId, string ModuleId, string Caption) | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Owns actual range checkbox state | Initialize point weapon defaults and reset on session identity, preserving user overrides. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. `GameSessionScreen.Countermeasures.cs:122-146`. Сейчас кольца показываются только при наведении.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Range_rings_default_visible_and_toggle_independently**: 70/30км, per launcher session state; new/load сбрасывает UI cache.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0010TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0010TK0003Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/WeaponPanelInstance.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US10-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 4 named / 2163 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0010-tk0003-results.json).

Baseline2/2 reproduced missing default rings (expected3,actual0). Actual Engine/UI tests now prove70/30/30km defaults, per-launcher checkbox off unaffected by hover, independent reenable, panel hiding preserves state, same-session revision preserves choices and new session resets defaults; DPI1/1.5. Separate public capability inputs80/17km at1/.1ppu prove geometry reads captured ranges rather than UI constants. Point stroke style comes from JSON. Existing legacy hover/pin tests preserved. Client nonbudget2163/2163; strict legacy ForecastBudget and native gates remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
