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

## Панели модулей

В тесном окне сначала раскрывается явно выбранная группа, затем торпедный
аппарат, ПРО, навигация, двигатель и остальные группы. Ручное сворачивание
сохраняется. Высота заголовков учитывает фактическое тело панели; при настоящем
переполнении доступна прокрутка. Правая карточка не содержит отдельного module diagram.

Торпедная таблица показывает клавишу и маркер выбора, профиль, скорость, R,
ядро и при наведении время до курсора с sigma рассеивания. Ядро равно R,
умноженному на авторитетный CoreFraction. Неоператорский прогноз не подменяется
выдуманным sigma. Строка оператора содержит имя, accuracy и sigma на25км;
у ПРО опорная дальность10км. Damage, decimal armor piercing, reload и дальность
поступают из snapshot, а не копии каталога в Client.

ПРО показывает широкую/быструю строки для одного наблюдаемого торпедного
контакта и различает ранний, поздний и недостижимый подлёт. Без контакта
не показывается выдуманное время перехвата. Подписи клавиш берутся из текущих
bindings. Выбор профиля помнится отдельно для owner/module до конца сессии;
defaultTorpedoProfile/defaultCountermissileProfile в Data/UI/combat-visuals.json
задают medium/wide. Неизвестный default использует первый доступный профиль.

Все длительности показываются в реальных секундах: physicalSeconds ×4/K,
где K — скорость игры. На паузе используется последняя ненулевая скорость,
до первого запуска —1×. Значения от10с округляются до целых, меньшие — до десятых.
Tooltip поясняет K и физическую длительность. Кольцо reload показывает число;
hover кнопки не скрывает состояние и таймер. Можно войти в подготовительный
прицел во время reload/отключения/разрушения или без оператора, но клик не
отправляет launch и показывает красную причину. Engine остаётся финальным арбитром.

Двигатель показывает current/max speed, acceleration, turn rate, время до
максимума и90° с тем же преобразованием времени. Нулевые rates не создают
NaN/бесконечность. Отказ двигателя различим и приглушает данные.

Броня и структура подписаны, имеют полосы и текущие числа; tooltip показывает
current/max. Critical marker имеет пояснение. Navigation показывает n/L;
DestroyedModuleLimit — ceil(ownModuleCount × destroyedModuleFraction) из Engine,
nullable для старых producers. Предупреждение начинается при n≥L−1.
Чужой модульный предел не проецируется. CommandsPanel кеширует подготовленный
рисунок и Skia-ресурсы; неизменный warmed Render не выделяет managed память.
Подготовка данных и весь экран не входят в этот узкий allocation-контракт.

LauncherCombatSnapshot и DefenseSnapshot содержат текущие point-параметры;
retired guided/mode/burst поля исключены из live DTO. ActiveTorpedoIds сохраняется
для действующих flight consumers. Старые wire-поля принадлежат отдельным
save DTO, см. [SaveFormat](SaveFormat.md).

Save19 продолжает flights/deadlines/RNG; Save18 имеет отдельную миграцию.
Подробности: [сохранение](SaveFormat.md), [reports](CombatReports.md),
[наблюдаемый NPC-прогноз](NpcObservedTrajectories.md).
Баланс, native input/audio и доступность проверяются отдельно; статистика
старых запусков не является свойством текущего контракта.

Звуковые сигналы воспроизводятся через OpenAL Soft на отдельном потоке,
владеющем native context. Настроенные PCM buffers подготавливаются до playback;
render только ставит cue в ограниченную очередь. Session epochs и интервалы
ограничивают stale/повторные сигналы. При недоступном устройстве выводится одно
предупреждение, клиент продолжает работу. PCM — mono16 с 5ms огибающей;
44100Hz для тонов ниже22050Hz, 96000Hz для более высоких поддержанных частот.
[Платформа и приёмка звука](../06-Tooling/Ubuntu.md).

Источники: [SimulationEngine.PointCombat.cs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs), [SimulationEngine.PointAim.cs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.PointAim.cs), [SimulationEngine.Evasion.cs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.Evasion.cs), [SimulationEngine.Encounter.cs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs), [combat-rules.json](../../../src/DeepSpaceSaga.Client/Data/Combat/combat-rules.json), [Modules](../../../src/DeepSpaceSaga.Client/Data/Modules).
