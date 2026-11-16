---
epic: EP-0030-debug-console
story: EP-0030-US-0001-global-console
ticket: EP-0030-US-0001-TK-0001-hotkey-edge
title: Опрос клавиши P для сочетания Ctrl+Alt+P
stage: approved
approval_kind: autonomous-execution
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
prerequisite_stories: []
files_touched: 2
serves: [AC-02]
created: 2026-11-16T06:50:00Z
revision: 1
---

# TK-0001 — Фронт клавиши P с модификаторами

## Why

Сочетание Ctrl+Alt+P (D-02 [эпика](../../Documentation.md)) не может сработать: `Key.P` отсутствует в опрашиваемом списке клавиш. Тикет даёт единственный фронт нажатия P вместе с текущими модификаторами. Покрывает AC-02 [истории](../EP-0030-US-0001-global-console.md).

## Decisions

- Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».
- Пользователь, 2026-11-16, о декомпозиции историй: «Декомпозировать сам».
- Предложение A-01 эпика принято исполнителем как техническое допущение автономного исполнения 2026-11-16 (это не решение пользователя).

## Assumptions

- Физическая клавиша P опрашивается через Silk `Key.P`, поэтому RU/EN раскладка не влияет на распознавание.
- Левые и правые Ctrl/Alt уже сворачиваются в `KeyboardEdgeTracker.Modifiers`; отдельной логики сторон не требуется.
- Добавление P в StandardKeys не меняет поведение существующих экранов: экраны, не обрабатывающие P, игнорируют `OnKeyDown(Key.P)`.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs` | `StandardKeys` (:9-18) не содержит `Key.P` | Добавить `Key.P` в конец массива с комментарием «debug console Ctrl+Alt+P» |
| `tests/DeepSpaceSaga.Client.Tests/KeyboardEdgeTrackerTests.cs` | Существующие тесты фронтов (`F10_is_reported_on_press_edge`) и helper `Poll` | Добавить три теста ниже по образцу существующих |

## Public API after the change

No API change. Изменяется только набор опрашиваемых клавиш.

## Implementation steps

1. Добавить в тесты `Ctrl_Alt_P_reports_single_P_edge_with_modifiers` (Theory: ControlLeft/ControlRight × AltLeft/AltRight), `Held_P_reports_no_second_edge`, `Bare_P_reports_edge_without_modifiers`.
2. Добавить `Key.P` в `StandardKeys` с комментарием.
3. Собрать Client и Client.Tests, scoped `dotnet format whitespace`, `git diff --check`.

## Out of scope

Обработка сочетания в `SkiaWindow` (TK-0003), консольный контроллер (TK-0002), изменение repeat-таймингов и модификаторов.

## Invariants

- Удержание клавиши не даёт второго фронта (существующий контракт трекера, `F10_is_reported_on_press_edge`).
- Набор остальных опрашиваемых клавиш не меняется.

## Tests

Тесты `KeyboardEdgeTrackerTests`: `Ctrl_Alt_P_reports_single_P_edge_with_modifiers`, `Held_P_reports_no_second_edge`, `Bare_P_reports_edge_without_modifiers`.

По инструкции оператора тесты пишутся в тикете, но запускаются после последнего тикета истории (US-0001 TK-0003). В тикете: build и format.

```powershell
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs tests/DeepSpaceSaga.Client.Tests/KeyboardEdgeTrackerTests.cs
# конец истории:
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter FullyQualifiedName~KeyboardEdgeTrackerTests --results-directory $run --logger trx
```

`$run` — `D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/<run>/`; process-tree cap 300000 ms.

## Definition of Done

- `Key.P` опрашивается; три названных теста написаны и компилируются, проходят в прогоне конца истории.
- Build/format без ошибок; изменены только два файла таблицы.
- Execution evidence записан в тикете, commit + push выполнены.

## Self-containment check

Файл, строка, изменение и тесты указаны; внешних решений не требуется.

## Execution evidence 2026-11-16

Изменённые файлы:

- `src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs` — `Key.P` добавлен в конец `StandardKeys` с комментарием «EP-0030 debug console Ctrl+Alt+P».
- `tests/DeepSpaceSaga.Client.Tests/KeyboardEdgeTrackerTests.cs` — тесты `Ctrl_Alt_P_reports_single_P_edge_with_modifiers` (Theory: ControlLeft/ControlRight × AltLeft/AltRight), `Held_P_reports_no_second_edge`, `Bare_P_reports_edge_without_modifiers`; добавлен `using DeepSpaceSaga.Client.UI.Screens` для `KeyModifiers`.

Команды и результаты:

- `dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — Build succeeded, 0 warnings, 0 errors (собирает и `DeepSpaceSaga.Client`).
- `dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs tests/DeepSpaceSaga.Client.Tests/KeyboardEdgeTrackerTests.cs` — exit 0.
- `git diff --check` — exit 0.
- Тесты: NOT RUN — по инструкции оператора выполняются в конце истории (после US-0001 TK-0003).
