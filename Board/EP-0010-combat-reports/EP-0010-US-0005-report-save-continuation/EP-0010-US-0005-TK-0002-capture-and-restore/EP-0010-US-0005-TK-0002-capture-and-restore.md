---
epic: EP-0010-combat-reports
story: EP-0010-US-0005-report-save-continuation
ticket: EP-0010-US-0005-TK-0002-capture-and-restore
title: "Capture/restore и bootstrap baseline"
stage: draft
status: planned
layer: engine
depends_on: ["EP-0010-US-0005-TK-0001-save-schema-validation"]
files_touched: 4
serves: ["US05-AC2"]
created: 2026-10-16
revision: 1
validation_status: not-run
---

# Capture/restore и bootstrap baseline

## Why

Продолжить идентичность докладов при чистом UI после загрузки. [История](../EP-0010-US-0005-report-save-continuation.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R17, R20, R21.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Выполнение этого тикета, commit/push, генерация art или запуск игры пока не поручены. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Proposed API ниже — ожидаемый результат, не утверждение о текущем коде. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `engine`; matching coverage `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | src/DeepSpaceSaga.Engine/SimulationEngine.cs:17 — `public sealed partial class SimulationEngine` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportPersistence.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Engine.Tests/CombatReportsSaveLoadTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0005-TK-0001-save-schema-validation; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. CaptureSaveState сохраняет Sequence и весь набор range IDs; UI карточки/outbox/scroll в save не писать.
2. До применения loaded world подготовить report state; после успешного restore зафиксировать baseline и пометить current in-range как исходное состояние без E01. Stage не создаёт событий/RNG и учитывает loaded availability.
3. Проверить out-of-range уже доложенную цель, уничтоженную цель, pre-feature Save17 и первый advance с новым событием; не менять существующее правило Defeated save.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `ReportedOutsideRangeNeverRepeatsAfterLoad` (планируемый сценарий проверки).
- `LoadedInRangeIsSilentBaseline` (планируемый сценарий проверки).
- `FirstPostLoadNewEventIsDelivered` (планируемый сценарий проверки).
- `FailedRestorePreservesCurrentSession` (планируемый сценарий проверки).
- `ReportGenerationDoesNotPermitDefeatedSave` (планируемый сценарий проверки).

Команды выполнять из корня репозитория **при реализации**, сейчас NOT RUN:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportsSaveLoadTests"
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportPersistence.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs tests/DeepSpaceSaga.Engine.Tests/CombatReportsSaveLoadTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 4 разрешённых файлах, выбранный layer соблюдён.
- Критерии US05-AC2 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Продолжить идентичность докладов при чистом UI после загрузки. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.
