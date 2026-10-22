---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0011-aiming-and-launch"
ticket: "EP-0012-US-0011-TK-0001-aim-mode-and-input-map"
title: "aim-mode-and-input-map"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0010-TK-0004-own-path-forecast-and-fit"]
files_touched: 9
serves: ["US11-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0011-TK-0001-aim-mode-and-input-map

## Why

[Прицеливание и пуск](../EP-0012-US-0011-aiming-and-launch.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: aim-mode-and-input-map; доказать US11-AC1 в границах этой карточки.

## Decisions / Assumptions

A-INPUT-INTEGRATION: scope includes real keyboard startup/composition, command bridge and addressed panel buttons. Consolidate duplicate keyboard poll paths into one ordered key set so JSON remapping is functional, not a decorative config; preserve Ctrl-gated I/S, physical F, press/release and focus resync. Native key buffers64 cover mapped keys. Q/W and panel buttons enter aim even during reload; W prefers first actually ready own counter, then an available non-destroyed counter for aiming. Profile/wheel events are consumed before speed/zoom, RMB/Esc cancels before menu/selection. LMB submits an actual point command only when public readiness permits; rejected reload clicks never queue. Add required GameSessionHandle point submit in this ticket rather than leave a click stub until TK0004; TK0004 retains detailed reasons/feedback/integration polish, preview and friendly-fire confirmation belong to TK0002/3. Point catalog activation remains downstream. Native platform modifier state is synchronized before edges; this avoids losing held Ctrl after focus resync.


D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (9 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:14; public sealed partial class GameSessionScreen : IScreen | Только изменения для scope этой карточки и served AC. |
| `src/DeepSpaceSaga.Client/Data/UI/combat-input.json` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| `src/DeepSpaceSaga.Client/CombatInputSettings.cs` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Client/GameSessionHandle.cs` | Required actual input/transport/button consumer | JSON startup/remapping, own point command submit or concrete launcher entry; preserve legacy branches until cleanup. |
| `src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs` | Required actual input/transport/button consumer | JSON startup/remapping, own point command submit or concrete launcher entry; preserve legacy branches until cleanup. |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | Required actual input/transport/button consumer | JSON startup/remapping, own point command submit or concrete launcher entry; preserve legacy branches until cleanup. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Required actual input/transport/button consumer | JSON startup/remapping, own point command submit or concrete launcher entry; preserve legacy branches until cleanup. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Новый partial `GameSessionScreen.Aiming.cs`, `Data/UI/combat-input.json`, обработка клавиш (`GameSessionScreen.cs:636-707`).
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Aiming_keys_select_launcher_profile_and_cancel**: Q/W,1-3,wheel,LMB,RMB/Esc; profile keys не меняют speed.
- **Reload_aim_does_not_queue_launch**: можно целиться, click при reload не исполняется позже.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0011TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0011TK0001Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/CombatInputSettings.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US11-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 13 named / 2184 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0011-tk0001-results.json).

Actual keyboard/mouse input and module panel buttons enter point aim; GameSessionHandle submits authoritative commands without optimistic projectiles or reload queues. Dynamic readiness recovers when reload ends. Remapped input reaches native tracker; reserved native keys are rejected. Preview and friendly confirmation belong to following tickets. Native and strict legacy ForecastBudget remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
