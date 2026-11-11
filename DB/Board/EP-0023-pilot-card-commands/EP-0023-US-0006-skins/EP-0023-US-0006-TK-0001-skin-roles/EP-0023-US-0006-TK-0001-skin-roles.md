---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0006-skins
ticket: EP-0023-US-0006-TK-0001-skin-roles
title: Роли скина для новой карточки и значения в паках
stage: done
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0023-US-0005-TK-0002-remove-panel-controls]
files_touched: 7
serves: [US6-AC1, US6-AC3]
created: 2026-11-11T14:30:00Z
revision: 1
---

# Роли скина для новой карточки и значения в паках

## Why

Скины должны продолжать управлять видом колонки после смены карточки (D-04).

## Decisions

Применяются [D-01–D-04](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в EP-0019, EP-0020 и EP-0022. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-12. Новые роли не требуют новых полей манифеста; используются существующие свойства `fill`, `stroke`, `text`, `image`, `opacity`, `dash`.

## Code context

Строки — `main` @ `8b0bf6ce`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | роли `ui.pilot-card*` | Роли UiSpec §9 для шкал, иконок, разделителя, индикатора прокрутки. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardMoveBlock.cs` | — | Роли кнопок шага и ползунков (`ui.button`, `ui.slider.*`). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardFireBlock.cs` | — | Роли строк целей (`ui.box`, `ui.box.target.selected`), ловушки и уклонения (`ui.box.decoy*`, `ui.box.evade*`). |
| `src/DeepSpaceSaga.Client/Skins/contrast/skin.json` | роли `ui.pilot-card*` | Значения новых ролей. |
| `src/DeepSpaceSaga.Client/Skins/xenon-star/skin.json` | :33-48 роли карточки, полос, ползунков | Значения новых ролей в палитре Xenon Star. |
| `src/DeepSpaceSaga.Client/Skins/README.md` | :36-42 описание ролей | Новые роли. |
| `DB/Documentation/04-Engineering/TacticalBattleSkins.md` | :50-62 таблица поверхностей | Строки новых ролей и запасных значений. |

## Public API after the change

Без изменения API скинов.

## Implementation steps

1. Подключить роли UiSpec §9 в коде карточки и блоков.
2. Задать значения в `contrast` и `xenon-star`.
3. Описать роли в контракте и README.

## Out of scope

Сцены и пробы скинов (TK-0002).

## Invariants

Формат манифеста v1 не меняется; классический вид без пака совпадает с UiSpec.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TacticalBattleSkinCatalogTests`
- `SkinBattleIntegrationTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TacticalBattleSkin|FullyQualifiedName~SkinBattleIntegrationTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us6-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardMoveBlock.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardFireBlock.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US6-AC1, US6-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Список ролей и запасных значений — UiSpec §9.

## Исполнение

US6-AC1/AC3: дорожка, иконки, разделитель и прокрутка потребляют отдельные роли с резервами Classic. Заливка панели не переопределяет шкалы при отсутствии узких ролей. Обновлены оба манифеста v1 и руководство. Xenon Star собран с локальным исходником лицензированных изображений, без добавления изображений в Git. Build, 46/46 тестов, scoped format, diff check и самопроверка: D:/DeepSpaceSaga/artifacts/validation/ep-0023/20261111-execution-01/us6-tk1/.
