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

Источники: [Export-TacticalButtonAssets.ps1](../../tools/Export-TacticalButtonAssets.ps1), [Settings.json](../../src/DeepSpaceSaga.Client/Settings.json), [UI](../../src/DeepSpaceSaga.Client/Data/UI).
