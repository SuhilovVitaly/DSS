---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0011-steel-panels
ticket: EP-0024-US-0011-TK-0003-role-colours
title: Цвета полос и имени в правых окнах из ролей
stage: done
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0011-TK-0001-steel-roles]
files_touched: 5
serves: [US11-AC3]
created: 2026-11-12T17:00:00Z
revision: 1
---

# Цвета полос и имени в правых окнах из ролей

## Why

На эталоне полосы корпуса и брони — зелёная и синяя, имя пилота — светлое; сейчас они зашиты как Cyan и Purple и роли скина на них не влияют.

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) §11; всё, что там не названо, остаётся как в Classic на базе. Эталон — [Materials/Reference/panels](../../Materials/Reference/panels). Утверждение — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Запасные цвета — прежние Cyan и Purple, поэтому Classic не меняется.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs` | :145 `Bar` (роль `ui.bar`) | Необязательный параметр роли: `Bar(..., role = "ui.bar")`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BuildCard.cs` | :38 имя Cyan; :41, :43 полосы Cyan / Purple; :68 цвет ячейки брони Purple | Имя — `ui.build.name` (`text`, запасной Cyan); полосы — `ui.bar.hull` / `ui.bar.armor` (запасные Cyan / Purple); полоса ячейки брони — `ui.build.slot.armor` (запасной Purple). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs` | :60 имя; :69, :71 полосы | Свой юнит: имя — `ui.build.name`; полосы — `ui.bar.hull` / `ui.bar.armor`. Имя противника — прежний Orange. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Salvo.cs` | :102 полоса брони Purple и полоса корпуса рядом | `ui.bar.armor` / `ui.bar.hull`. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopPanelFrameTests.cs` | TK-0002 | Проверка цвета полос в «Пилот и сборка» в `tabletop` (`#5C9440`, `#5A7CA8` ±12) и прежних в Classic. |

## Public API after the change

`BattleDesign.Bar(..., string role = "ui.bar")`.

## Implementation steps

1. Параметр роли у `Bar`.
2. Перевести вызовы на роли.
3. Тест цветов.

## Out of scope

Геометрия окон.

## Invariants

В Classic, Contrast и Xenon Star цвета прежние.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopPanelFrameTests`
- `BuildCardRenderTests`
- `HoverCardTests`
- `PhaseSalvoTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopPanelFrameTests|FullyQualifiedName~BuildCardRenderTests|FullyQualifiedName~HoverCardTests|FullyQualifiedName~PhaseSalvoTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us11-tk3" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BuildCard.cs --verify-no-changes
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs --verify-no-changes
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Salvo.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US11-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Получившийся кадр сверен на глаз с `Materials/Reference/panels` (тот же кадр и вырезка ×2), расхождения записаны в «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Роли и цвета — UiSpec §11.2, §11.10.

## Исполнение

Не начато.


### Результат us11-tk3

Полосы корпуса/брони, имя своего пилота и броневой слот читают роли. Имя противника остаётся Orange. Реальный русский кадр steel-frames-windows-x2.png сверен с эталоном: зелёная/синяя полосы и светлое имя подтверждены пикселями; портрет штатный, геометрия правой карточки прежняя. В PhasePanel.Salvo на базе есть только полоса брони; новая полоса корпуса не добавлялась. Build: 0 ошибок/предупреждений.

Проверки: 21/21 PASS; сборка Client/tests выполнена тестовой командой. Scoped whitespace и diff check PASS. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/us11-tk3/client.trx`, `us11-tk3-test.log`, `us11-tk3-format.log` в том же run. Самопроверка diff выполнена; это не независимое review. Native/product gates отдельно в ExecutionStatus.
