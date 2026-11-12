---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0012-slim-column
ticket: EP-0024-US-0012-TK-0002-slim-card
title: Узкая карточка пилота
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0012-TK-0001-slim-layout, EP-0024-US-0002-TK-0001-sprite-renderer]
files_touched: 3
serves: [US12-AC2, US12-AC3]
created: 2026-11-12T17:00:00Z
revision: 1
---

# Узкая карточка пилота

## Why

Пользователь выбрал узкую карточку «Фишка поля» (13.03): портрет, имя, квадраты брони и корпуса, оружие и пиксельный корабль, как фишка на карте.

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) §11; всё, что там не названо, остаётся как в Classic на базе. Эталон — [Materials/Reference/panels](../../Materials/Reference/panels). Утверждение — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-15, A-16, A-18. Корабль — тем же рендером спрайтов, что на поле (US-0002), с множителем `max(1, f/2)`.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | :159-180 `DrawCards`; :295-305 `Portrait`; :214-293 метка, шкалы, строка фазы | В режиме — карточка по UiSpec §11.5: плита `ui.pilot-card`, портрет (4, 4) 32×32 с тёмной рамкой, имя, квадраты брони и корпуса с золотой рамкой потери, оружие, корабль; метка, шкалы и строка фазы не рисуются; рамка выбора Gold 2 на 2,2 снаружи; сбитый — palette `disabled` и затемнение; `Rects["pilot:<id>"]` — как сейчас. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalShipSprites.cs` | US-0002-TK-0001 | Перегрузка с явным множителем пикселя (уже есть параметр `factor`). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopSlimCardTests.cs` | — | Новый файл: элементы и цвета карточки (пробы §11.13.1, §11.13.3), сбитый, рамка потери после залпа, клик выбирает пилота. |

## Public API after the change

—

## Implementation steps

1. Ветка отрисовки узкой карточки.
2. Корабль и квадраты.
3. Выбор, потеря, сбитый.
4. Тесты.

## Out of scope

Панель команд (US-0013).

## Invariants

Без метрики карточка прежняя.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopSlimCardTests`
- `PilotCardColumnTests`
- `PilotPortraitCatalogTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopSlimCardTests|FullyQualifiedName~PilotCardColumnTests|FullyQualifiedName~PilotPortraitCatalogTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us12-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US12-AC2, US12-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Получившийся кадр сверен на глаз с `Materials/Reference/panels` (тот же кадр и вырезка ×2), расхождения записаны в «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Все размеры — UiSpec §11.5; спрайты — §2.

## Исполнение

Не начато.
