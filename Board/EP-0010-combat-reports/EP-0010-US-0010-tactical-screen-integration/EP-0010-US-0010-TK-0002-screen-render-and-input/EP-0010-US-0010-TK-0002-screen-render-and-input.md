---
epic: EP-0010-combat-reports
story: EP-0010-US-0010-tactical-screen-integration
ticket: EP-0010-US-0010-TK-0002-screen-render-and-input
title: "Подключение докладов к тактическому экрану"
stage: draft
status: implemented
layer: client
depends_on: ["EP-0010-US-0010-TK-0001-right-column-layout"]
files_touched: 5
serves: ["US10-AC2"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# Подключение докладов к тактическому экрану

## Why

Соединить inbox/store/preparation/render и ввод на экране. [История](../EP-0010-US-0010-tactical-screen-integration.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R01, R02, R03, R06, R13, R20, R21, R22.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Выполнение этого тикета, commit/push, генерация art или запуск игры пока не поручены. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Proposed API ниже — ожидаемый результат, не утверждение о текущем коде. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:1348 — `_objectInfoPanel.Render` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanel.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Client.Tests/CombatReportScreenFlowTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:292` — OnRender; `:351` — screen.Render | Перед screen.Render вызвать PrepareCombatReports на том же UI thread; остальной render loop не менять |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0010-TK-0001-right-column-layout; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

Proposed internal GameSessionScreen.PrepareCombatReports() вызывается из SkiaWindow.OnRender перед screen.Render. Публичный IScreen не меняется.

## Implementation steps

1. Добавить partial screen с store/panel/assets; drain inbox и Prepare assets в новом internal PrepareCombatReports до screen.Render. В SkiaWindow.OnRender для каждого рисуемого GameSessionScreen вызвать этот метод перед записью/рисованием screen.Render на UI thread; из самого Render/Draw ресурсов не загружать, к Engine не обращаться.
2. Render читает готовые модели и правую колонку; пустой store показывает R03. Порядок pointer/wheel/key handlers отдаёт panel первый отказ перед map/commands по его bounds.
3. Проверить auto-expand batches, manual browse, independent map selection; ввод panel не вызывает SendCommand/SetSpeed и не меняет camera. Новые данные не захватывают focus.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `ScreenConsumesPendingReportsBeforePresentation` (планируемый сценарий проверки).
- `PanelInputDoesNotSendCommandsOrZoom` (планируемый сценарий проверки).
- `MapSelectionDoesNotFilterReports` (планируемый сценарий проверки).
- `BatchExpandsLatest` (планируемый сценарий проверки).
- `DrawUsesOnlyPreparedAssets` (планируемый сценарий проверки).

Команды выполнять из корня репозитория **при реализации**, сейчас NOT RUN:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportScreenFlowTests"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanel.cs tests/DeepSpaceSaga.Client.Tests/CombatReportScreenFlowTests.cs src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 5 разрешённых файлах, выбранный layer соблюдён.
- Критерии US10-AC2 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Соединить inbox/store/preparation/render и ввод на экране. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Execution evidence - 2026-10-16

SkiaWindow prepares receive/store/resources on UI thread before every drawn GameSessionScreen.Render. Render only lays out against actual current ObjectInfo bottom and draws prepared assets; no drain/decode there. Panel has first pointer/wheel/key refusal and blocks map hit testing. Five integrated screen cases plus48 prior cases PASS (53), including probe connection proving no command/speed change and unchanged camera/selection. Client compile/scoped format/diff PASS.
