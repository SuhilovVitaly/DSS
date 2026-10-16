---
epic: EP-0010-combat-reports
story: EP-0010-US-0006-reliable-report-delivery
ticket: EP-0010-US-0006-TK-0002-buffer-pending-reports
title: "Потокобезопасное накопление перед UI"
stage: draft
status: planned
layer: client
depends_on: ["EP-0010-US-0006-TK-0001-mailbox-report-merge"]
files_touched: 3
serves: ["US06-AC2"]
created: 2026-10-16
revision: 1
validation_status: not-run
---

# Потокобезопасное накопление перед UI

## Why

Сохранить события, которые UI не успел обработать до замены Latest. [История](../EP-0010-US-0006-reliable-report-delivery.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R07, R16, R17, R19, R20.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Выполнение этого тикета, commit/push, генерация art или запуск игры пока не поручены. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Proposed API ниже — ожидаемый результат, не утверждение о текущем коде. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/SnapshotBuffer.cs` | src/DeepSpaceSaga.Client/SnapshotBuffer.cs:81 — `public void Update` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/CombatReportInbox.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Client.Tests/CombatReportInboxTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0006-TK-0001-mailbox-report-merge; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

Proposed internal SnapshotBuffer.DrainCombatReports(): ImmutableArray<BattleReportSnapshot>.

## Implementation steps

1. В Update на receive path сохранять Reports в inbox под lock до замены Latest; принимать Sequence>Baseline, не baseline-as-first-snapshot-watermark.
2. Добавить drain новых immutable reports по Sequence; dedup watermarks, 4096 pending, явная ошибка конфликта/overflow без тихого drop; память seen не растёт на всю сессию.
3. Inbox принадлежит одному SnapshotBuffer/сессии. UI не пишет в receive collections; null/empty от старого producer не очищает накопленное.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `TwoUpdatesBeforeDrainKeepEveryReport` (планируемый сценарий проверки).
- `FirstSnapshotNewReportsAreNotSuppressed` (планируемый сценарий проверки).
- `DuplicateAndOutOfOrderDelivery` (планируемый сценарий проверки).
- `ConcurrentUpdateAndDrain` (планируемый сценарий проверки).
- `SessionBufferIsolationAndBoundedMemory` (планируемый сценарий проверки).

Команды выполнять из корня репозитория **при реализации**, сейчас NOT RUN:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportInboxTests"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/SnapshotBuffer.cs src/DeepSpaceSaga.Client/CombatReportInbox.cs tests/DeepSpaceSaga.Client.Tests/CombatReportInboxTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 3 разрешённых файлах, выбранный layer соблюдён.
- Критерии US06-AC2 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Сохранить события, которые UI не успел обработать до замены Latest. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.
