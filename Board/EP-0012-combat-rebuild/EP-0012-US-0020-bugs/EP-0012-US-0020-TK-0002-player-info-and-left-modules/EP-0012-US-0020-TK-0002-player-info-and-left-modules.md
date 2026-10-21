---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0020-bugs"
ticket: "EP-0012-US-0020-TK-0002-player-info-and-left-modules"
title: "Вернуть информацию о корабле и перенести модули влево"
stage: "draft"
layer: "client"
depends_on: []
files_touched: 5
serves: ["US20-AC2"]
created: "2026-10-21T12:02:46.488Z"
revision: 1
---

# TK-0002 — Вернуть информацию о корабле и перенести модули влево

[История](../EP-0012-US-0020-bugs.md) · [Контракт эпика](../../Documentation.md)

## Why

На скриншоте 2 раздел Player Ship заменён сеткой модулей и счётчиком уничтоженных модулей. Покрывается US20-AC2.

## Decisions / Assumptions

D20-02: прямой запрос пользователя 2026-10-21; описание и скриншоты сохранены в истории.

Справа Player Ship возвращается к прежней корабельной карточке: изображение и доступные корабельные сведения ObjectInfoPanelData/BuildLines (имя, скорость, направление и остальные ранее применимые поля). Вся информация о модулях, включая броню/структуру, состояние, предупреждения и сводки, переносится в соответствующие левые модульные панели. Ничего из прежних модульных сведений не теряется. Selected Object и Combat reports сохраняют работу.

A20: точные визуальные параметры и внутренние helpers — технические предложения, не одобренные пользователем числа. Причину дефекта проверить при реализации; скриншот не доказывает диагноз.

## Code context

Пути относительно DSS; evidence относится к baseline 89e084c6. Разрешены только эти файлы; всё вне allowlist запрещено. Board не входит в allowlist реализации.

| File | Current state / evidence | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs) | 155: OwnModuleDiagram; 170: BuildLines; 449: DrawRowBody | Изменить для US20-AC2 в пределах описанного результата |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs) | CommandsPanel — модульные панели | Изменить для US20-AC2 в пределах описанного результата |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ModuleDiagram.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ModuleDiagram.cs) | 8: ModuleDiagram | Изменить для US20-AC2 в пределах описанного результата |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs) | 1435–1441: OwnModuleDiagram | Изменить для US20-AC2 в пределах описанного результата |
| tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0002Tests.cs (NEW) | NEW — предлагаемый файл | Именованные проверки ниже |

## Public API after the change

No public API change. Contracts/Engine/Motion/save schema сохраняются. Внутренние Client helpers допускаются в allowlist; новые публичные поля не требуются.

## Implementation steps

1. Проверить симптом и актуальность перечисленных точек интеграции.
2. Реализовать описанный результат в allowlist, используя display-admitted state.
3. Добавить named cases ниже в matching Client test file, проверить соседние регрессии.
4. Зафиксировать automated evidence и отдельно native/product acceptance.

## Out of scope / Invariants

Только US20-AC2. Не менять AI, наведение, RNG, урон, баланс, Engine-команды и сохранения. Сохранить паузу и общий scene clock. Соседние тикеты не исполняются неявно.

## Tests

- Player_info_restores_ship_image_and_fields: игрок с модулями; справа изображение и поля без модульной сетки.
- Module_details_are_available_on_left: исправный/повреждённый/отключённый/уничтоженный модуль; прежние сведения доступны слева.
- Panels_survive_resize_and_selection: смена выбранного объекта и resize сохраняют карточку и доступ к модулям.

План при реализации: dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~PointCombatUS0020TK0002Tests. Hard cap каждого процесса 300000 ms с завершением дерева. Затем релевантные Client regressions, owning build, scoped format и git diff --check.

Native: повторить исходный пользовательский сценарий, проверить обычный масштаб, zoom/pan и resize; сверить ожидаемый результат. Визуальные критерии требуют отдельной человеческой проверки.

## Definition of Done

US20-AC2 доказан named tests; owning build/scoped format/diff проходят. Ручной результат и ограничения записаны отдельно. Сейчас implementation NOT STARTED; automated/native/product NOT RUN. Текущее поручение — только planning, без игры и commit/push.

## Scope update — execution 2026-10-21

Include existing Client tests PointCombatUS0013TK0001Tests.cs and PointCombatUS0016TK0001Tests.cs: their old right-side diagram assertions must verify left-side details and right-side ship instead. Current authorization supersedes planning-only restrictions.

## Execution result — 2026-10-21

Right Player Ship now uses the existing image/BuildLines card. Presented module information is updated before left command panels: each module retains name, armor/structure, critical warning, disabled countdown, destruction and transient hit damage; Navigation carries destruction summary and modules without dedicated controls. Weapon profile/operator/reload details remain. Detail text wraps and the whole left panel scrolls; body sizing accounts for details and command rows.

Validation: Client `--filter "FullyQualifiedName~PointCombatUS0020TK0002Tests|FullyQualifiedName~PointCombatUS0013TK0001Tests|FullyQualifiedName~PointCombatUS0016TK0001Tests|FullyQualifiedName~CommandsPanelTests"`: 19/19 passed, including 4 new parameterized cases; dependencies/Client build passed. Scoped format for the two changed production files exit 0; diff check exit 0. Checks capped at 300000ms. Self-review completed. Native/product visual acceptance NOT RUN/OPEN.
