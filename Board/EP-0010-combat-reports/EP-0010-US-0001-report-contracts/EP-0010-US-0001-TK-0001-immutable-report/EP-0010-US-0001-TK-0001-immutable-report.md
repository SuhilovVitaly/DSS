---
epic: EP-0010-combat-reports
story: EP-0010-US-0001-report-contracts
ticket: EP-0010-US-0001-TK-0001-immutable-report
title: "Неизменяемые данные доклада"
stage: draft
status: implemented
layer: contracts
depends_on: []
files_touched: 2
serves: ["US01-AC1"]
created: 2026-10-16
revision: 1
validation_status: passed
---

# Неизменяемые данные доклада

## Why

Определить общий неизменяемый факт без зависимости от существования объектов. [История](../EP-0010-US-0001-report-contracts.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R11, R12, R16, R17.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Выполнение этого тикета, commit/push, генерация art или запуск игры пока не поручены. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Proposed API ниже — ожидаемый результат, не утверждение о текущем коде. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `contracts`; matching coverage `tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Contracts.Tests/BattleReportSnapshotTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: none; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

Proposed: BattleReportSnapshot, BattleReportReporterSnapshot, BattleReportKind, BattleReportVariant, CombatReportBatchSnapshot. Sequence: long > 0; MotionTimeMs: finite double >= 0; EpisodeId: string. Batch Reports: ImmutableArray<BattleReportSnapshot>.

## Implementation steps

1. Создать enum семи видов, variant и reporter role, record докладчика и доклада; nullable данные не заменять выдуманными значениями.
2. Определить CombatReportBatchSnapshot(BaselineSequence, LatestSequence, Reports) и лимит pending=4096; все участники/имена фиксируются данными, не ссылками на runtime.
3. Для E07 передавать before/after только разрешённого модуля; локализованные тексты и SK-типы исключить.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `JsonRoundTrip_AllKindsAndVariants` (планируемый сценарий проверки).
- `CapturedIdentitySurvivesMissingObjects` (планируемый сценарий проверки).
- `NullableReporterAndDamageArePreserved` (планируемый сценарий проверки).

Команды выполнять из корня репозитория **при реализации**, сейчас NOT RUN:

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~BattleReportSnapshotTests"
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs tests/DeepSpaceSaga.Contracts.Tests/BattleReportSnapshotTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 2 разрешённых файлах, выбранный layer соблюдён.
- Критерии US01-AC1 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Определить общий неизменяемый факт без зависимости от существования объектов. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence — 2026-10-16

Текущий запрос разрешает execution/commit/push по EpicExecutionPrompt.md; прежние planning-only ограничения исторические. Реализованы immutable records/enums, captured identity, nullable privacy fields, module before/after и batch с лимитом4096. DTO не содержит runtime/graphics dependencies. Проверены все7 kinds и оба E06 variants, дробное время, long.MaxValue sequence, nullable службы/портреты и независимость captured данных от live world.

Validation: `dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --nologo --verbosity minimal -p:UseSharedCompilation=false --filter FullyQualifiedName~BattleReportSnapshotTests` —13/13 PASS; тот же project без filter —207/207 PASS. `dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore --nologo -p:UseSharedCompilation=false` —0 warnings/errors. `dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs tests/DeepSpaceSaga.Contracts.Tests/BattleReportSnapshotTests.cs --verbosity quiet` и `git diff --check` —PASS. Выполнен self-review относительно US01-AC1; это не независимая приёмка. Native/FPS не относятся к DTO и не заявляются пройденными.
