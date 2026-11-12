---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0002-ship-sprites
ticket: EP-0024-US-0002-TK-0002-sprite-geometry
title: Занятые прямоугольники и наведение на модели
stage: done
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0002-TK-0001-sprite-renderer]
files_touched: 3
serves: [US2-AC3]
created: 2026-11-12T13:00:00Z
revision: 1
---

# Занятые прямоугольники и наведение на модели

## Why

Модели крупнее прежних значков: подписи и наведение должны учитывать их размер.

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в Classic на базе. Материалы — [Materials](../../Materials/README.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-06.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs` | :86-102 занятые прямоугольники юнитов | В режиме tabletop — прямоугольники UiSpec §2.7. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs` | :106-113 `UpdateHover`, радиусы 12/16 | В режиме tabletop — 26/36. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/HoverCardTests.cs` | наведение на юниты | Случай режима tabletop: наведение на краю модели. |

## Public API after the change

—

## Implementation steps

1. Подставить прямоугольники и радиусы режима.
2. Тест наведения в режиме и неизменность Classic.

## Out of scope

Плашки (US-0003).

## Invariants

В Classic радиусы 12/16 и прямоугольники прежние.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `HoverCardTests`
- `TabletopSpriteTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~HoverCardTests|FullyQualifiedName~TabletopSpriteTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us2-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Размеры — UiSpec §2.7–§2.8.

## Исполнение

Не начато.


### Результат us2-tk2

Tabletop использует увеличенные области моделей и наведения; ввод проверен вне render scope, Classic сохраняет прежние радиусы.

Проверки: 16/16 PASS; сборка Client/tests выполнена тестовой командой. Scoped whitespace и diff check PASS. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/us2-tk2/client.trx`, `us2-tk2-test.log`, `us2-tk2-format.log` в том же run. Самопроверка diff выполнена; это не независимое review. Native/product gates отдельно в ExecutionStatus.
