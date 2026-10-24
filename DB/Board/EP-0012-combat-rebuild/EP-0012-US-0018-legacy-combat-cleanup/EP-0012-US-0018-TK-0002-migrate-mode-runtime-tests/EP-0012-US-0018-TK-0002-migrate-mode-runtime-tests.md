---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0018-legacy-combat-cleanup"
ticket: "EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests"
title: "migrate-mode-runtime-tests"
stage: "implemented"
layer: "contracts-engine"
depends_on: ["EP-0012-US-0019-TK-0004-client-combat-regressions"]
files_touched: 9
serves: ["US18-AC1", "US18-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests

## Why

[Удаление старой боевой модели и регрессии](../EP-0012-US-0018-legacy-combat-cleanup.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: migrate-mode-runtime-tests; доказать US18-AC1, US18-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (9 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs](../../../../../src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs) | Существует: src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs:9; internal sealed class GameDataRegistry | Только изменения для scope этой карточки и served AC. |
| [tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs](../../../../../tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs:7; public sealed class FireModeRuntimeTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs](../../../../../tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs:7; public sealed class SetFireModeTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Contracts/CombatSnapshot.cs` | LauncherCombatSnapshot still defaults to standard even on point launchers | Empty no-mode default; preserve the transitional legacy field until runtime retirement. No current point launcher claims standard mode. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Point bootstrap inherits standard by omission | Keep standard explicit only in the existing historical legacy bootstrap pending runtime deletion; actual point bootstrap uses no mode. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Retired combat commands still enter legacy handlers in a point session | Reject the six retired IDs at the point-session dispatch boundary with unknown_command_type; preserve known-command ownership/receipt semantics. |

| `tests/DeepSpaceSaga.Client.Tests/TorpedoCommandPanelTests.cs` | Mode-selector case consumes the removed implicit standard default | Replace only retired mode cycling with current local Q/profile selection, no mode/reload/RNG/optimistic launch; other retained transport/panel assertions stay until their runtime-consumer migration. |

| `tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs` | One old DTO test requires implicit standard and serializes mode/breakdown commands | Replace its retired mode assertions with point profiles, no-mode default and point command payload while preserving fractional reload and active-projectile ID roundtrip. |

## Public API after the change

LauncherCombatSnapshot.ModeId now defaults to empty/no-mode; the transitional legacy bootstrap passes standard explicitly until deletion. Current point sessions reject all six retired command IDs with unknown_command_type before their old handlers. Signatures and Save18 physical profile/reload continuation remain unchanged.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Заменить старые mode/setMode tests на отрицательные проверки legacy commands и новые профили; обновить runtime/registry defaults без сохранения режима standard.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Production boundary: Contracts + Engine for the existing DTO default, registry guard and dispatch. Matching Contracts/Engine tests and the directly affected Client consumer tests are explicitly scoped above. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Legacy_commands_are_rejected_and_profiles_are_captured**: никакого mode bonus/breakdown RNG в новой модели.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0018TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0018TK0002Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US18-AC1, US18-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-21: implementation и self-review выполнены. PASS 11 named / 1284 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0018-tk0002-results.json).

Mode/default migration from the active canonical source catalog: captured profiles/speed/radius/fuse/fixed reload and save roundtrip; six retired IDs reject before legacy handlers without clock/scatter effects; rejected identity stays durable across save/load and payload change. No implicit standard on point launchers; mixed point modules/fireModes rejected even with omitted rules. PASS19mode cases,151direct Client consumers,1284Engine regression,2365Client non-budget,256Contracts,259Motion, solution/owning format checks. Remaining legacy runtime, matching automatic-defense/homing tests, full Engine corpus, native and1000-seed balance remain OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

2026-10-21 source audit: actual point bootstrap omitted ModeId and inherited the DTO standard default. Make no-mode the default and keep standard explicit only in the still-retained legacy bootstrap until its deletion. Reject a fireModes catalog when any point module is present even if a test omits combatRules. Replace all six retired mode assertions with captured profiles/fuse/reload, no breakdown, paused rejection/idempotency and unchanged active-flight/reload checks; actual canonical source registry, no historical catalog. Further directly/transitively affected consumer fixtures will be scoped before migration. This is not authorization to weaken retained invariants or treat old characterization as current point acceptance.

Consumer staging clarification: this ticket migrates the directly affected mode/default consumers before deletion. Matching runtime-deletion tickets also own their obsolete automatic-defense/homing tests and point replacements; US18-TK5 remains the complete integration gate. No global Engine/Client legacy-retirement claim is made from the mode subset.
