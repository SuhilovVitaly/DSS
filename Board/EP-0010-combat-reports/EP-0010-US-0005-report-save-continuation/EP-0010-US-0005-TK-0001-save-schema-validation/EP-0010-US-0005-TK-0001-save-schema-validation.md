---
epic: EP-0010-combat-reports
story: EP-0010-US-0005-report-save-continuation
ticket: EP-0010-US-0005-TK-0001-save-schema-validation
title: "Схема и атомарная валидация состояния докладов"
stage: implemented
status: implemented
layer: engine
depends_on: ["EP-0010-US-0004-TK-0002-contact-summary"]
files_touched: 5
serves: ["US05-AC1"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# Схема и атомарная валидация состояния докладов

## Why

Сохранить durable отметки без обязательной миграции текущих Save17. [История](../EP-0010-US-0005-report-save-continuation.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R17, R20, R21.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `engine`; matching coverage `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Scenario/CombatReportsSaveData.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs` | src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:66 — `public sealed record GameStateData` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Engine/Scenario/CombatReportsSaveValidation.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs` | src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:92 — `internal static ScenarioFile ValidateAndNormalize` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Engine.Tests/CombatReportsSaveSchemaTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0004-TK-0002-contact-summary; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

GameStateData(..., CombatReportsSaveData? CombatReportsState = null); Version=1. No root save version change.

## Implementation steps

1. Добавить optional CombatReportsState в конец GameStateData и record Version1(Sequence, ReportedRangeTargetIds); root Save17 не менять.
2. Вызвать отдельный validator до атомарной замены мира: версия, неотрицательная sequence, корректные уникальные нормализованные IDs; удалённые enemy IDs допустимы и не обязаны ссылаться на живой объект.
3. Отсутствие блока допустимо для pre-feature Save17 и NewGame; присутствующий malformed/null-required block отклонить ScenarioException. Root<=16 остаётся запрещён.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `NewReportStateRoundTrips` (планируемый сценарий проверки).
- `PreFeatureSave17HasExplicitBaseline` (планируемый сценарий проверки).
- `MalformedReportStateIsRejectedAtomically` (планируемый сценарий проверки).
- `DestroyedTargetIdsRemainValid` (планируемый сценарий проверки).
- `Save16StillRejected` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportsSaveSchemaTests"
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Scenario/CombatReportsSaveData.cs src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs src/DeepSpaceSaga.Engine/Scenario/CombatReportsSaveValidation.cs src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs tests/DeepSpaceSaga.Engine.Tests/CombatReportsSaveSchemaTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 5 разрешённых файлах, выбранный layer соблюдён.
- Критерии US05-AC1 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Сохранить durable отметки без обязательной миграции текущих Save17. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence - 2026-10-16

Optional Version1 report continuation added at end of GameStateData. Validator rejects invalid version/sequence/null IDs, duplicates including case aliases, blank/control/untrimmed IDs; removed targets accepted without world references. Required JSON properties enforced. Thirteen schema tests PASS, including atomic rejection and pre-feature Save17/Save16 boundaries; Engine compile and scoped format/diff PASS.
