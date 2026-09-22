---
epic: EP-0001-trading-system
story: EP-0001-US-0013-economy-balance-evidence
ticket: EP-0001-US-0013-TK-0001-balance-diagnostic-seam
title: Детерминированный Engine seam для balance tooling
stage: approved
layer: engine
depends_on: [EP-0001-US-0012-TK-0004-deterministic-economy-continuation]
files_touched: 2
serves: [AC-01, AC-05]
created: 2026-09-21T14:55:45Z
revision: 1
---

# Детерминированный Engine seam для balance tooling

## Why

Разрешить отдельному in-repo balance tool управляемо продвигать Engine по явному календарному времени и снимать save/snapshot без ожидания wall clock. End state: assembly `DeepSpaceSaga.EconomyBalance` использует уже существующие internal explicit-time seams, а Engine regression доказывает почасовую эквивалентность, отсутствие motion-time подмены и совпадение непрерывного/Save/Load-прогона.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`.

## Decisions

Единственное сообщение пользователя: `сделай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0013-economy-balance-evidence\EP-0001-US-0013-economy-balance-evidence.md` (`2026-09-21T14:55:45Z`). Технических решений пользователя нет.

## Assumptions

- Balance tool является доверенным repository tooling, как существующий `DeepSpaceSaga.Performance`, поэтому friend assembly допустим без public gameplay API.
- Upstream TK-0004 сохраняет `CaptureSnapshotForTests`/`CaptureSaveStateForTests` или эквивалентную explicit-time семантику. Если сигнатуры изменены, этот тикет возвращается в review до правки allowlist.
- Tool передаёт неубывающие целые часы; rewind и произвольная mutation runtime не поддерживаются.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj` | Friend assemblies перечислены в `:14–18`; tooling friend существует только для `DeepSpaceSaga.Performance` | Добавить ровно `InternalsVisibleTo Include="DeepSpaceSaga.EconomyBalance"`; остальные references/properties не менять |
| `tests/DeepSpaceSaga.Engine.Tests/EconomyBalanceSeamTests.cs` | Новый файл; explicit-time seams находятся в `SimulationEngine.cs:728–734,1676–1682`, world update — `:393–421` | Regression на hourly stepping, calendar/motion separation и Save/Load continuation после US-0012 |

## Public API after the change

No public API change. Новый assembly friendship:

```xml
<InternalsVisibleTo Include="DeepSpaceSaga.EconomyBalance" />
```

Tool использует существующие signatures:

```csharp
internal AuthoritativeSnapshot CaptureSnapshotForTests(
    long gameTimeMs = 0,
    SimulationSpeed? speed = null,
    long? simulationTimeMs = null);

internal ScenarioFile CaptureSaveStateForTests(
    long gameTimeMs,
    SimulationSpeed speed);
```

## Implementation steps

1. Добавить только named friend assembly; не делать seams public и не давать доступ Client assembly.
2. Fixture после реализации US-0012 загружает минимальную полноценную торговую экономику с hourly production, deterministic events, route state и persisted ledger.
3. Снять `t=0`, затем вызвать `CaptureSnapshotForTests` на `1..240 * GameCalendar.HourMs` при `Speed0`; доказать, что каждый economic boundary применён ровно один раз, `GameTimeMs` достиг target, а `MotionTimeMs`/Approach состояние не получают скрытого ускорения.
4. В отдельной ветке на `120 * HourMs` получить `CaptureSaveStateForTests`, serialize/parse и загрузить новым Engine с `isSave:true`; продолжить до 240 часов. Сравнить canonical economy/map/event/voyage/ledger save blocks с непрерывной веткой.
5. Проверить, что повторный capture того же target не применяет production/event/fees/ledger второй раз, а убывающий target отклоняется существующим invariant либо не меняет state согласно реализованному US-0012 contract; тест фиксирует фактический единый контракт.

## Out of scope

Новый public time API, изменение `SimulationClock`, production scheduler, trade/voyage правила, tool project, report schema, wall-clock loop, Approach или performance assertions.

## Invariants

- `BuildSnapshot` под lock вызывает `AdvanceWorldTo` до commands: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:393–421`.
- Save capture имеет explicit-time seam и общий state owner: `SimulationEngine.cs:720–734`.
- Calendar/economic time не должно менять физическую скорость/Approach: `Documentation/01-Requirements/EngineRequirements.md:5335–5343`.
- Ровно два implementation files, только engine layer и matching Engine.Tests.

## Tests

`EconomyBalanceSeamTests`:

- `Hourly_explicit_time_run_applies_each_economy_boundary_once` (AC-01).
- `Repeated_target_is_idempotent_and_does_not_advance_motion` (AC-01).
- `Ten_day_continuous_and_midpoint_save_load_states_are_canonical_equal` (AC-01/05).
- `Friend_tool_uses_existing_seams_without_public_time_api` (structural assertion через reflection: methods non-public; project XML содержит exact friend once).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter FullyQualifiedName~EconomyBalanceSeamTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Изменены только два allowed files; friend entry встречается ровно один раз.
- AC-01/05 покрыты hourly, idempotency и midpoint Save/Load named tests.
- Engine build/tests/format проходят либо конкретное baseline-падение записано отдельно.
- Public API, clocks, gameplay calculations и Approach не изменены.
- Нет незаписанных assumptions, скрытого wall-clock поведения или правок вне `Code context`.
- Результат проверяется указанными командами и canonical state comparison.

## Self-containment check

Точный assembly name, разрешённые seams, часовой corpus, midpoint и сравниваемые domains заданы. Implementer не выбирает новый time API и не ищет альтернативный способ ускорить игру.
