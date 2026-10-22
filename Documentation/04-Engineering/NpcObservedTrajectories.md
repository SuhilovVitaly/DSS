# Прогноз движения NPC по наблюдениям

Будущая линия NPC строится в Client по наблюдениям, уже допущенным в сцену.
Нельзя читать скрытый Engine snapshot, будущий маршрут, активную команду или
параметры чужого двигателя для улучшения player-visible прогноза.

NpcMotionObservationHistory хранит ограниченную историю подтверждённых курсов.
Прямой текущий курс — безопасный fallback до подтверждения наблюдаемого поворота.
Расчёт использует физическое motion time. Повтор паузы не создаёт нового
наблюдения; новая сессия, rollback, потеря/удаление объекта и разрыв наблюдений
сбрасывают соответствующий track. История не сохраняется в save.

FutureTrajectoryProjector и CombatForecastCache не меняют авторитетное движение.
Worker-результаты имеют session identity/cancellation; stale задача не должна
публиковаться в новый кадр/сессию. CalculationLimited — ограничение расчёта,
а не подтверждённый физический исход. Геометрия будущей линии должна зависеть
от доступного наблюдения и текущей позы; фактические дедлайны/урон принадлежат Engine.
Правила показа NPC torpedo заданы [точечным боем](PointAimCombat.md).

Источники: [NpcMotionObservationHistory.cs](../../src/DeepSpaceSaga.Client/NpcMotionObservationHistory.cs), [FutureTrajectoryProjector.cs](../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/FutureTrajectoryProjector.cs).
