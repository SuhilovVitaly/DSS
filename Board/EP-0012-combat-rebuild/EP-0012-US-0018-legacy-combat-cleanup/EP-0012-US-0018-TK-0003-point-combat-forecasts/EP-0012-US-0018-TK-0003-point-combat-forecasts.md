---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0018-legacy-combat-cleanup"
ticket: "EP-0012-US-0018-TK-0003-point-combat-forecasts"
title: "point-combat-forecasts"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0008-TK-0003-encounter-stats"]
files_touched: 5
serves: ["US18-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 5
---

# EP-0012-US-0018-TK-0003-point-combat-forecasts

## Why

[Удаление старой боевой модели и регрессии](../EP-0012-US-0018-legacy-combat-cleanup.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: point-combat-forecasts; доказать US18-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (5 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.DefensePreview.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.DefensePreview.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.DefensePreview.cs:6; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:9; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.ManualDefense.cs` | Existing | Legacy centre-contact helper rejects point payloads instead of inferring private deadlines. |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Existing | Point launcher module projection has no legacy fixed lifetime. |

A-FORECAST-ENGINE-01: shared ModelVersion3 and actual CombatPath parity were repaired/verified separately in published US18-TK4 fix. This owning Engine integration removes point consumers from legacy lifetime/chance/contact projections; own forecasts use captured endpoint/deadline and foreign forecasts only observed ray/horizon. Legacy-only branches remain for the inactive legacy catalog until its explicit cleanup ticket; no claim that source legacy symbols are already deleted.

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Перевести Engine combat preview/forecast на линейные point-projectiles; не оставлять ссылок на removed DTO/route/ratings. Own known endpoint допустим; enemy hidden endpoint не используется.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Forecast_privacy_and_linear_projectile_parity**: forecast own explosion соответствует runtime, enemy forecast не раскрывает detonation.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0018TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0018TK0003Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.DefensePreview.cs src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US18-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 4 named / 709 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0018-tk0003-results.json).

Reproduced3 failed /1 passed: point launchers incorrectly published legacy120000ms lifetime. Fixed projection to omit it; point defenses have no legacy chance preview, and legacy centre-contact helper refuses point payloads. All3 own profiles use captured deadlines and match actual Engine hit times/line positions. Two worlds differing only in hidden enemy endpoint produce identical observed-only forecasts with no contact ETA, target or guidance legs; no launch RNG consumed. Separate shared-runtime parity fix ebf66d65 remains linked, not claimed as new source deletion. Legacy-only helpers stay until explicit cleanup. Solution build0 warnings/errors; production data/Save18/Client/native integration remains OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

2026-10-20 execution prerequisite correction: legacy removal follows Save18, forecast and Client migrations; see [current DAG](../../Tickets.md#migration-prerequisite-correction). AC/deletion requirements remain unchanged.

2026-10-20 consumer prerequisite correction: point consumers precede production data activation; actual integration gates remain OPEN until activation/Save18. See [DAG](../../Tickets.md#consumer-prerequisite-correction).
