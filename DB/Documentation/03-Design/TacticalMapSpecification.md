# Дизайн тактической карты

> **Контракт EP-0013.** Дополнение EP-0013: правый overlay360px×UI scale, пять секций/25 полей, mouse capture перед aim/threat/zoom, неизменные данные без managed allocations в самой панели. [Панель](../04-Engineering/DebugTuningPanel.md) и [ручные оси](../04-Engineering/ManualManeuver.md) задают текущую семантику взаимодействия; renderer использует подтверждённые значения, pending отделён визуально.

Карта — отображение допущенных snapshot/observations и локального состояния камеры.
Она не меняет Engine physics и не раскрывает скрытый NPC state. Геометрия мира
не зависит от zoom. LOD, markers, labels и clustering задаются Settings, а не
другой версией игровых координат. Палитра боя и input загружаются из Data/UI.

Левая область содержит module commands и health; правая — выбранный объект,
его изображение и ship fields. Панели учитывают viewport, перенос и прокрутку.
UI hit-test имеет приоритет над map drag/zoom; колесо над списком прокручивает список.
Orbits/Territories/Fields/POI переключаются локально, включая соответствующий hit-test.

Свободный drag панорамирует, Follow следует игроку, selection/hover отделены.
Повторный выбор перекрытий перебирает кандидатов без случайного скачка камеры.
Ctrl+free click отправляет engine.orbit. Выбранная линия идёт через указанную
точку до края viewport. Preview команды не выдаётся за выполненный манёвр.

Движение, полёт и reports рисуются на согласованной presentation timeline.
NPC future line строится из наблюдений; NPC torpedo не имеет передней траектории,
показывает точку, короткий хвост и тёмный наблюдённый след. Threat classification
остаётся отдельной UI-информацией. Численные palette/scale значения находятся
в content. Стиль кнопок — [STYLE](TacticalButtons/STYLE.md).

Источники: [Settings.json](../../../src/DeepSpaceSaga.Client/Settings.json), [GameSessionScreen.MapView.cs](../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs), [UI](../../../src/DeepSpaceSaga.Client/Data/UI).
