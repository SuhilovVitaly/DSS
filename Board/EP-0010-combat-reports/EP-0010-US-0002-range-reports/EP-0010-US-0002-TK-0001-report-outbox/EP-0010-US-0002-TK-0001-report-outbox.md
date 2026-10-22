---
epic: EP-0010-combat-reports
story: EP-0010-US-0002-range-reports
ticket: EP-0010-US-0002-TK-0001-report-outbox
title: "Сессия докладов и авторитетный outbox"
stage: implemented
status: implemented
layer: engine
depends_on: ["EP-0010-US-0001-TK-0002-snapshot-batch"]
files_touched: 3
serves: ["US02-AC1"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# Сессия докладов и авторитетный outbox

## Why

Создать единственную точку выпуска и публикации докладов. [История](../EP-0010-US-0002-range-reports.md), [ТЗ](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R08, R16, R18, R20, R21.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `engine`; matching coverage `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | src/DeepSpaceSaga.Engine/SimulationEngine.cs:17 — `public sealed partial class SimulationEngine` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Engine.Tests/CombatReportOutboxTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0001-TK-0002-snapshot-batch; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

Internal proposed helpers: EmitCombatReport(...), CaptureCombatReportBatch(), EvaluateCombatReportRange(double motionTimeMs), ObserveCombatReportFact(CombatJournalEntry entry). Existing Engine public API remains.

## Implementation steps

1. Добавить partial с sequence, baseline, pending outbox и набором ReportedRangeTargetIds; без statics и без нового RNG stream.
2. Интегрировать reset/bootstrap, BuildSnapshot и физические подшаги AdvanceMotionTo из AdvanceWorldTo, включая его fast path без активного боя; пока подготовить no-op hooks наблюдения журнала/range, реализуемые следующими тикетами.
3. Публиковать новые данные в CombatReports, очищая outbox только после успешного создания snapshot; переполнение 4096 явно завершает сессию ошибкой, а не теряет события.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `OutboxSequenceAndBootstrapBaseline` (планируемый сценарий проверки).
- `SnapshotPublishesOutboxOnce` (планируемый сценарий проверки).
- `ReportOutboxOverflowIsExplicit` (планируемый сценарий проверки).
- `ReportsDoNotAdvanceRngOrClocks` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportOutboxTests"
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs tests/DeepSpaceSaga.Engine.Tests/CombatReportOutboxTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 3 разрешённых файлах, выбранный layer соблюдён.
- Критерии US02-AC1 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Создать единственную точку выпуска и публикации докладов. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence - 2026-10-16

Implemented session sequence, fixed baseline, bounded outbox, reset and physical-step hooks. Range/fact hooks intentionally await following tickets. Four focused tests PASS; Engine build and scoped format PASS; git diff --check PASS. Full Engine suite running separately; native not applicable to this infrastructure ticket. Overflow raises an explicit session exception without removing queued reports.
