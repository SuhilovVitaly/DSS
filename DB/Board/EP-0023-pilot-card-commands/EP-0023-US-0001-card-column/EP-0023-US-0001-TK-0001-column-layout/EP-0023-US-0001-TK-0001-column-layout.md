---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0001-card-column
ticket: EP-0023-US-0001-TK-0001-column-layout
title: Раскладка колонки переменной высоты и прокрутка
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
files_touched: 7
serves: [US1-AC2, US1-AC3]
created: 2026-11-11T14:30:00Z
revision: 1
---

# Раскладка колонки переменной высоты и прокрутка

## Why

Сейчас все карточки фиксированной высоты, и шаг 87 зашит в трёх местах; раскрытая карточка выбранного дрона требует одной раскладки на кадр, которой пользуются отрисовка, попадания и зона подписей карты.

## Decisions

Применяются [D-01–D-04](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в EP-0019, EP-0020 и EP-0022. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-01, A-02. Высота блока раскрытия запрашивается у построителей блоков (US-0002…US-0004) через одну функцию; до их появления она равна 0, и раскладка совпадает с текущей.

## Code context

Строки — `main` @ `8b0bf6ce`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | :10-13 `Bounds`, `Rect(int)` с шагом 87, `Hit` | Новый тип раскладки `PilotColumnLayout` (прямоугольники карточек, смещение прокрутки, видимая область `6…714`); `Rect(int)` и `Bounds` читают последнюю раскладку. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs` | :21-28 `OverlayRects` строит `new(6, 6 + 87 * i, 196, 88 + 87 * i)` | Брать видимые прямоугольники из раскладки колонки. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs` | :98 зона подписей `new(0, 0, 202, 6 + 87 * count)` | Нижняя граница — низ видимой части колонки. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | :313-317 `OnMouseWheel` → `ScrollTargets`, `ScrollQueue`, `ScrollResults` | Колесо над колонкой прокручивает её, если колонка не помещается (раньше остальных обработчиков, кроме прокрутки целей карточки из US-0003). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardColumnTests.cs` | пробы колонки и выбор кликом | Ожидания раскладки по UiSpec §4. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/OverlayBoundaryTests.cs` | :11-26 восемь карточек, `Bounds.Bottom = 697` | Сохранить 697 для восьми нераскрытых; добавить случай с раскрытой карточкой и прокруткой. |

| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotColumnLayout.cs` | — | Выделить расчёт раскладки и отсечение в отдельный тип для независимых тестов. |

## Public API after the change

`internal sealed class PilotColumnLayout { IReadOnlyList<SKRect> Cards; float Scroll; SKRect Viewport; SKRect Visible(int index); }`; `PilotCardColumn.Layout` (последний кадр); `PilotCardColumn.Scroll(SKPoint point, float delta) : bool`; `PilotCardColumn.ExpandedHeight(PilotCard card, TacticalBattleFrame frame) : float` (пока 0).

## Implementation steps

1. Ввести `PilotColumnLayout` и построение раскладки в начале `Draw` по UiSpec §3–§4; рисовать карточки с отсечением по `Viewport` и смещением `Scroll`.
2. Перевести `Bounds`, `Rect`, `Hit`, `OverlayRects` и зону подписей карты на раскладку.
3. Добавить прокрутку колонки колесом и автопрокрутку к выбранной карточке при смене выбора; индикатор прокрутки по UiSpec §4.
4. Обновить `PilotCardColumnTests` и `OverlayBoundaryTests`; добавить тест прокрутки с восемью карточками и раскрытой карточкой высотой 240.

## Out of scope

Отрисовка содержимого карточки (TK-0002), команды (TK-0003, US-0002…US-0004).

## Invariants

Без раскрытия (высота блока 0) прямоугольники карточек совпадают с текущими (`6 + 87 × i`). Безопасная зона камеры не меняется.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `PilotCardColumnTests`
- `OverlayBoundaryTests`
- `PilotColumnScrollTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotCardColumnTests|FullyQualifiedName~OverlayBoundaryTests|FullyQualifiedName~PilotColumnScrollTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us1-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotColumnLayout.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US1-AC2, US1-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Карточка самодостаточна: геометрия — UiSpec §3–§4, точки изменения перечислены в Code context, функция высоты блока возвращает 0 до US-0002…US-0004.

## Исполнение

Не начато.
