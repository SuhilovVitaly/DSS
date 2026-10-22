---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0013-combat-state-and-feedback"
ticket: "EP-0012-US-0013-TK-0005-combat-start-timer-summary"
title: "combat-start-timer-summary"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0013-TK-0004-journal-and-reports-v3"]
files_touched: 13
serves: ["US13-AC5"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0013-TK-0005-combat-start-timer-summary

## Why

[Состояние, панели, обратная связь](../EP-0012-US-0013-combat-state-and-feedback.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: combat-start-timer-summary; доказать US13-AC5 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (13 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:14; public sealed partial class GameSessionScreen : IScreen | Только изменения для scope этой карточки и served AC. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatSummaryPanel.cs` | **NEW — proposed**, сейчас отсутствует | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/Defeat/DefeatScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/Defeat/DefeatScreen.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/Defeat/DefeatScreen.cs:8; public sealed class DefeatScreen : IScreen | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/Defeat/DefeatLayout.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/Defeat/DefeatLayout.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/Defeat/DefeatLayout.cs:6; internal readonly record struct DefeatLayout(SKRect Panel, SKRect Load, SKRect MainMenu, float Scale) | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0005Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Encounter.cs` | NEW | Actual encounter HUD, hit/fit integration, session-bound start cue, native defeat/new-game/load wiring or localized summary labels. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs` | Existing | Actual encounter HUD, hit/fit integration, session-bound start cue, native defeat/new-game/load wiring or localized summary labels. |
| `src/DeepSpaceSaga.Client/UI/CombatAlertAudio.cs` | Existing | Actual encounter HUD, hit/fit integration, session-bound start cue, native defeat/new-game/load wiring or localized summary labels. |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | Existing | Actual encounter HUD, hit/fit integration, session-bound start cue, native defeat/new-game/load wiring or localized summary labels. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Existing | Actual encounter HUD, hit/fit integration, session-bound start cue, native defeat/new-game/load wiring or localized summary labels. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Existing | Actual encounter HUD, hit/fit integration, session-bound start cue, native defeat/new-game/load wiring or localized summary labels. |

| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatPanel.cs` | Existing | Regression found reciprocal layout dependency: threat panel measured last banner, banner measured threat panel. Measure threat from common base without encounter HUD, then place HUD around current threat panel. Keep old no-click-through test intact. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0003Tests.cs` | Existing | Admission test now expects explicit EnemyLaunch plus CombatStarted cues; preserve pre-admission silence, exact launch cue, flash and offscreen indicator checks. |

## Public API after the change

GameSessionScreen optional playInitialCombatStart flag (native new game true, load false); DefeatScreen optional captured CombatEncounterSnapshot; append CombatStarted audio cue. Other details internal.

Scope correction before code: keep encounter presentation in a partial, include summary/banner in hit testing and map obstacles. Wire existing concrete audio session and actual SkiaWindow defeat transition; constructor-only stats would not satisfy integration. Timer is authoritative physical DurationSeconds times four (nominal x1 real, excluding pauses), plus displayed Active interpolation; completed duration freezes. Won Continue sends existing acknowledgement and waits for admission, never mutates encounter locally. Broad localization audit remains US16.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Combat_banner_timer_summary_and_defeat_stats**: Active banner, real-seconds timer, Won немодальный Continue, Defeat stats.
- **Combat_speed_buttons_are_disabled_with_reason**: x5+ rejected visually; сервер остаётся владельцем lock.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0013TK0005Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0013TK0005Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatSummaryPanel.cs src/DeepSpaceSaga.Client/UI/Screens/Defeat/DefeatScreen.cs src/DeepSpaceSaga.Client/UI/Screens/Defeat/DefeatLayout.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0005Tests.cs
git diff --check
```

## Definition of Done

- Served AC US13-AC5 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 15 named / 2322 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0013-tk0005-results.json).

Admitted Active banner/frame and x1 real timer, one-shot configured native CombatStarted cue (new game opt-in, load baseline silent), nonmodal scrollable Won stats/Continue, captured Defeat stats through actual SkiaWindow transition, disabled x5+ and localized reason. Actual hostile encounter -> real own launch/death/Won -> aiming stays available -> double-click Continue sends one executed acknowledgement, no local state mutation or underlying launch. Related threat/HUD 58/58 PASS. Fixed reciprocal threat/banner layout found by regression; original no-click-through assertions retained. Initial audio assertion updated to explicitly check both admitted cue kinds. Headless layouts/scroll/100-150 percent images inspected; native/heard audio NOT RUN. Story automated review recorded; canonical data/native/product/strict budget OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
