---
epic: EP-0010-combat-reports
story: EP-0010-US-0007-report-history-and-text
ticket: EP-0010-US-0007-TK-0001-history-state
title: "Ограниченная история и раскрытая карточка"
stage: draft
status: planned
layer: client
depends_on: ["EP-0010-US-0006-TK-0002-buffer-pending-reports"]
files_touched: 2
serves: ["US07-AC1"]
created: 2026-10-16
revision: 1
validation_status: not-run
---

# Ограниченная история и раскрытая карточка

## Why

Отделить логику карточек от рендера и объектов мира. [История](../EP-0010-US-0007-report-history-and-text.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R03, R04, R05, R07, R11, R12, R13, R19, R20.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Выполнение этого тикета, commit/push, генерация art или запуск игры пока не поручены. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Proposed API ниже — ожидаемый результат, не утверждение о текущем коде. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportStore.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Client.Tests/CombatReportStoreTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0006-TK-0002-buffer-pending-reports; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

Internal CombatReportStore; presentation state is not saved.

## Implementation steps

1. Создать store: Receive(batch), Expand(sequence), Reset; последние200, newest-first, logical expanded id и keyboard selection отдельно.
2. Для пачки раскрыть последнее по sequence; ручное раскрытие закрывает предыдущее, повторный click не закрывает всё; новый факт снова открывает newest.
3. Eviction самой старой записи не оставляет dangling selection; reset очищает историю, дубли не сбрасывают scroll/focus. Никакой зависимости от SelectedObjectId.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `ExactlyOneCardIsExpanded` (планируемый сценарий проверки).
- `NewestEventReplacesManualExpansion` (планируемый сценарий проверки).
- `DuplicateDoesNotResetView` (планируемый сценарий проверки).
- `HistoryEvictsOldestAfter200` (планируемый сценарий проверки).
- `ResetAndMapSelectionAreIndependent` (планируемый сценарий проверки).

Команды выполнять из корня репозитория **при реализации**, сейчас NOT RUN:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportStoreTests"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportStore.cs tests/DeepSpaceSaga.Client.Tests/CombatReportStoreTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 2 разрешённых файлах, выбранный layer соблюдён.
- Критерии US07-AC1 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Отделить логику карточек от рендера и объектов мира. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.
