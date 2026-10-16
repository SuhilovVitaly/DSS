---
epic: EP-0010-combat-reports
story: EP-0010-US-0010-tactical-screen-integration
ticket: EP-0010-US-0010-TK-0003-session-reset-and-defeat
title: "Чистая смена сессии и приоритет Defeat"
stage: implemented
status: implemented
layer: client
depends_on: ["EP-0010-US-0010-TK-0002-screen-render-and-input"]
files_touched: 4
serves: ["US10-AC3"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# Чистая смена сессии и приоритет Defeat

## Why

Закрыть first-snapshot и session replacement races. [История](../EP-0010-US-0010-tactical-screen-integration.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R01, R02, R03, R06, R13, R20, R21, R22.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:1348 — `_objectInfoPanel.Render` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/GameSessionHandle.cs` | src/DeepSpaceSaga.Client/GameSessionHandle.cs:295 — `ReadSnapshotsAsync` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Client.Tests/CombatReportLifecycleTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0010-TK-0002-screen-render-and-input; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. При реальной замене session store/inbox/resources принадлежат новой сессии; initial baseline не гасит реальные новые факты первого snapshot. Modal возврат к тому же screen не очищает историю.
2. Не очищать действующий store до успешной загрузки; dispose старого handle отменяет delivery, новые callbacks не применяют прежние данные. Dispose графических ресурсов по текущему screen lifecycle.
3. При Outcome.Defeated сохранён существующий auto-transition; new report не вызывает advance или паузу и не задерживает переход.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `FailedLoadKeepsExistingHistory` (планируемый сценарий проверки).
- `SuccessfulLoadDoesNotReplayHistory` (планируемый сценарий проверки).
- `FirstNewSessionSnapshotKeepsNewReports` (планируемый сценарий проверки).
- `OldConnectionCannotPolluteReplacement` (планируемый сценарий проверки).
- `ModalResumeKeepsHistory` (планируемый сценарий проверки).
- `DefeatTransitionIsImmediate` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportLifecycleTests"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/GameSessionHandle.cs tests/DeepSpaceSaga.Client.Tests/CombatReportLifecycleTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 4 разрешённых файлах, выбранный layer соблюдён.
- Критерии US10-AC3 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Закрыть first-snapshot и session replacement races. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence - 2026-10-16

Handle closing synchronously releases report paints/bitmaps/history; receive loop checks cancellation/disposal before applying a late snapshot. Modal deactivation clears focus but retains history/resources. Defeat deactivation releases resources without delaying existing transition. Six lifecycle cases plus53 report cases and8 session reliability regressions PASS (67). Client compile/scoped format/diff PASS. Failed-load UI policy retains current screen until successful replacement; actual loader failures also covered by existing reliability tests and later acceptance.
