---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0010-tactical-map"
ticket: "EP-0012-US-0010-TK-0001-combat-visuals-v3"
title: "combat-visuals-v3"
stage: "implemented"
layer: "client"
depends_on: ["EP-0012-US-0019-TK-0003-client-forecast-cache-migration"]
files_touched: 4
serves: ["US10-AC4"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0010-TK-0001-combat-visuals-v3

## Why

[Тактическая карта](../EP-0012-US-0010-tactical-map.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: combat-visuals-v3; доказать US10-AC4 в границах этой карточки.

## Decisions / Assumptions

A-VISUALS: extend the existing schema1 palette with a strict pointCombat group of presentation colors, stroke/dash/marker sizes and real effect durations. Preserve legacy schema1 fallback only when the whole new group is absent; malformed/missing/unknown fields inside a provided group must report file and key. Defaults match shipped JSON. Physical blast/core sizes come from projectile/gameplay contracts, never from visual config. Update existing exact root-key coverage test to assert the new group rather than deleting its completeness check. New rendering consumers belong to following tickets.


D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs](../../../../src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs) | Существует: src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs:8; public sealed record CombatVisualSettings( | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json](../../../../src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json) | Существует: src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json:1; { | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| [tests/DeepSpaceSaga.Client.Tests/CombatPaletteContentTests.cs](../../../../tests/DeepSpaceSaga.Client.Tests/CombatPaletteContentTests.cs) | Existing exact palette property-count gate | Include pointCombat group and preserve all semantic color assertions. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. `CombatVisualSettings.cs`, `combat-visuals.json`.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Combat_visuals_are_data_driven**: все новые стили читаются из JSON, ошибки validation диагностированы.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0010TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0010TK0001Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US10-AC4 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 10 named / 2148 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0010-tk0001-results.json).

Point combat palette group exposes24 presentation-only colors/styles/durations, loaded once at startup, strict missing/invalid/duplicate/unknown group diagnostics with path/key. Shipped and deployed JSON match defaults; editing every group property changes loaded values without mutating defaults. Schema1 whole-group absence keeps legacy palette compatibility. Existing exact root completeness test extended, all original color assertions retained. Physical blast/core radius and physical fuse remain projectile contracts, not visual settings. Rendering consumers are next tickets. Client nonbudget2148/2148; strict legacy ForecastBudget remains OPEN; headless only.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

2026-10-20 consumer prerequisite correction: point consumers precede production data activation; actual integration gates remain OPEN until activation/Save18. See [DAG](../../Tickets.md#consumer-prerequisite-correction).
