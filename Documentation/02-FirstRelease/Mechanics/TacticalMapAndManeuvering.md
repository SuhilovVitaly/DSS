# Тактическая карта и маневрирование

> **Контракт EP-0013.** Актуальное управление EP-0013: A/← и D/→ непрерывно поворачивают; W/↑ и S/↓ разгоняют/тормозят в бою и вне боя. Оси независимы, синонимы и противоположные клавиши обрабатываются стеком, Shift защёлкивает разгон. E — прицел ПРО, Z/C — уклонение. [Полный текущий контракт](../../04-Engineering/ManualManeuver.md) заменяет прежнюю keyboard step-схему. [F12-панель](../../04-Engineering/DebugTuningPanel.md) перехватывает мышь, сохраняя боевые клавиши.

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

Источники: [Settings.json](../../../src/DeepSpaceSaga.Client/Settings.json), [GameSessionScreen.MapView.cs](../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs).
