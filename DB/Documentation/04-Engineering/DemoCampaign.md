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
два жилых модуля по четыре каюты и семь членов экипажа, включая инженера
([перевозки](DemoTransportContracts.md)). Tutorial и Default используют общий manifest.
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
выполняют gameplay. Company/Start проецируют подтверждённый мир. Transport содержит
каюты, [станционные предложения](DemoTransportContracts.md), пассажирские и грузовые обязательства, доставку и отказы.
Encounters содержит [пиратские встречи](../02-FirstRelease/Mechanics/DemoEncounters.md),
наблюдения, доклады и связь с боем; ThreatRoutes — варианты рейса. Rewards,
progression и story остаются поверхностями расширения.

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
перед собственным вылетом записывается checkpoint; после выхода мир обновляет его с повтором без нового settlement. Для обычного вылета терминальный мир не перезаписывает continuation. Для captured встречи pending checkpoint обязательно заменяется finished результатом, включая гибель игрока, до закрытия боя: старый pending не должен воскресить уже завершённую встречу.
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

Pending encounter восстанавливается с теми же IDs, seed и prebattle состоянием
с начала боя, без доступной для управления карты между загрузкой и боем.
Во время captured/pending встречи переходы к другому миру запрещены. После
settlement группа retired, остальные группы не меняются. Повтор записи finished
не повторяет потери. [Полный контракт встречи](../02-FirstRelease/Mechanics/DemoEncounters.md).

Владельцы: [Engine campaign](../../../src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs),
[pilot loss](../../../src/DeepSpaceSaga.Engine/SimulationEngine.PilotLoss.cs),
[LocalClient autosave](../../../src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.Autosave.cs),
[client autosave](../../../src/DeepSpaceSaga.Client/GameSessionHandle.Autosave.cs).

## Послебоевая кампания

Rewards содержит отдельные сведения о наградах, выплатах и добыче, Company — roster пленных. Подтверждение исхода фиксирует sortie до открытия результата, мир остаётся на паузе. Continue восстанавливает стадию результата и стабильный ledger. LocalClient пишет checkpoint завершённых операций и удерживает мир до окончательного выхода. [Полный контракт](DemoRewards.md).
