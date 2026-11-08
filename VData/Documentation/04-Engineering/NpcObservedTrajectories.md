# Прогноз движения NPC на карте мира

Будущая линия NPC строится по допущенным наблюдениям и текущей отображаемой позе. При отсутствии подтверждённой тенденции она продолжает текущий курс; при наблюдаемом повороте — дугу постоянной скорости. Прогноз не раскрывает маршрут, будущие команды или скрытые параметры двигателя.

`NpcMotionObservationHistory` использует физическое время и ограниченную историю курсов. Повтор паузы не создаёт новое наблюдение; разрыв, потеря объекта и смена сессии сбрасывают соответствующий след. История не сохраняется. `FutureTrajectoryProjector` не вызывает Engine и не меняет мир. Прогноз отдельного боя описан в [его контракте](TacticalBattle.md).

Источники: [NpcMotionObservationHistory](../../../src/DeepSpaceSaga.Client/NpcMotionObservationHistory.cs), [FutureTrajectoryProjector](../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/FutureTrajectoryProjector.cs).