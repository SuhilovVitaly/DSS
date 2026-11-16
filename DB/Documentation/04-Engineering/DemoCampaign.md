# Непрерывная демо-кампания

Публичный профиль задаётся `gameSettings.campaignProfile: "demo"` в
[Settings.json](../../../src/DeepSpaceSaga.Client/Settings.json). Новая игра
использует [Scenarios/Demo](../../../src/DeepSpaceSaga.Client/Scenarios/Demo/scenario.json):
один фиксированный район из пяти станций с ролями service, mining, industrial,
agricultural, far_trade. Геометрия использует существующий TradingMap generator;
это не запуск полного SolarSystem generator. Seed, station IDs и роли проверяются
по [cluster.json](../../../src/DeepSpaceSaga.Client/Data/Demo/cluster.json).
Стартовое имущество берётся из общего starter-carrier manifest только на New Game.
Обучение использует собственные дроны, пилотов и боезапас этого носителя.

## Последствия вылета

Engine применяет единственный `CommitSortieOutcome`: корпус, постоянное крыло,
боезапас и последний settlement. Повторный выход из того же боя не применяет
потери повторно. Уничтоженный дрон остаётся tombstone; его пилот удаляется из
живого crew, назначение очищается, каюта освобождается. Мемориал сохраняет полную
проекцию пилота, battleId и календарное `GameTimeMs`. Disabled не равен гибели.
Миграция старого слота не убивает пилотов ретроактивно.

Живой носитель возвращается на карту. Без готового крыла мир остаётся доступным,
но UI объясняет необходимость восстановления. Полноценные ремонт, найм и покупка
дронов принадлежат последующим демо-эпикам. Носитель с нулевым корпусом ведёт на
терминальный экран; доступны checkpoint, новая кампания, ручная загрузка и меню.
Загрузка терминального сохранения не воскрешает носитель.

## Авторитет и transport

[DemoCampaign](../../../src/DeepSpaceSaga.Contracts/DemoCampaign.cs) содержит
immutable command identity, expected revision, response и snapshot. Engine
сохраняет payload hash и ответ: повтор идентичной команды возвращает прежний
ответ, другая нагрузка с тем же ID отвергается. Неподдерживаемые команды не
выполняют gameplay. Company/Start проецируют подтверждённый мир; секции transport,
encounters, rewards, progression, story являются поверхностью расширения,
а не доказательством готовности соответствующих механик.

GameSessionHandle отправляет команды через асинхронный connection, объединяет
ожидание одного command ID, отбрасывает старые snapshot sequences и отменяет
ожидания закрытой сессии. Рендер не изменяет Engine и не рассчитывает второй
settlement. Каталоги Demo проходят строгую проверку schema, диапазонов и ссылок;
их fingerprint сохраняется. [Save22](SaveFormat.md) проверяет целостность состояния
и мемориала до принятия мира.

## Запись и продолжение

После принятия демо-сессии клиент наблюдает commit sequence и стыковку. Изменение
запрашивает асинхронную запись в существующий continuation/quicksave. Запросы
объединяются LocalClient; запись сериализуется общим save gate и заменяет файл
только после успешной записи temp. Внутри активного боя сохранение запрещено;
после выхода действует существующий checkpoint с повтором без нового settlement.
Ручные слоты автоматически не перезаписываются.

UI показывает Saving/Saved/Failed; Saved означает завершённую запись, F6 повторяет
неудачную запись. При замене сессии старый autosave приостанавливается и ожидается
до пяти секунд; кандидат начинает писать только после принятия. Ошибка или timeout
не выдаются за сохранение. Закрытие также пытается дождаться записи; аварийное
завершение до replace оставляет последний durable файл и может потерять ещё не
записанные изменения.

Continue проверяет слоты по времени записи полным loader и выбирает самый свежий
совместимый. Повреждённые слоты пропускаются, отсутствие валидного явно сообщается.
Терминальная кнопка checkpoint использует quicksave, ручной Load позволяет выбрать
другой слот. Новый demo-профиль не скрывает Load старых совместимых миров.

Владельцы: [Engine campaign](../../../src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs),
[pilot loss](../../../src/DeepSpaceSaga.Engine/SimulationEngine.PilotLoss.cs),
[LocalClient autosave](../../../src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.Autosave.cs),
[client autosave](../../../src/DeepSpaceSaga.Client/GameSessionHandle.Autosave.cs).
