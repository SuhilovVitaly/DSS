---
epic: EP-0010-combat-reports
story: EP-0010-US-0010-tactical-screen-integration
ticket: EP-0010-US-0010-TK-0001-right-column-layout
title: "Общая геометрия Object Info и докладов"
stage: implemented
status: implemented
layer: client
depends_on: ["EP-0010-US-0009-TK-0002-accordion-input"]
files_touched: 5
serves: ["US10-AC1"]
created: 2026-10-16
revision: 1
validation_status: automated-passed
---

# Общая геометрия Object Info и докладов

## Why

Резервировать место докладам под фактическим Object Info. [История](../EP-0010-US-0010-tactical-screen-integration.md), [ТЗ](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R01, R02, R03, R06, R13, R20, R21, R22.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionRightColumnLayout.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs` | src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs:359 — `public void Render` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:1348 — `_objectInfoPanel.Render` | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Client.Tests/CombatReportRightColumnTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0009-TK-0002-accordion-input; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. Добавить явные Bounds/Bottom в результат ObjectInfoPanel layout и согласованный width; не дублировать вычисление ширины в reports.
2. Ввести расчёт правой колонки: ограничить Object Info с его существующей внутренней прокруткой, обеспечить минимум reports title/header, соблюдать нижний toolbar и journal.
3. Интегрировать вычисление layout без включения вымышленных данных; доказать no-overlap при 1280x720/1920x1080 UI 100/120/150, open/collapsed. Подготовить hook вызова render reports для следующего тикета.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `ReportsFollowActualObjectInfoBottom` (планируемый сценарий проверки).
- `CollapsedObjectInfoReleasesSpace` (планируемый сценарий проверки).
- `MinimumPanelHeightAndNoHudOverlap` (планируемый сценарий проверки).
- `ViewportAndScaleMatrix` (планируемый сценарий проверки).
- `EmptyPanelDoesNotCoverJournal` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportRightColumnTests"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionRightColumnLayout.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs tests/DeepSpaceSaga.Client.Tests/CombatReportRightColumnTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 4 разрешённых файлах, выбранный layer соблюдён.
- Критерии US10-AC1 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Резервировать место докладам под фактическим Object Info. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Scope adjustment before implementation - 2026-10-16

Add Controls/CombatJournalPanel.cs to allowlist (five actual files total): accept maximumRight from shared right-column layout so the technical journal cannot cover reports at high UI scales. Existing journal content/event behavior stays unchanged.

## Execution evidence - 2026-10-16

Actual ObjectInfo Bounds and shared ResolveWidth drive report placement. Right column reserves80 logical pixels for report caption/card header above footer, caps existing scrollable ObjectInfo. Journal accepts maximumRight to remain beside column. Ten geometry cases (six viewport/scale plus expanded/collapsed, actual bottom and journal) plus38 prior report cases PASS (48). Client compile/scoped format/diff PASS. Render hook exposes CombatReportsBounds for next ticket.
