---
epic: EP-maintenance
story: EP-maintenance-US-0004-tactical-map-orientation
ticket: EP-maintenance-US-0004-TK-0001-player-edge-arrow-and-focus-cross
title: Показать указатель на корабль за экраном и уменьшить крест фокуса
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
files_touched: 4
serves: [AC1, AC2]
created: "2026-11-10T07:18:31Z"
revision: 1
---

# Показать указатель на корабль за экраном и уменьшить крест фокуса

[История и критерии приёмки](../EP-maintenance-US-0004-tactical-map-orientation.md) · [Связанный тикет по навигации и фокусу карты](../../../../EP-0018-map-geography-and-scale/EP-0018-US-0007-map-navigation-and-estimates/EP-0018-US-0007-TK-0001-cluster-focus-navigation/EP-0018-US-0007-TK-0001-cluster-focus-navigation.md)

## Why

За пределами экранной области тактической карты корабль игрока выпадает из существующего прохода стрелок. Центральный указатель камеры занимает 88 экранных пикселей по каждой оси между крайними концами штрихов.

## Decisions

- Пользователь запросил указатель на собственный корабль, когда тот уходит за пределы видимой карты, и простой крест того же цвета вдвое меньшего размера. Исходные формулировки приведены в истории.

## Assumptions

- Скриншот относится к центральному указателю фокуса камеры. Половина размера означает 44 экранных пикселя от конца до конца по каждой оси, без учёта толщины линии и сглаживания.
- Видимость корабля определяется по центру его маркера в полном экранном прямоугольнике карты. Свободная от панелей область определяет место размещения стрелки. Перекрытие маркера панелью внутри экрана само по себе стрелку не включает.
- Для отличия от стрелок целей используется существующая локализованная подпись `World.Panel.PlayerShip` (English.json и Russian.json, строка 559). Стрелка показывает направление на свой корабль без расстояния до него.

## Code context

| Файл | Текущее состояние | Допустимое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs` | Строки 293–315 вычисляют свободную область карты. Строки 430–465 рисуют краевые стрелки и исключают корабль игрока. | Добавить собственный корабль в этот проход, используя существующее проектирование, размещение и дедупликацию. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapDepthRenderer.cs` | Строки 19–22 задают старый полуразмах 26 + 4 + 14 = 44. Строки 38–45 задают цвет и кисть. Строки 242–293 рисуют скобки и штрихи. | Заменить геометрию центрального указателя на крест. Сохранить кисть и сигнатуру `DrawFocusIndicator`. |
| `tests/DeepSpaceSaga.Client.Tests/WeaponHudControlsTests.cs` | Строки 13–46 проверяют появление стрелки выбранного контакта за экраном и отсутствие стрелки для контакта в пределах экрана. | Добавить сценарии стрелки корабля игрока. Сохранить регрессию для выбранного контакта. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalMapDepthRendererTests.cs` | Строки 203–299 проверяют растровое изображение старого указателя. | Обновить проверки геометрии центрального указателя под новый критерий. |

## Public API after the change

Публичные сигнатуры и схемы данных сохраняются.

## Implementation steps

1. В `DrawOffscreenTargets` взять состояние собственного корабля через `FindPlayerShip(_renderStates)`. Проецировать `Pose.X/Y` той же камерой, что рисует маркер корабля (`GameSessionScreen.cs:1254`). При отсутствии состояния стрелку не выводить.
2. Использовать проверку полного экранного прямоугольника и пересечение направления на корабль с краем свободной области с существующим отступом. Добавить одну стрелку с подписью корабля через тот же проход и ключ дедупликации. В тесном окне соблюдать существующую проверку наличия места.
3. В `DrawFocusIndicator` провести через переданный центр две линии с полуразмахом 22 экранных пикселя по горизонтали и вертикали. Использовать текущий цвет RGBA (151, 168, 184, 102) и текущую толщину линии 1,5. Убрать геометрию угловых скобок и внешних штрихов.

## Out of scope

Отдельный экран пошагового боя, управление камерой, игровой ввод, маркеры навигационных целей и изменение локализационных файлов.

## Invariants

- `GameSessionScreen.cs:1200` передаёт указателю текущий центр фокуса. Точка привязки и условия вызова сохраняются.
- В `GameSessionScreen.MapView.cs:439–452` стрелки выбранных и навигационных целей используют существующие условия появления и дедупликацию. Их подписи и расстояния сохраняются.
- Маркеры выбора и наведения отрисовываются отдельно в `GameSessionScreen.cs:1275–1277`.

## Tests

- `Player_ship_edge_arrow_tracks_offscreen_pose_and_clears_on_return`. Проверить выход маркера за левый и правый края, вверх, вниз и по диагонали. Проверить одну стрелку внутри свободной области, её направление и исчезновение при возвращении маркера. Проверить кадр без позиции корабля и одновременный показ стрелки цели. Для проверки кадра использовать штатный рендер панели карты.
- `Focus_indicator_draws_half_size_cross_in_existing_color`. На `SKCanvas` проверить закрашенный центр, четыре плеча длиной 22, пустые диагонали и отсутствие штрихов за новым размахом. Сверить цвет с прежней кистью с учётом растрового смешивания.
- При визуальной проверке сопоставить стрелку с положением корабля после сдвига и масштабирования карты. Проверить размещение рядом с панелями HUD при масштабах UI 100% и 150%.
- Будущие команды из корня DSS. Подставить уникальный `RUN-ID`, создать каталог результатов и установить лимит дерева тестовых процессов 300000 мс.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter 'FullyQualifiedName~WeaponHudControlsTests|FullyQualifiedName~TacticalMapDepthRendererTests' --results-directory D:/DeepSpaceSaga/artifacts/validation/maintenance-tactical-map-orientation/RUN-ID
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj
dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapDepthRenderer.cs tests/DeepSpaceSaga.Client.Tests/WeaponHudControlsTests.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapDepthRendererTests.cs
```

## Definition of Done

- AC1 и AC2 подтверждены именованными тестами и визуальной проверкой. Сборка и проверка форматирования проходят либо исходное падение зафиксировано отдельно.
- Изменения ограничены разрешёнными файлами. Инварианты соблюдены.
- Статус реализации и фактические проверки записаны в тикете.

## Self-containment check

Указаны источники позиции и подписи корабля, условия показа и место отрисовки стрелки, исходные размеры и цвет указателя фокуса. Зависимостей от других тикетов нет.

## Статус

Реализовано 2026-11-10. Краевая стрелка использует экранную позицию маркера корабля, крест фокуса имеет размах 44 px. `dotnet test` с фильтром `WeaponHudControlsTests|TacticalMapDepthRendererTests` — 14/14, `dotnet build` Client, `dotnet format --verify-no-changes` для четырёх файлов и `git diff --check` прошли. Кадры без нативного окна при 100% и 150% просмотрены в `D:/DeepSpaceSaga/artifacts/validation/maintenance-tactical-map-orientation/20261110-01/`.
