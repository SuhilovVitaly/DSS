---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0013-combat-state-and-feedback"
ticket: "EP-0012-US-0013-TK-0004-journal-and-reports-v3"
title: "journal-and-reports-v3"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0013-TK-0003-enemy-badges-and-hit-labels"]
files_touched: 12
serves: ["US13-AC4"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0013-TK-0004-journal-and-reports-v3

## Why

[Состояние, панели, обратная связь](../EP-0012-US-0013-combat-state-and-feedback.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: journal-and-reports-v3; доказать US13-AC4 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (12 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatJournalPanel.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatJournalPanel.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatJournalPanel.cs:6; internal sealed class CombatJournalPanel | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs:6; internal sealed record CombatReportPresentation(string Title, string Message, string Reporter, string Time, | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs:6; public sealed partial class GameSessionScreen | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanel.cs](../../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanel.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanel.cs:8; internal sealed class CombatReportPanel : IDisposable | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0004Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Existing | Pass own identity to journal or replace superseded chance/roll assertions with authoritative geometric/own-health expectations; preserve scrolling/history/reasons/captured identity. |
| `tests/DeepSpaceSaga.Client.Tests/CombatJournalPanelTests.cs` | Existing | Pass own identity to journal or replace superseded chance/roll assertions with authoritative geometric/own-health expectations; preserve scrolling/history/reasons/captured identity. |
| `tests/DeepSpaceSaga.Client.Tests/PhaseThreeJournalUiTests.cs` | Existing | Pass own identity to journal or replace superseded chance/roll assertions with authoritative geometric/own-health expectations; preserve scrolling/history/reasons/captured identity. |
| `tests/DeepSpaceSaga.Client.Tests/PhaseThreeLocaleTests.cs` | Existing | Pass own identity to journal or replace superseded chance/roll assertions with authoritative geometric/own-health expectations; preserve scrolling/history/reasons/captured identity. |
| `tests/DeepSpaceSaga.Client.Tests/CombatReportFormatterTests.cs` | Existing | Pass own identity to journal or replace superseded chance/roll assertions with authoritative geometric/own-health expectations; preserve scrolling/history/reasons/captured identity. |

| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Existing | Localized public profile/radius/result labels used by this journal/report formatter. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Existing | Matching labels; broad combat localization audit remains US16. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

Scope correction before code: journal rendering must receive actual player object identity from the root Render call; do not infer ownership from actor/role. Several existing UI tests explicitly require retired chance/roll formatting. Replace those assertions with new public geometric facts and explicit absence, preserving independent history/scroll/reason/own-health/captured reporter checks. Report DamageDetails is permitted only for PlayerShipDamaged. No Engine producer changes.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Только Client: отобразить новые journal/report события US17, убрать шанс/бросок и не раскрывать чужие damage/module payload. Генерацию серверных событий выполняет US17, не этот тикет.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Journal_and_reports_render_new_events_without_chance_or_roll**: launch/blast/shotdown/hit; bounded history/sequence and captured speaker сохранены.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0013TK0004Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0013TK0004Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatJournalPanel.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanel.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0004Tests.cs
git diff --check
```

## Definition of Done

- Served AC US13-AC4 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 21 named / 2307 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0013-tk0004-results.json).

Journal/report point kind, profile, blast radius, public result and shot-down IDs; retired chance/roll/rating text removed, legacy trial event rendered as neutral Weapon event. Own health requires actual player identity and cache invalidates on identity change; report health only PlayerShipDamaged. Captured speaker, reasons, scroll/collapse and 200-entry sequence-bound history retained. Baseline 15 failures/1 pass reproduced before fix. RU/EN and 100/150 percent actual report component render PASS; inspected PNG is component/headless evidence. Twelve shared localized labels added; full localization audit US16 pending. Native/product, Save18 and strict legacy budget OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
