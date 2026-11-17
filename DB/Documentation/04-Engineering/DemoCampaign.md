# Непрерывная демо-кампания

Публичный профиль задаётся `gameSettings.campaignProfile: "demo"` в
[Settings.json](../../../src/DeepSpaceSaga.Client/Settings.json). Новая игра
использует [Scenarios/Demo](../../../src/DeepSpaceSaga.Client/Scenarios/Demo/scenario.json):
один фиксированный район из пяти станций с ролями service, mining, industrial,
agricultural, far_trade. Геометрия использует существующий TradingMap generator;
это не запуск полного SolarSystem generator. Seed, station IDs и роли проверяются
по [cluster.json](../../../src/DeepSpaceSaga.Client/Data/Demo/cluster.json).
Стартовое имущество берётся только на New Game из демо-manifest
[demo-starter-carrier.json](../../../src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json):
общий starter-carrier с дополнительным жилым модулем для четырёх пассажирских
кают ([перевозки](DemoTransportContracts.md)); другие сценарии используют общий manifest.
Обучение использует собственные дроны, пилотов и боезапас этого носителя.

## Последствия вылета

Engine применяет единственный `CommitSortieOutcome`: корпус, постоянное крыло,
боезапас и последний settlement. Повторный выход из того же боя не применяет
потери повторно. Уничтоженный дрон остаётся tombstone; его пилот удаляется из
живого crew, назначение очищается, каюта освобождается. Мемориал сохраняет полную
проекцию пилота, battleId и календарное `GameTimeMs`. Disabled не равен гибели.
Миграция старого слота не убивает пилотов ретроактивно.

Живой носитель возвращается на карту. Без готового крыла мир остаётся доступным,
но UI объясняет необходимость восстановления. Ремонт, снабжение, замена дронов,
наём и содержание описаны в [CompanyServices](CompanyServices.md). Носитель с нулевым корпусом ведёт на
терминальный экран; доступны checkpoint, новая кампания, ручная загрузка и меню.
Загрузка терминального сохранения не воскрешает носитель.

## Авторитет и transport

[DemoCampaign](../../../src/DeepSpaceSaga.Contracts/DemoCampaign.cs) содержит
immutable command identity, expected revision, response и snapshot. Engine
сохраняет окно из 1024 последних payload hash и ответов: повтор идентичной команды в окне возвращает прежний
ответ, другая нагрузка с тем же ID отвергается. Для применённых команд, кроме запросов котировок, сохраняется долговечный hash: вне окна тот же payload получает `already_applied`, изменённый — `identity_conflict`. Исходный ответ вне окна не гарантируется; повторных side effects нет. Неподдерживаемые команды не
выполняют gameplay. Company/Start проецируют подтверждённый мир; Transport проецирует каюты, доску заказов и перевозки ([контракт](DemoTransportContracts.md)); секции
encounters, rewards, progression, story являются поверхностью расширения,
а не доказательством готовности соответствующих механик.

GameSessionHandle отправляет команды через асинхронный connection, объединяет
ожидание одного command ID, отбрасывает старые snapshot sequences и отменяет
ожидания закрытой сессии. Рендер не изменяет Engine и не рассчитывает второй
settlement. Каталоги Demo проходят строгую проверку schema, диапазонов и ссылок;
семантический fingerprint только стабильного cluster сохраняется. Добавление optional services/jobs/encounters/progression и форматирование JSON не меняют его. Старый отпечаток исходного cluster принимается и нормализуется при загрузке. Будущие owning-эпики отвечают за миграцию собственного состояния при изменении правил. Ошибка Demo content не блокирует development-миры. [Save22](SaveFormat.md) проверяет целостность состояния
и мемориала до принятия мира.

## Запись и продолжение

После принятия демо-сессии клиент наблюдает gameplay revision и стыковку. Изменение
запрашивает асинхронную запись в существующий campaign-continuation (отдельно от F5/F9 quicksave). Запросы
объединяются LocalClient; запись сериализуется общим save gate и заменяет файл
только после успешной записи temp. Внутри активного боя сохранение запрещено;
перед собственным вылетом записывается живой checkpoint; после выхода живой мир обновляет его с повтором без нового settlement. Терминальный мир не перезаписывает continuation, но может быть сохранён вручную.
Ручные слоты автоматически не перезаписываются.

UI показывает Saving/Saved/Failed вне боя; Saved исчезает через пять секунд. F6 во время боя не запрашивает запись. Saved означает завершённую запись, F6 повторяет
неудачную запись. При замене сессии старый autosave приостанавливается и ожидается
до пяти секунд; кандидат после Load принимает первый snapshot как baseline без записи; autosave начинается с последующего gameplay изменения или новой стыковки. Ошибка или timeout
не выдаются за сохранение. Закрытие также пытается дождаться записи; аварийное
завершение до replace оставляет последний durable файл и может потерять ещё не
записанные изменения.

Continue проверяет слоты по времени записи полным loader и выбирает самый свежий
совместимый мир с живым носителем. Повреждённые и терминальные слоты пропускаются, отсутствие валидного явно сообщается.
Терминальная кнопка checkpoint использует campaign-continuation, ручной Load позволяет выбрать
другой слот. Новый demo-профиль не скрывает Load старых совместимых миров.

Владельцы: [Engine campaign](../../../src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs),
[pilot loss](../../../src/DeepSpaceSaga.Engine/SimulationEngine.PilotLoss.cs),
[LocalClient autosave](../../../src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.Autosave.cs),
[client autosave](../../../src/DeepSpaceSaga.Client/GameSessionHandle.Autosave.cs).
