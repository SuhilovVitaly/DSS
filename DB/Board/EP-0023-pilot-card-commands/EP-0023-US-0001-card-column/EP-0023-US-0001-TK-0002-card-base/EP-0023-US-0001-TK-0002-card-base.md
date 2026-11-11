---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0001-card-column
ticket: EP-0023-US-0001-TK-0002-card-base
title: Базовая карточка: портрет, плашка, вертикальные шкалы и иконки
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0023-US-0001-TK-0001-column-layout]
files_touched: 4
serves: [US1-AC1]
created: 2026-11-11T14:30:00Z
revision: 1
---

# Базовая карточка: портрет, плашка, вертикальные шкалы и иконки

## Why

Пользователь выбрал вид v18 с вертикальными шкалами и иконками вместо подписей и горизонтальных полос (D-01).

## Decisions

Применяются [D-01–D-04](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в EP-0019, EP-0020 и EP-0022. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-01, A-03. Иконки — векторные, одного семейства (UiSpec §2.3), без новых файлов ресурсов. Значения шкал — текущее значение без максимума; максимум виден в карточке «Пилот и сборка».

## Code context

Строки — `main` @ `8b0bf6ce`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | :14-66 отрисовка карточки (портрет 24, подписи «Броня/Корпус/Сознание», горизонтальные полосы, строка фазы) | Отрисовка базовой части по UiSpec §2; подписи показателей не рисуются; маркер ловушки справа от плашки. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardIcons.cs` | — | Новый файл: `StatIcon(SKCanvas, string kind, SKPoint center, SKColor color)` — щит, силуэт корпуса, глаз (UiSpec §2.3). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs` | :8-19 `PilotStat`, `PilotCard`; `:108-114` значения `"80/80"` | Без изменения смысла; при необходимости добавить `PilotStat.Current` (int) для значения над шкалой. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardColumnTests.cs` | пробы | Пробы UiSpec §10.1. |

## Public API after the change

`internal static void StatIcon(SKCanvas c, string kind, SKPoint center, SKColor color)`; `PilotStat.Current` (если добавлен).

## Implementation steps

1. Нарисовать базовую часть по UiSpec §2.1–§2.2 с теми же ролями скина (`ui.pilot-card`, `ui.pilot-card.selected`, `.art`, `ui.pilot-card.chip`, `ui.pilot-card.bar.<kind>`).
2. Добавить иконки показателей (UiSpec §2.3).
3. Подгонка плашки статуса по ширине (UiSpec §2.1) и обрезка имени многоточием.
4. Записать в `Layout.Texts` текст строки фазы, значения шкал и плашку (как сейчас — для тестов текстов).

## Out of scope

Новые роли скина (US-0006), блоки раскрытия.

## Invariants

`PilotCardModel.Project` остаётся источником всех текстов и цветов значений (в т. ч. красное сознание ниже минимума).

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `PilotCardColumnTests`
- `GlyphCoverageTests`
- `OverlayBoundaryTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotCardColumnTests|FullyQualifiedName~GlyphCoverageTests|FullyQualifiedName~OverlayBoundaryTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us1-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardIcons.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US1-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Все размеры, цвета и координаты — UiSpec §2; данные — существующий `PilotCard`.

## Исполнение

Не начато.
