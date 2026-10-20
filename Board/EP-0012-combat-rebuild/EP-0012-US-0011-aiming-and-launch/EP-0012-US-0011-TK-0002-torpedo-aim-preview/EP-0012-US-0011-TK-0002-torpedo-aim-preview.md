---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0011-aiming-and-launch"
ticket: "EP-0012-US-0011-TK-0002-torpedo-aim-preview"
title: "torpedo-aim-preview"
stage: "implemented"
layer: "contracts-engine-client"
depends_on: ["EP-0012-US-0011-TK-0001-aim-mode-and-input-map"]
files_touched: 7
serves: ["US11-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0011-TK-0002-torpedo-aim-preview

## Why

[Прицеливание и пуск](../EP-0012-US-0011-aiming-and-launch.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: torpedo-aim-preview; доказать US11-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Bounded scope (7 files), updated before implementation under EpicExecutionPrompt authority. Actual pointer movement must invalidate confirmation even when it returns to the original point before another render.

| File | Current state / Allowed change |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Actual pointer-move confirmation reset |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs` | Actual input and launch submission |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.TorpedoAimPreview.cs` | NEW actual preview geometry, rendering and friendly guard |
| `src/DeepSpaceSaga.Contracts/CombatSnapshot.cs` | Own launcher effective scatter and core fraction, nullable legacy compatibility |
| `src/DeepSpaceSaga.Engine/SimulationEngine.WeaponOperators.cs` | Compute own current launcher preview capabilities using actual operator/rules |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0002Tests.cs` | NEW independent geometry and real Engine/client input checks |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatSessionFixture.cs` | Optional real registry for nondefault-rule producer validation |

## Public API after the change

LauncherCombatSnapshot appends nullable EffectiveScatterPercent (sigma per km after current operator/rules) and CoreFraction for own preview. Existing ScatterPercent remains module base value. Foreign snapshots still omit launch capabilities; O12 HostileCombat unchanged.

A-PREVIEW-PUBLIC: current client only has base scatter and no configurable blast core. Client must not duplicate Engine rule constants or read private config. Minimal cross-layer public capability is necessary for AC2; producer and actual client consumption are validated together. Scope does not change RNG, launch outcome or NPC forecast policy.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Заменяет предпросмотр пуска `GameSessionScreen.Combat.cs:16-83` и `.CombatForecast.cs`.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Production scope is the bounded public capability and its Engine/Client consumer; matching tests use actual Engine and Client. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Torpedo_preview_is_linear_at_eta_and_has_no_chance**: ring/scatter/trajectory/real ETA и линейные enemy shadows.
- **Friendly_fire_requires_second_click_within_one_second**: другой point/profile/cancel сбрасывает подтверждение.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0011TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0011TK0002Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatForecast.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US11-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 14 named / 2198 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0011-tk0002-results.json).

Actual torpedo aim renders physical R/core/sigma/path, real ETA, each enemy linear shadow and out-of-range warning. Own current rule/operator capabilities cross Contracts/Engine/Client with no RNG draw. Friendly/self blast guard sends only on second click within one real second and resets on changed input. Headless render inspected; label fits 100/150 percent UI. Relevant Engine 785/785 and Contracts 256/256 PASS; full Engine, strict legacy ForecastBudget, native/product remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
