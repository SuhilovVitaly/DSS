---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0005-move-templates
ticket: EP-0024-US-0005-TK-0001-templates
title: Ленты-шаблоны движения
stage: done
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0003-TK-0001-plates]
files_touched: 5
serves: [US5-AC1, US5-AC2]
created: 2026-11-12T13:00:00Z
revision: 1
---

# Ленты-шаблоны движения

## Why

Движение в новом виде — ленты, а не тонкие линии (D-01).

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в Classic на базе. Материалы — [Materials](../../Materials/README.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

—

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Templates.cs` | — | Новый файл по образцу прототипа: `DrawTemplates`, `Brackets` (UiSpec §5). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Movement.cs` | :8-41 | В режиме — `DrawTemplates` вместо прежней отрисовки; выноска пыли `(end.X, end.Y + 34)`. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | `TacticalBattle.*` | `TacticalBattle.Map.TemplateTip` (UiSpec §9). |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | `TacticalBattle.*` | То же. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopMoveTests.cs` | — | Новый файл: пробы §10.1, перетаскивание конца, отсутствие сектора. |

## Public API after the change

—

## Implementation steps

1. Перенести отрисовку лент, кружков, зоны, «призраков», скобок и подсказки.
2. Подключить в режиме; отключить прежний сектор. Значки носителей за краем сохранить по UiSpec §5.8 и A-11 (исправлена противоречащая им фраза шага).
3. Строка подсказки в локализации.
4. Тесты на `1-move-no-hover` и `1-anomaly-move-dust`; сценарий перетаскивания из `MovementOrdersTests` в режиме.

## Out of scope

Камера (US-0008). Значки носителей за краем (`Edge`) не меняются (A-11).

## Invariants

В Classic движение прежнее.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopMoveTests`
- `MovementOrdersTests`
- `LocalizationCoverageTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopMoveTests|FullyQualifiedName~MovementOrdersTests|FullyQualifiedName~LocalizationCoverageTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us5-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Templates.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Movement.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US5-AC1, US5-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Геометрия, цвета и тексты — UiSpec §5, §9.

## Исполнение

Не начато.


### Результат us5-tk1

Ленты, зона досягаемости, скорость, призраки, скобки, RU/EN-подсказка и выноска пыли реализованы. Проверены две сцены, геометрия handles, отсутствие зависимости рендера движения от оружейного сектора и реальное редактирование приказа перетаскиванием. Значки Edge сохранены по A-11. Build: 0 ошибок/предупреждений.

Проверки: 15/15 PASS; сборка Client/tests выполнена тестовой командой. Scoped whitespace и diff check PASS. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/us5-tk1/client.trx`, `us5-tk1-test.log`, `us5-tk1-format.log` в том же run. Самопроверка diff выполнена; это не независимое review. Native/product gates отдельно в ExecutionStatus.
