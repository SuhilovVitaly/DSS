---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0013-command-panel
ticket: EP-0024-US-0013-TK-0003-panel-fire
title: Стрельба в панели
stage: done
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0013-TK-0001-panel-frame]
files_touched: 5
serves: [US13-AC3]
created: 2026-11-12T17:00:00Z
revision: 1
---

# Стрельба в панели

## Why

В стрельбе панель содержит оружие, цели с силуэтом Тага и шансом, броню цели, угрозу, ловушку, уклонение, отказ от огня и подсказку (эталон `1-fire`).

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) §11; всё, что там не названо, остаётся как в Classic на базе. Эталон — [Materials/Reference/panels](../../Materials/Reference/panels). Утверждение — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-19. Силуэт цели — спрайт её модели стороны `enemy` курсом 180°.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardFireBlock.cs` | :10-12 строки по 20, 3 видимых; :39-42 `Height`; :55-166 отрисовка; :76-117 состояния и кнопки | В режиме — раскладка UiSpec §11.8 в ширине 138: строка оружия в две строки, кнопки целей 138×17 с силуэтом, броня цели, угроза, черта, «Ловушка [D]» с зарядами, «Уклон [E]» и «Не стрелять», подсказка; состояния уклонения, пыли и «нет целей» — §11.8; те же действия `target:<id>`, `decoy`, `evade`, `hold`; прокрутка списка при > 3 целях прежняя. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Fire.cs` | тексты стрельбы | Без изменений; ключи прежние. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | `TacticalBattle.*` | `Panel.NoTargets`, `Panel.UnderFireShort`, `Panel.EvadeShort` (UiSpec §11.12). |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | `TacticalBattle.*` | То же. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardFireBlockTests.cs` | прежние проверки | Режим: высота 129 для Юна на ходе 4 и 106 без целей; `card-command:*` внутри панели (проба §11.13.2); выбор цели кликом. |

## Public API after the change

`internal static float FireHeight(...)` (режим).

## Implementation steps

1. Раскладка и силуэты целей.
2. Состояния и кнопки.
3. Строки.
4. Тесты.

## Out of scope

Другие фазы.

## Invariants

В Classic блок прежний.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `PilotCardFireBlockTests`
- `PilotCardCommandsInputTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotCardFireBlockTests|FullyQualifiedName~PilotCardCommandsInputTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us13-tk3" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardFireBlock.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US13-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Получившийся кадр сверен на глаз с `Materials/Reference/panels` (тот же кадр и вырезка ×2), расхождения записаны в «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Раскладка — UiSpec §11.8, строки — §11.12.

## Исполнение

Не начато.

## Уточнение scope до реализации

Добавлены PilotCardColumn.cs (выбор вычисленной FireHeight) и PilotCardFireBlock.Slim.cs (изолированная компактная раскладка с единым путём измерения/отрисовки). Исходный Classic путь сохранён. Высота без целей не меньше 106 по явной пробе UiSpec; длинные тексты увеличивают её.


### Результат us13-tk3

Стрельба: единый путь измерения/отрисовки, цели со спрайтами, броня/угрозы, ловушка/уклон/отказ, перенос текста. 14/14: Юн 129, Вера без целей 106, dust/evade, target click, прокрутка >3 целей и Classic. Визуально проверены fire-panel-2-fire-column-x2.png и fire-panel-2-no-target-column-x2.png; геометрия соответствует Reference/panels/1-fire, выбран Таг-5 после проверочного клика (эталон Таг-6); шрифт/портреты A-16. Самопроверка source/input пройдена.

Проверки: 14/14 PASS; сборка Client/tests выполнена тестовой командой. Scoped whitespace и diff check PASS. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/us13-tk3/client.trx`, `us13-tk3-test.log`, `us13-tk3-format.log` в том же run. Самопроверка diff выполнена; это не независимое review. Native/product gates отдельно в ExecutionStatus.
