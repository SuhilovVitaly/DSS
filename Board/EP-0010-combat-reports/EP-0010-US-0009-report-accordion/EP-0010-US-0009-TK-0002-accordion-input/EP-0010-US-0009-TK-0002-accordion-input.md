---
epic: EP-0010-combat-reports
story: EP-0010-US-0009-report-accordion
ticket: EP-0010-US-0009-TK-0002-accordion-input
title: "Ввод и навигация по карточкам"
stage: draft
status: planned
layer: client
depends_on: ["EP-0010-US-0009-TK-0001-card-rendering"]
files_touched: 3
serves: ["US09-AC2"]
created: 2026-10-16
revision: 1
validation_status: not-run
---

# Ввод и навигация по карточкам

## Why

Реализовать один раскрытый доклад и локальное управление списком. [История](../EP-0010-US-0009-report-accordion.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R04, R05, R06, R07, R13, R15, R22.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Выполнение этого тикета, commit/push, генерация art или запуск игры пока не поручены. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Proposed API ниже — ожидаемый результат, не утверждение о текущем коде. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanel.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanelLayout.cs` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `tests/DeepSpaceSaga.Client.Tests/CombatReportPanelInputTests.cs` | NEW — proposed; файл отсутствует на planning baseline | Создать/дополнить тесты только этого тикета |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0009-TK-0001-card-rendering; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. Click заголовка выбирает ExpandedId, повторный click не закрывает всё; новый report раскрывает newest и делает его видимым.
2. Wheel прокручивает только панель; pointer/key events возвращают consumed. При focus панели Up/Down меняют selection, Enter раскрывает; вне focus не перехватывать игровые keys.
3. Hit tests используют представленный layout после resize/scale; новый report не меняет keyboard focus, закрытые тела не имеют активных зон.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `ClickSwitchesExactlyOneExpandedCard` (планируемый сценарий проверки).
- `NewEventScrollsToNewestWithoutFocusSteal` (планируемый сценарий проверки).
- `WheelConsumedInsidePanel` (планируемый сценарий проверки).
- `KeyboardSelectionAndEnter` (планируемый сценарий проверки).
- `ClosedCardBodyHasNoHitRegion` (планируемый сценарий проверки).

Команды выполнять из корня репозитория **при реализации**, сейчас NOT RUN:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~CombatReportPanelInputTests"
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanel.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanelLayout.cs tests/DeepSpaceSaga.Client.Tests/CombatReportPanelInputTests.cs
git diff --check
```

## Definition of Done

- Шаги выполнены только в 3 разрешённых файлах, выбранный layer соблюдён.
- Критерии US09-AC2 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Реализовать один раскрытый доклад и локальное управление списком. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.
