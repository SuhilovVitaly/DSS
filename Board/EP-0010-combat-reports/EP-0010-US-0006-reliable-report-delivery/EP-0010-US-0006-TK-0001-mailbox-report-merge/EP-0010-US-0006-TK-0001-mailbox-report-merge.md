---
epic: EP-0010-combat-reports
story: EP-0010-US-0006-reliable-report-delivery
ticket: EP-0010-US-0006-TK-0001-mailbox-report-merge
title: "Слияние докладов в local adapter"
stage: implemented
status: implemented
layer: local-client
depends_on: ["EP-0010-US-0005-TK-0002-capture-and-restore"]
files_touched: 2
serves: ["US06-AC1"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# Слияние докладов в local adapter

## Why

Не потерять report deltas, когда world snapshots coalesce. [История](../EP-0010-US-0006-reliable-report-delivery.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R07, R16, R17, R19, R20.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `local-client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine.LocalClient/SnapshotMailbox.cs` | src/DeepSpaceSaga.Engine.LocalClient/SnapshotMailbox.cs:14 — `internal void Publish` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Client.Tests/CombatReportMailboxTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0005-TK-0002-capture-and-restore; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

SnapshotMailbox internal only; public IGameSessionConnection remains unchanged.

## Implementation steps

1. Расширить SnapshotMailbox.Publish слиянием CombatReports по Sequence; stable order, максимум LatestSequence, одинаковый baseline в одной сессии.
2. Повтор одного sequence с разным payload и несогласованный baseline считать ошибкой; >4096 pending явно fault. Не менять объединение CommandResults/ShipEvents/DialogueEvents.
3. Named integration coverage находится в Client.Tests по существующему примеру SessionReliabilityTests; не создавать новый test project.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `CoalescingPreservesAllReportDeltas` (планируемый сценарий проверки).
- `RepeatedReportsDeduplicateInMailbox` (планируемый сценарий проверки).
- `ConflictingPayloadAndOverflowFailExplicitly` (планируемый сценарий проверки).
- `OtherEventStreamsStillMerge` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportMailboxTests"
dotnet build src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine.LocalClient/SnapshotMailbox.cs tests/DeepSpaceSaga.Client.Tests/CombatReportMailboxTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 2 разрешённых файлах, выбранный layer соблюдён.
- Критерии US06-AC1 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Не потерять report deltas, когда world snapshots coalesce. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence - 2026-10-16

Mailbox merges pending report deltas by sequence, orders them, preserves fixed baseline/max latest, rejects conflicting payload/baseline/overflow. Legacy null keeps pending reports. Four mailbox cases plus eight SessionReliability regressions PASS (12 total); Client/LocalClient compile and scoped format/diff PASS. Other event streams unchanged.
