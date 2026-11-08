# Ручное управление кораблём на карте

Поворот и изменение скорости — независимые оси. Дефолты: A/← и D/→ поворачивают; W/↑ и S/↓ разгоняют и тормозят; Shift начинает разгон до максимума. Отпускание Shift не отменяет разгон; новый ввод скорости или достижение предела его снимает. Синонимы не дублируют команды, последняя из противоположных удерживаемых клавиш имеет приоритет.

`engine.turnHold` получает направление и Active; `engine.speedHold` получает направление разгона/торможения, Active и optional Latched. Команды проверяют свой корабль, двигатель и доступность. Движение опирается на `HoldMotion.Sample`: при достижении нуля или максимума меняется ускорение, а поворот продолжается. Общая математика используется для мира и прогноза текущего курса. Снимок сообщает подтверждённые ручные оси только для своего корабля.

Доки, диалог, остановка или блокировка двигателя ограничивают ввод по состоянию мира. Потеря фокуса, модальный переход и смена сессии очищают нажатия. Load восстанавливает позу и скорость без физического удержания и защёлки. Пауза останавливает физический ход времени.

Назначения читаются при старте из [maneuver-input.json](../../../src/DeepSpaceSaga.Client/Data/UI/maneuver-input.json); коллизии и недопустимые поля обрабатывает [ManeuverInputSettings](../../../src/DeepSpaceSaga.Client/ManeuverInputSettings.cs). В [тактическом бою](../02-FirstRelease/Screens/TacticalBattle.md) стрелки редактируют приказ дрона и не управляют кораблём мира.

Источники: [ShipEngineCommandTypes](../../../src/DeepSpaceSaga.Contracts/ShipEngineCommandTypes.cs), [HoldMotion](../../../src/DeepSpaceSaga.Motion/HoldMotion.cs), [SimulationEngine.SpeedHold](../../../src/DeepSpaceSaga.Engine/SimulationEngine.SpeedHold.cs), [GameSessionScreen.Maneuver](../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Maneuver.cs).