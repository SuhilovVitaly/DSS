---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0002-overlay-layout-shell
ticket: EP-0019-US-0002-TK-0001-design-tokens-and-primitives
title: Токены и примитивы макета
stage: draft
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0001-TK-0003-scene-harness]
files_touched: 3
serves: [US2-AC1, US2-AC2]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Токены и примитивы макета

## Why

Все панели и карта используют одни и те же цвета, шрифты и примитивы. Их нужно реализовать один раз, буквально по DesignSpec §2–3.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-03, A-04. Роли скинов: каждый примитив принимает имя роли и использует значение пакета, если оно есть, иначе токен DesignSpec.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs` | нет | Новый класс: `S`, `Wd`, токены цветов DesignSpec §2, `T`, `Measure`, `FitSize`, `PanelBox`, `Tag`, `BoxText`, `Button`, `BigButton`, `IconButton`, `Slider`, `Bar`, `Line`, `Callout`, `Arrow`, фигуры `Diamond`, `Hex`, `Triangle`; форматирование `F`, `Deg`, `Pct`, `Plural`, `Genitive`, `CursorGlyph`. Источник — DesignSpec §2–3. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinRenderScope.cs` | :61-162 Color/Number/Text | Использовать для подмены токенов ролями; новые роли `ui.*` (список в DesignSpec §2) добавить в карту ролей. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleDesignTests.cs` | нет | Тесты значений и рисунка примитивов. |

## Public API after the change

`internal static class BattleDesign` и `internal readonly record struct BattleViewport(float S, float Wd)`.

## Implementation steps

1. Реализовать токены и примитивы DesignSpec §2–3 без изменения чисел; шрифт — `BattleTypography`.
2. Добавить `BattleViewport.From(width, height)`.
3. Тесты: цвета токенов; `FitSize`; `Pct(0.555)="56%"`, `Deg(-24.26)="−24,3°"`, `Plural(3, ...)`; растровая проба `PanelBox(selected)` и `BigButton` в 3 точках даёт цвета DesignSpec §2–3.

## Out of scope

Применение примитивов на экране.

## Invariants

Токены — единственный источник цветов нового интерфейса; в коде экрана нет литералов цветов.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `BattleDesignTests.Tokens_match_design_spec`
- `BattleDesignTests.Number_formatting_matches_design_spec`
- `BattleDesignTests.Primitives_draw_reference_pixels`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~BattleDesignTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us2-tk1" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinRenderScope.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleDesignTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC1, US2-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Все значения находятся в DesignSpec §2–3.

## Исполнение

Не начато.
