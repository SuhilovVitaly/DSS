# Точечный бой

> **Контракт EP-0013.** EP-0013 уточняет текущий point combat: [DebugTuningPanel](DebugTuningPanel.md) задаёт авторитетный live retiming кораблей, captured point flights и reload; [ManualManeuver](ManualManeuver.md) задаёт совместные speedHold/turnHold, schema2 и клавиши Q/E/Z/C. В этих областях прежние keyboard назначения и отсутствие live overlay считаются заменёнными. Endpoint, ownership, damage/scatter и RNG сохраняются.

Engine владеет проверкой пуска, aim/scatter RNG, полётом, моментом взрыва,
радиусным поражением, уроном модулей, AI, encounter и сохранением.
Motion предоставляет общую геометрию; Client показывает допущенные данные.
Точка назначения и профиль выбираются при пуске; отменённая guided-contact
модель, старые fire modes и автоматическая/очередная ПРО не применяются.

Профили, скорости, радиусы, дальности и reload заданы в Data/Modules.
`combat-rules.json` задаёт armor, hit-disable, critical roles, blast, scatter
и реакцию NPC. Не дублируй их численные значения в UI. Пуск захватывает
параметры; последующий tuning проходит явный Engine retiming, не пересчитывается
неявно из изменённого JSON. Произвольная hot-reload content не поддерживается.

Контрмеры запускаются вручную. Уклонение и манёвры не меняют ownership/identity
projectile. Повреждения учитывают броню и структуру модулей; попадание,
временное отключение и разрушение — разные состояния. Критические bridge/engine
и порог разрушенных модулей определяются combat rules. Окончание боя/Defeat
согласуется с авторитетной сценой и прекращением дальнейших side effects.

В бою доступны 0/1/2/5/10×. При 1× motion = real/4; длительность reload в
физическом времени не меняется от скорости. Engine создаёт одно собственное
reload-complete событие на цикл; journal/report/audio дедуплицируются.
Начальная готовность и Load уже завершённого цикла не дают ложного звука.

Клиентский preview и worker-прогноз не являются будущим Engine snapshot.
NPC flight marker показывает текущую точку, хвост до 18 px и тёмный уже
наблюдённый след, без передней геометрии и подписи профиля. Классификация угрозы
доступна отдельно. Успешная отправка торпеды выходит из aiming; неуспешная
валидация/ожидание подтверждения дружеской цели не должны терять нужный контекст.
Модульные подробности собственного корабля находятся слева в командных группах,
справа — изображение и данные выбранного объекта.

Save19 продолжает flights/deadlines/RNG; Save18 имеет отдельную миграцию.
Подробности: [сохранение](SaveFormat.md), [reports](CombatReports.md),
[наблюдаемый NPC-прогноз](NpcObservedTrajectories.md).
Баланс, native input/audio и доступность проверяются отдельно; статистика
старых запусков не является свойством текущего контракта.

Источники: [SimulationEngine.PointCombat.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs), [SimulationEngine.PointAim.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.PointAim.cs), [SimulationEngine.Evasion.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.Evasion.cs), [SimulationEngine.Encounter.cs](../../src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs), [combat-rules.json](../../src/DeepSpaceSaga.Client/Data/Combat/combat-rules.json), [Modules](../../src/DeepSpaceSaga.Client/Data/Modules).
