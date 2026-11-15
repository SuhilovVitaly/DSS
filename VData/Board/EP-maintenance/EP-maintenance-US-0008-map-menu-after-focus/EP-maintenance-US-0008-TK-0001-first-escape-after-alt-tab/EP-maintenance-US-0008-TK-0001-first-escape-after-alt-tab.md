---
epic: EP-maintenance
story: EP-maintenance-US-0008-map-menu-after-focus
ticket: EP-maintenance-US-0008-TK-0001-first-escape-after-alt-tab
title: Открывать меню первым нажатием Esc после Alt+Tab
stage: approved
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: []
files_touched: 2
serves: [AC1, AC2, AC3]
created: "2026-11-10T08:48:36Z"
revision: 1
---

# Открывать меню первым нажатием Esc после Alt+Tab

## Why

После переключения в другое окно и возврата на карту солнечной системы игрок иногда нажимает Esc дважды или больше, прежде чем откроется меню. В коде есть подтверждённый путь потери запроса. `OpenGameMenuAsync` отбрасывает запрос в первые 1500 мс после возврата фокуса. Причастность этого пути к конкретному пользовательскому случаю предстоит проверить.

## Decisions

- **D-01.** Исправить пропуск первого нажатия Esc после возврата на карту. Исходное сообщение приведено в [истории](../EP-maintenance-US-0008-map-menu-after-focus.md#входное-техническое-задание).

## Assumptions

- **A-01.** Карта активна и готова принимать ввод. Сценарий относится к свежему нажатию после возврата фокуса. Панели, которые штатно обрабатывают Esc раньше карты, сохраняют приоритет.

## Code context

| Файл | Текущее состояние | Разрешённое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | Строки 79–80 и 551–584 хранят время возврата фокуса и синхронизируют состояние клавиш. Строки 686–749 проводят новый фронт через экран. Строки 1166–1184 подавляют открытие меню в течение 1500 мс, в том числе для принятого фронта Esc. | Разрешить свежему Esc открыть меню сразу после фокуса. Сохранить защиту от удерживаемой клавиши, текущий порядок переходов и единственный экземпляр меню. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs` | `Fixture` создаёт `SkiaWindow` без нативного окна и проводит нажатие через `HandleKeyboardEdge` (строки 276–336). | Добавить проверки маршрута Esc после возврата фокуса и штатного открытия/закрытия меню с восстановлением скорости. |

`GameSessionScreen.OnKeyDown` уже возвращает `ScreenEvent.OpenGameMenu` на Esc (строки 677–678). Текущая проверка `KeyboardEdgeTrackerTests.Resync_on_focus_regain_suppresses_phantom_escape_edge` покрывает удерживаемый Esc. Оба файла служат справкой и остаются вне списка разрешённых изменений.

## Public API after the change

Публичный API сохраняется.

## Implementation steps

1. Воспроизвести прохождение нового Esc до `OpenGameMenuAsync` при только что восстановленном фокусе. При нативной проверке сопоставить `FocusChanged`, `PollKeyboard` и `OpenGameMenu kept hidden` по существующим сообщениям журнала.
2. Устранить общий временной запрет для свежего Esc после возврата фокуса. Сохранить синхронизацию состояния клавиш при фокусе и сброс удержаний при уходе из игры. Проверить сценарий возврата из Windows Snipping Tool, ради которого добавлялся временной запрет.
3. Провести регрессионные проверки через маршрут окна до `GameMenuScreen`. Убедиться, что возврат фокуса сам по себе не открывает меню, а открытие и закрытие продолжают использовать штатный модальный переход.

## Out of scope

Правила ввода внутри меню и других модальных экранов, камера, новые настройки и правила симуляции.

## Invariants

- Опрос клавиатуры прекращается, пока окно не в фокусе (`SkiaWindow.cs:686–689`). При возврате фокуса синхронизируется состояние уже нажатых клавиш (`SkiaWindow.cs:567–570)).
- Открытие проходит через `PushModalAsync`, а повторный запрос при активном меню не добавляет ещё одно меню (`SkiaWindow.cs:1166–1184)).

## Tests

Добавить в `BattleNavigationTests` тесты `FirstEscapeOpensGameMenuImmediatelyAfterFocusRegain` и `EscapeMenuStillPausesAndRestoresMapSpeed`. В первом провести свежий Esc через обработчик окна в пределах прежнего интервала 1500 мс, с контролируемым состоянием фокуса и без ожидания. Проверить одно открытие и отсутствие меню до Esc. Во втором проверить паузу при открытии и восстановление прежней скорости после закрытия. Запустить также существующий тест синхронизации удерживаемого Esc.

Из корня DSS, после создания каталога результатов, выполнить команды с ограничением 300000 мс на дерево каждого процесса.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter 'FullyQualifiedName~FirstEscapeOpensGameMenuImmediatelyAfterFocusRegain|FullyQualifiedName~EscapeMenuStillPausesAndRestoresMapSpeed|FullyQualifiedName~Resync_on_focus_regain_suppresses_phantom_escape_edge' --results-directory 'D:/DeepSpaceSaga/artifacts/validation/map-menu-after-focus/<run>'
git diff --check
```

Нативная проверка на Windows. На карте солнечной системы повторить Alt+Tab в другое окно и обратно, затем сразу нажать Esc один раз без щелчка мышью. Повторить несколько раз и записать число успешных попыток. Отдельно закрыть Snipping Tool и проверить возврат без самопроизвольного меню. Для запуска использовать меньший монитор по правилам `AGENTS.md`.

## Definition of Done

- AC1–AC3 выполнены в пределах разрешённых файлов. Сборка Client и именованные проверки проходят.
- Записаны команды, результаты автоматических проверок и фактический результат сценария с физическим Alt+Tab.

## Self-containment check

Указаны точка блокировки, маршрут Esc, тестовая обвязка окна и шаги нативной проверки. Зависимостей на другие тикеты нет.

## Статус

Код реализован 2026-11-10. До исправления первый Esc был отброшен при msSinceFocusRegain=0. После удаления временного запрета сборка Client и git diff --check прошли. BattleNavigationTests и KeyboardEdgeTrackerTests — 35/35. Автоматические проверки подтверждают первое свежее нажатие, отсутствие открытия при возврате фокуса или удержании Esc, единственное меню, паузу и восстановление скорости. Физический Alt+Tab и возврат из Windows Snipping Tool остаются на приёмке. Журналы в D:/DeepSpaceSaga/artifacts/validation/map-menu-after-focus/20261110-before/ и D:/DeepSpaceSaga/artifacts/validation/map-menu-after-focus/20261110-after/.
