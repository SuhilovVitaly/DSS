# Тактическая карта и маневрирование

A/← и D/→ поворачивают; W/↑ и S/↓ разгоняют и тормозят. Shift задаёт разгон до максимума. [Ручные оси](../../04-Engineering/ManualManeuver.md) и [F12](../../04-Engineering/DebugTuningPanel.md) принадлежат карте мира. F8 открывает [подготовку тактического боя](../Screens/TacticalBattleSetup.md), где [свой набор управления](../Screens/TacticalBattle.md).

Карта рисует принятые объекты, орбиты, trail, selection/hover и локальные слои.
Панорамирование, Follow, масштаб и selection не меняют авторитетный мир.
Колесо над прокручиваемой панелью прокручивает её, а не карту. При перекрытии
маркеров выбор циклически перебирает допустимых кандидатов.
Ctrl+клик по свободной карте задаёт engine.orbit. Выбранная навигационная линия
проходит через выбранную точку и продолжается до края viewport. Ручной поворот,
Approach и синхронизация скорости — разные команды. NPC future line использует
только наблюдения. Настройки масштаба/LOD берутся из Settings.gameSettings.tacticalMap.
Детали: [дизайн карты](../../03-Design/TacticalMapSpecification.md) и
[NPC-прогноз](../../04-Engineering/NpcObservedTrajectories.md).

Источники: [Settings.json](../../../../src/DeepSpaceSaga.Client/Settings.json), [GameSessionScreen.MapView.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs).
