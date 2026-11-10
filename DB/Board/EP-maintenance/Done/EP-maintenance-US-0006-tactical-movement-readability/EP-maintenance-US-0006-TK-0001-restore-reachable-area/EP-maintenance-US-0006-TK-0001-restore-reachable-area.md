---
epic: EP-maintenance
story: EP-maintenance-US-0006-tactical-movement-readability
ticket: EP-maintenance-US-0006-TK-0001-restore-reachable-area
title: Вернуть область достижимости и упростить траектории движения
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
files_touched: 2
serves: [AC1, AC2, AC3]
created: "2026-11-10T07:23:22Z"
revision: 1
---

# Вернуть область достижимости и упростить траектории движения

[История и критерии приёмки](../EP-maintenance-US-0006-tactical-movement-readability.md) · [Слои движения EP-0019](../../../../EP-0019-tactical-battle-ui-redesign/EP-0019-US-0005-map-presentation/EP-0019-US-0005-TK-0002-movement-overlays/EP-0019-US-0005-TK-0002-movement-overlays.md) · [Предупреждение о пыли EP-0020](../../../../EP-0020-tactical-battle-mechanics/EP-0020-US-0010-anomaly-ui/EP-0020-US-0010-TK-0002-anomaly-move-fire/EP-0020-US-0010-TK-0002-anomaly-move-fire.md)

## Why

В кадре подготовки движения есть граница возможных конечных точек выбранного дрона, но рендер рисует только пути и их концы. Траектории сейчас бирюзовые, а выбранная линия имеет широкое свечение и обычную текстовую выноску.

## Decisions

Пользователь запросил вернуть зону движения, убрать `Path end` и сделать линии тоньше и серыми. Исходные формулировки и описание скриншота приведены в истории.

## Assumptions

- «Зона» означает область достижимых конечных точек одного выбранного своего дрона. Её геометрия берётся из `Reachable` в порядке готовой границы.
- «Линии» означает планируемые траектории движения на скриншоте. Рабочая толщина в координатах макета 720p — 1,5 для выбранной и 1,0 для остальных. Использовать нейтральный серый с большей яркостью у выбранной линии.
- Убирается стандартная выноска `Path end` во всех локалях. Отдельное предупреждение о запрете стрельбы из пыли сохраняется.

## Code context

| Файл | Текущее состояние | Допустимое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Movement.cs` | Строки 8–28 рисуют планы, квадраты и выноску. Толщины 8 и 3 для выбранного пути, 1,3 для остальных. `Reachable` не читается. | Добавить зону под путями, изменить штрихи путей и оставить выноску только для пыли. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MapMovementTests.cs` | Строки 11–22 проверяют цвет пути, четыре конечных квадрата и перетаскивание в сцене `1-move`. | Обновить проверку цвета, добавить проверки зоны и обычной выноски, сохранить проверку перетаскивания. |

Источники для чтения: `TacticalBattleOrders.cs:145–158,178–204` отдаёт упорядоченную границу выбранного дрона. `TacticalBattleScreen.cs:120` передаёт `Movement` только при подготовке движения. `BattleMapRenderer.cs:68–80` рисует слой перед маркерами юнитов.

Сгенерированные эталоны для визуальной регрессии в `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Snapshots/` учитываются отдельно от двух файлов реализации и тестового кода. После визуальной сверки обновить затронутые кадры подготовки движения `1-move.png`, `1-move-no-hover.png`, `1-anomaly-move-dust.png` и `1-anomaly-hover.png`. Механизм обновления описан в `View/Ep0019Snapshots.cs:16–28`.

## Public API after the change

Публичные сигнатуры и схемы данных сохраняются.

## Implementation steps

1. До прохода путей отрисовать полупрозрачную заливку и тонкий различимый контур замкнутой границы `movement.Reachable`. Использовать `f.View.Project` и существующее отсечение поля. При отсутствии выбранного дрона или при вырожденной границе пропустить слой.
2. Отрисовать каждый план одним тонким серым штрихом, без второго прохода свечения. Для квадратов концов сохранить текущие размеры, видимость, запись геометрии и резервирование места для подписей.
3. Создавать выноску и резервировать место под неё только при окончании выбранного пути в пыли. Сохранить специальный текст и цвет предупреждения.
4. Проверить новую геометрию и стиль, затем сверить и обновить перечисленные эталонные кадры.

## Out of scope

Изменение расчёта приказов, хит-теста, локализационных файлов и любых визуальных слоёв вне подготовки движения.

## Invariants

- Линии и квадраты берутся из `TacticalOrderPreview.Path`, зона — из `TacticalMovementFrame.Reachable`. Рендер использует готовую геометрию модели.
- Квадраты концов сохраняют стороны 10/8 единиц макета, а существующий хит-тест в `TacticalBattleOrders.cs:161–175` остаётся применим.
- Условие предупреждения о пыли сохраняется и проверяется тестом `AnomalyFirePanelTests.Move_end_in_dust_callout`.

## Tests

- `Forecast_path_and_endpoint_drag_use_order_model` — проверить тонкую серую линию на участке вне квадратов и подписей, квадрат каждого конца, отсутствие `callout:endpoint` вне пыли и изменение приказа перетаскиванием.
- `Reachable_area_follows_selected_drone_during_move_planning` — проверить растр внутри и вне спроецированной границы, смену области при выборе другого дрона и отсутствие области у кадра без плана движения. Визуально сверить выбранную и остальные линии на масштабе 100% и 150%.
- Сохранить `Move_end_in_dust_callout` и выполнить `Ep0019ViewTests` после проверки изменившихся эталонов. Пороги сравнений сохраняются.
- Будущие команды из корня DSS. Создать каталог результатов с уникальным `RUN-ID` и ограничить дерево каждого тестового процесса 300000 мс.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter 'FullyQualifiedName~MapMovementTests|FullyQualifiedName~Move_end_in_dust_callout|FullyQualifiedName~Ep0019ViewTests' --results-directory D:/DeepSpaceSaga/artifacts/validation/maintenance-reachable-area/RUN-ID
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Movement.cs tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MapMovementTests.cs
```

## Definition of Done

- AC1–AC3 выполнены в штатном рендере. Проверки перетаскивания и предупреждения о пыли проходят.
- Визуальные эталоны соответствуют новому виду. Сборка и проверки проходят либо исходное падение записано отдельно.
- Статус реализации и фактические результаты записаны в тикете.

## Self-containment check

Тикет задаёт источник и порядок точек зоны, условия видимости, толщины путей, исключение для пыли, границы правки и проверки. Зависимостей от других тикетов нет.

## Статус

Реализовано 2026-11-10. Область достижимости использует границу выбранного дрона. Траектории серые, выноска сохраняется для пыли. `dotnet build` Client, `dotnet format whitespace --verify-no-changes` и `git diff --check` прошли. `MapMovementTests` и `Move_end_in_dust_callout` — 3/3. Сверены кадры 100% и 150% и обновлены четыре разрешённых эталона. Совместный прогон с `Ep0019ViewTests` — 27/40: 11 расхождений других PNG воспроизведены на исходном `4dfd5dc2`, две проверки `Scene_probes_pass` ещё ожидают бирюзовые линии. Код этих проверок лежит вне разрешённого контекста. Журнал, растровые кадры и исходный прогон находятся в `D:/DeepSpaceSaga/artifacts/validation/maintenance-reachable-area/`.
