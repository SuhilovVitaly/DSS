---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0001-reference-and-golden-harness
ticket: EP-0019-US-0001-TK-0001-fix-sdk-build-ambiguity
title: Сборка на SDK 8.0.4xx
stage: draft
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

## Tests

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

Не начато.
