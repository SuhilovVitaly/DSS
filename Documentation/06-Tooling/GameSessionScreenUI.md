# Ассеты и данные тактического UI

> **Контракт EP-0013.** EP-0013: [панель](../04-Engineering/DebugTuningPanel.md) владеет cached Skia ресурсами, mouse routing и pending text; [маневрирование](../04-Engineering/ManualManeuver.md) использует native keyboard edges и независимые оси. Нативный режим проверки описан в [Performance/EP0013](../../tools/DeepSpaceSaga.Performance/EP0013.md). Raster, real OpenGL managed-input и physical/human acceptance учитываются отдельно.

Настройки карты находятся в Settings.gameSettings.tacticalMap; параметры
боевого представления и ввода — в Data/UI. Панели работают с принятыми
DTO и локальными UI-состояниями, не с прямым вызовом Engine из Render.
Кнопочные PNG экспортируются Export-TacticalButtonAssets.ps1; размеры и
семантика hover задаются [стилем](../03-Design/TacticalButtons/STYLE.md).
Изображения персонажей используют manifests своих наборов, а не отдельные
скриншоты в Documentation. Численные color/input настройки не следует
копировать в несколько документов. Слои, геометрия и UI hit-test должны
использовать общий подготовленный кадр и актуальный viewport.

CommandsPanel сохраняет SKPicture, detail font/paints и reload path до изменения
входов; деактивация освобождает presentation cache. Ключи Combat.Panel.* покрывают
RU/EN, единицы, состояния и символы. Detail font должен содержать одновременно
кириллицу, bar cells и маркеры; проверять только наличие строки в JSON недостаточно.
ModuleDiagram хранит данные левых групп, отдельного правого renderer нет.
Настройки defaultTorpedoProfile/defaultCountermissileProfile находятся в
Data/UI/combat-visuals.json; это UI defaults, а не изменение боевого каталога.
Нативный режим --module-panels-window описан в [Performance README](../../tools/DeepSpaceSaga.Performance/README.md#module-panel-native-validation).
Он проверяет реальный OpenGL и managed callbacks, сохраняя hardware/human gates отдельно.

Источники: [Export-TacticalButtonAssets.ps1](../../tools/Export-TacticalButtonAssets.ps1), [Settings.json](../../src/DeepSpaceSaga.Client/Settings.json), [UI](../../src/DeepSpaceSaga.Client/Data/UI).
