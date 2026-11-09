---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0002-overlay-layout-shell
ticket: EP-0019-US-0002-TK-0002-full-screen-map-shell
title: Карта на весь экран и удаление старой панели
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0002-TK-0001-design-tokens-and-primitives]
files_touched: 5
serves: [US2-AC1, US2-AC3]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Карта на весь экран и удаление старой панели

## Why

Новый экран — это карта на весь холст и панели поверх неё. Старая правая панель с вкладками и флажками не входит в дизайн (D-02, A-09, Q-01, Q-02).

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Q-01/Q-02 по умолчанию: вкладки «Стороны/Журнал/Прогноз» и «ИИ — всем» удаляются с экрана; классы вкладок остаются в коде, если их используют другие места, иначе удаляются вместе с тестами, которые проверяют только их отрисовку.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | :93-116 CaptureFrame (ширина панели :102), :118-147 Render, :149-228 DrawPanel, :336-473 мышь | Карта на весь viewport; `canvas.Scale(S)`; порядок слоёв DesignSpec §4; удалить старую панель, кнопки Fit/Exit/New/Replay, флажки, вкладки; хит-тест панелей по `PanelRects`; клавиши 0/Esc/Enter/Tab/стрелки сохранить. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | :27-80 Draw | Фон, сетка и линейка по DesignSpec §8.1. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` | модальное подтверждение выхода | Оформить токенами (`PanelBox`, `Button`, `BigButton`), центр экрана. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMovePanel.cs, TacticalBattleFirePanel.cs, TacticalBattleSidesTab.cs, TacticalBattleLogTab.cs, TacticalBattleForecastTab.cs` | панели и вкладки старого вида | Отключить от экрана; удалить неиспользуемые. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleKeyTests.cs, MouseRoutingTests.cs, KeyboardRoutingTests.cs, PanelHeaderTests.cs, SidesAndLogTests.cs, ForecastTabTests.cs` | проверки старой панели | Обновить на новый каркас; удалить проверки удалённых элементов, сохраняя проверки клавиш и маршрутов. |

## Public API after the change

`TacticalBattleScreen.PanelRects` (internal) — прямоугольники панелей в пикселях.

## Implementation steps

1. Ввести `BattleViewport` в `CaptureFrame`; карта получает весь экран.
2. Переписать `Render`: MapBase → содержимое карты → панели (пока пустые рамки по размерам DesignSpec) → модалка выхода.
3. Удалить старую панель, флажки, вкладки и кнопки; клавиши оставить.
4. Мышь: клики и колесо внутри `PanelRects` не идут в карту.
5. Обновить тесты маршрутов клавиш и мыши.

## Out of scope

Содержимое панелей и новые слои карты.

## Invariants

`BuildMoveRequest`/`BuildFireRequest`, блокировка мировых клавиш (`TacticalBattleFlow.cs:123-131`), подтверждение выхода и возврат камеры мира не меняются.

## Tests (исходный план; актуальные проверки ниже)

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `MouseRoutingTests.Panel_rectangles_block_map_clicks`
- `BattleKeyTests.Fit_exit_enter_tab_keys_still_work`
- `BattleMapRenderTests.Map_background_and_grid_cover_full_screen`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~MouseRoutingTests|FullyQualifiedName~BattleKeyTests|FullyQualifiedName~KeyboardRoutingTests|FullyQualifiedName~BattleMapRenderTests|FullyQualifiedName~PanelHeaderTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us2-tk2" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC1, US2-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Список удаляемых элементов — DesignSpec §10.

## Исполнение

См. проверенное исполнение ниже.

Scope: TacticalBattleScreen.Overlay.cs отделяет overlay geometry/input от session/presentation. Старые internal test hooks остаются для самостоятельных legacy проверок, но не подключены к active screen.

## Проверенное исполнение 2026-11-08 (продолжение)

Replace sidebar with full screen map and compact overlay hit regions

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/us2-tk2. Self-review выполнен; это не независимое review. Build в составе test run, scoped whitespace и diff-check проверены перед публикацией. Остаточные native/OS gates отражаются в ExecutionStatus.

## Фактическая привязка проверок

OverlayShellTests. Плановые имена тестов заменены реализацией тех же AC в указанных owning classes; результаты и paths — ExecutionStatus.md. Source scope разнесён по partial-компонентам overlay/map/phase для сохранения session ownership; никакой Engine-side реализации не добавлялось.
