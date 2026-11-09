---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0001-reference-and-golden-harness
ticket: EP-0019-US-0001-TK-0001-fix-sdk-build-ambiguity
title: Сборка на SDK 8.0.4xx
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: []
files_touched: 2
serves: [US1-AC1]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Сборка на SDK 8.0.4xx

## Why

Свежий `main` не собирается на SDK 8.0.4xx: компилятор C# 12 не выбирает перегрузку `SKShader.CreateLinearGradient` для коллекционного выражения с `null`. Без сборки нельзя выполнить ни один тикет.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Перегрузка с `SKColor[]` и `float[]?` — та, что выбиралась на новом SDK.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | :1732 `CreateLinearGradient(from, to, [c1, c2], null, Clamp)` | Заменить на `new SKColor[] { c1, c2 }`. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapDepthRenderer.cs` | :420 то же для пламени двигателя | Заменить на `new SKColor[] { ... }`. |

## Public API after the change

No API change.

## Implementation steps

1. Найти все вызовы `CreateLinearGradient`/`CreateRadialGradient` с коллекционным выражением цветов и `null` (`git grep -n "Gradient(" src tests tools`).
2. Заменить коллекционные выражения цветов на явные `new SKColor[] { ... }`.
3. Собрать решение на установленном SDK; при наличии — также на SDK 8.0.4xx.

## Out of scope

Обновление SkiaSharp и `global.json`.

## Invariants

Цвета, позиции и режим градиентов прежние.

## Tests (исходный план; актуальные проверки ниже)

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- Проверка содержания и ссылок.

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapDepthRenderer.cs --verify-no-changes
git diff --check
dotnet build DeepSpaceSaga.sln --no-restore
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US1-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Правка локальна; проверка — сборка.

## Исполнение

2026-11-08: заменены ровно два коллекционных выражения на явные SKColor[]. Цвета, позиции, tile mode сохранены; других Gradient-вызовов в src/tests/tools нет. Self-review diff выполнен (не независимое review).

Проверки Windows / SDK 10.0.400: Client build PASS (0 warnings/errors), solution build PASS (3 исходных MSB9008 о отсутствующем архивном ModApi), scoped whitespace format PASS, git diff --check PASS. Логи: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-setup/{baseline-build,tk1-solution-build,tk1-format}.log. SDK 8.0.4xx NOT RUN: не установлен; baseline на SDK 10 собирался и до правки. Подтверждена совместимая форма перегрузки, а не воспроизведение CS0121 на SDK 8. Native не применим к выбору перегрузки. Публикация проверяется в ExecutionStatus.md после коммита.

## Фактическая привязка проверок

Client/solution build; scoped format. Плановые имена тестов заменены реализацией тех же AC в указанных owning classes; результаты и paths — ExecutionStatus.md. Source scope разнесён по partial-компонентам overlay/map/phase для сохранения session ownership; никакой Engine-side реализации не добавлялось.
