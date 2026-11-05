---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0014-balance-and-acceptance"
ticket: "EP-0012-US-0014-TK-0001-production-balance-harness"
title: "production-balance-harness"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0018-TK-0005-module-combat-regressions"]
files_touched: 3
serves: ["US14-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0014-TK-0001-production-balance-harness

## Why

[Приёмка и баланс](../EP-0012-US-0014-balance-and-acceptance.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: production-balance-harness; доказать US14-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (3 файла). Baseline6eab4dbe9672d82018e794fa3bf8951be1f7a2e8. Пререквизит: полный Engine2331/2331 и Client2221/2221, solution0 warnings/errors. Harness использует только публичные clock/snapshot/ReceiveCommand API настоящего Engine, production registry и CombatMukha. Геометрия damage/fuse/AI/RNG не дублируется. Повторно используемый input-policy source не зависит от xUnit и затем подключается analyzer в следующем тикете.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs` | Current100-seed integration smoke | Add fixed production policy batch protocol; retain independent integration smoke. |
| `tests/DeepSpaceSaga.Engine.Tests/ProductionCombatBalanceHarness.cs` | NEW | Pure public-command input policy, actual production Engine runs, raw commands/outcomes and corridor report. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0014TK0001Tests.cs` | NEW | Production source/operator/public-command and deterministic policy regression checks. |

Execution decisions: immutable policy v1 before any corridor measurement;100ms public observation cadence,500ms physical reaction, arc aim admitted from two equal one-second observed turns with straight fallback; counter interception from public observed flight vector at own range frontier; dodge via maneuver.evade, static retains fire/PRO with no evade. Seeds3001..4000 each policy, all production operators20 checked rather than replaced. Each run fixed600000ms physical horizon; real duration physical×4 at x1; unfinished runs count outside acceptance. No retuning of catalog/scenario/AI or policy to force corridor success; G-03 remains explicit.

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. `tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs`, `ModuleCombatEndToEndTests.cs`. LegacyWeaponRegistry уже удалён пререквизитом; никакие калиброванные fixtures для баланса не используются.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Проверки протокола выполнены; результаты балансной приёмки приведены отдельно и остаются OPEN.

- **Production_balance_1000_seeds_reports_corridors**: seeds3001..4000, dodge72–88%, static<=10%, P90<=5.5real min.
- **Production_bot_uses_public_commands_and_production_registry**: no LegacyWeaponRegistry для balance; raw seeds/outcomes сохраняются.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0014TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0014TK0001Tests"
# Full production balance:40 explicitly discovered policy/seed batches, each50 seeds. Exact commands in evidence JSON; do not run all batches in one uncapped process.
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs tests/DeepSpaceSaga.Engine.Tests/ProductionCombatBalanceHarness.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0014TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US14-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-21: implementation и self-review выполнены. PASS 43 named / 2195 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0014-tk0001-results.json).

Protocol PASS3 focused +40 production batch cases, all2000 battles and37593 actual commands recorded;100-seed smoke PASS. BALANCE ACCEPTANCE OPEN: dodge96.6%/P907.493333min outside72–88%/5.5min; static5.9%/P902.296505min within limits. No gameplay/catalog/scenario/policy retuning; author decision required. Source-verified incremental Engine coverage2374 cases retains138 unchanged long cases from ticket65, not a claim of rerunning them. Raw JSONL gzip and complete summary published in evidence.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

2026-10-20 execution prerequisite correction: legacy removal follows Save18, forecast and Client migrations; see [current DAG](../../Tickets.md#migration-prerequisite-correction). AC/deletion requirements remain unchanged.

Acceptance: **US14-AC1 OPEN / OUTSIDE CORRIDOR**. Protocol implementation and all2000 real battles complete; dodge96.6% wins/P907.493333real min exceed72–88%/5.5min. Static5.9%/P902.296505min pass. [Measured report and limitations](../../evidence/balance.md), [complete summary](../../evidence/us0014-tk0001-balance-summary.json), [raw2000 outcomes and37593 commands](../../evidence/us0014-tk0001-production-balance.jsonl.gz). No gameplay, catalog, scenario or policy retuning. Report is implemented; balance acceptance requires the author. Analyzer/native/documentation continue independently.
