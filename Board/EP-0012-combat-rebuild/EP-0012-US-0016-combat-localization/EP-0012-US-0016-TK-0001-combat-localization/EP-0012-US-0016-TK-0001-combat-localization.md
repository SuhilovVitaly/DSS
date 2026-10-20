---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0016-combat-localization"
ticket: "EP-0012-US-0016-TK-0001-combat-localization"
title: "combat-localization"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0013-TK-0005-combat-start-timer-summary"]
files_touched: 18
serves: ["US16-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0016-TK-0001-combat-localization

## Why

[Локализация боевого интерфейса](../EP-0012-US-0016-combat-localization.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: combat-localization; доказать US16-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (18 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/Data/Locale/English.json](../../../../src/DeepSpaceSaga.Client/Data/Locale/English.json) | Существует: src/DeepSpaceSaga.Client/Data/Locale/English.json:1; { | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/Data/Locale/Russian.json](../../../../src/DeepSpaceSaga.Client/Data/Locale/Russian.json) | Существует: src/DeepSpaceSaga.Client/Data/Locale/Russian.json:1; { | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/Localization.cs](../../../../src/DeepSpaceSaga.Client/Localization.cs) | Существует: src/DeepSpaceSaga.Client/Localization.cs:11; public static class Localization | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:8; public sealed partial class GameSessionScreen | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0016TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualDefense.cs` | Existing | Actual localized combat consumer, labels/units/profile formatting, current CombatText definition or preservation of label-policy tests. Internal IDs and captured names remain unchanged. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs` | Existing | Actual localized combat consumer, labels/units/profile formatting, current CombatText definition or preservation of label-policy tests. Internal IDs and captured names remain unchanged. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.TorpedoAimPreview.cs` | Existing | Actual localized combat consumer, labels/units/profile formatting, current CombatText definition or preservation of label-policy tests. Internal IDs and captured names remain unchanged. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CounterAimPreview.cs` | Existing | Actual localized combat consumer, labels/units/profile formatting, current CombatText definition or preservation of label-policy tests. Internal IDs and captured names remain unchanged. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Maneuver.cs` | Existing | Actual localized combat consumer, labels/units/profile formatting, current CombatText definition or preservation of label-policy tests. Internal IDs and captured names remain unchanged. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ThreatPanel.cs` | Existing | Actual localized combat consumer, labels/units/profile formatting, current CombatText definition or preservation of label-policy tests. Internal IDs and captured names remain unchanged. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ModuleDiagram.cs` | Existing | Actual localized combat consumer, labels/units/profile formatting, current CombatText definition or preservation of label-policy tests. Internal IDs and captured names remain unchanged. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Existing | Actual localized combat consumer, labels/units/profile formatting, current CombatText definition or preservation of label-policy tests. Internal IDs and captured names remain unchanged. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs` | Existing | Actual localized combat consumer, labels/units/profile formatting, current CombatText definition or preservation of label-policy tests. Internal IDs and captured names remain unchanged. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs` | Existing | Actual localized combat consumer, labels/units/profile formatting, current CombatText definition or preservation of label-policy tests. Internal IDs and captured names remain unchanged. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelText.cs` | Existing | Actual localized combat consumer, labels/units/profile formatting, current CombatText definition or preservation of label-policy tests. Internal IDs and captured names remain unchanged. |
| `tests/DeepSpaceSaga.Client.Tests/ObjectLabelTests.cs` | Existing | Actual localized combat consumer, labels/units/profile formatting, current CombatText definition or preservation of label-policy tests. Internal IDs and captured names remain unchanged. |

| `tests/DeepSpaceSaga.Client.Tests/RangedTorpedoUiTests.cs` | Existing | Preserve range/rejection behavior assertions while selecting expected text through Localization instead of fixed Russian literals. |

## Public API after the change

No public API change; Localization остаётся единым API строк.

Scope correction before code: actual CombatText definition is in ManualDefense.cs, not Combat.cs. Missing locale keys and direct literals span the point previews, input panels, own module scheme and map labels. Redirect CombatText to Localization with no literal fallback; localize render text without changing panel/command/profile identity or Engine catalogs. Known own module captions use type-ID locale keys, while custom/captured names remain data. Add a thread-local test-only locale scope to exercise actual RU/EN rendering without changing runtime settings or introducing live language switching. Broad canonical data activation remains following US2/US9.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. `Data/Locale/English.json`, `Russian.json` и русские `displayName` в JSON.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Combat_locale_key_sets_match_and_have_no_fallback_literals**: RU/EN keys, все отображаемые боевые reasons/labels переведены.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0016TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0016TK0001Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/Localization.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0016TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US16-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 8 named / 2330 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0016-tk0001-results.json).

Actual RU/EN GameSessionScreen renders at 100/150 percent, locale key/uniqueness audit, profile/reason/report translation and preserved internal identities PASS. Headless images inspected; native/GPU/input NOT RUN. Existing strict ForecastBudget failures remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
