---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0016-combat-localization"
ticket: "EP-0012-US-0016-TK-0001-combat-localization"
title: "combat-localization"
stage: "draft"
layer: "client"
depends_on: ["EP-0012-US-0013-TK-0005-combat-start-timer-summary"]
files_touched: 5
serves: ["US16-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0016-TK-0001-combat-localization

## Why

[Локализация боевого интерфейса](../EP-0012-US-0016-combat-localization.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: combat-localization; доказать US16-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (5 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/Data/Locale/English.json](../../../../src/DeepSpaceSaga.Client/Data/Locale/English.json) | Существует: src/DeepSpaceSaga.Client/Data/Locale/English.json:1; { | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/Data/Locale/Russian.json](../../../../src/DeepSpaceSaga.Client/Data/Locale/Russian.json) | Существует: src/DeepSpaceSaga.Client/Data/Locale/Russian.json:1; { | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/Localization.cs](../../../../src/DeepSpaceSaga.Client/Localization.cs) | Существует: src/DeepSpaceSaga.Client/Localization.cs:11; public static class Localization | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs:8; public sealed partial class GameSessionScreen | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0016TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; Localization остаётся единым API строк.

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

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
