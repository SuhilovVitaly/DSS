# Демо-перевозки: доска заказов, пассажиры и груз

Технический контракт перевозок демо-кампании: пассажирская вместимость носителя,
доска заказов станций, пассажирские и грузовые контракты, отмена, просрочка,
потеря носителя, учёт и сохранение. Игровое описание — в
[механике перевозок](../02-FirstRelease/Mechanics/DemoTransportContracts.md),
экран — в [Contracts](../02-FirstRelease/Screens/Contracts.md). Численные
настройки берутся из [jobs.json](../../../src/DeepSpaceSaga.Client/Data/Demo/jobs.json)
и не копируются в Client. Перевозки работают только в демо-кампании
(`StationJobsEnabled`: отпечаток демо-каталогов, cluster и jobs загружены);
в Tutorial, Default и других мирах все секции транспорта равны null.

## Авторитет и transport

Engine владеет предложениями, обязательствами, людьми на борту, опечатанным
грузом, деньгами, штрафами и долгом. Client получает только
`DemoCampaignSnapshot.Transport` (`DemoTransportSnapshot`) и отправляет две
типизированные команды кампании через session: `AcceptJobOfferCommand(OfferId)` и
`CancelTransportCommand(ContractId)`. Обе проходят общий путь
[DemoCampaign](DemoCampaign.md): command identity, expected revision, receipt,
отказ при активном бою или уничтоженном носителе. Клиент не отправляет цены,
сроки и штрафы и не угадывает доступность: действие разрешено только явным
null-вердиктом Engine (`StationJobReasons.AcceptBlocker`,
`TransportFailureReasons.CancelBlocker`); отсутствие секции или вердикта —
`unavailable`. Коды причин стабильны и локализуются (`Demo.Jobs.*`,
`Demo.Passengers.*`, `Demo.PassengerContracts.*`, `Demo.CargoContracts.*`,
`Demo.TransportFailures.*`); сырой код игроку не показывается.

Диспетчеризация в [SimulationEngine.DemoCampaign.cs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs)
явная: prepare-шаги доски, посадки, отмены и опечатывания работают на candidate-состоянии
кампании; после commit тот же вызов сажает пассажиров и применяет деньги отмены.
Общие partial-hooks кампании этими историями не заняты.

## Пассажирская вместимость

Демо New Game использует [demo-starter-carrier.json](../../../src/DeepSpaceSaga.Client/Data/Ships/demo-starter-carrier.json):
два жилых модуля по четыре каюты, семь членов экипажа, включая инженера,
и одна свободная каюта. Loader отвергает manifest с повтором ID модуля, выходом
за корпус или наложением. Tutorial и Default используют общий
[starter-carrier.json](../../../src/DeepSpaceSaga.Client/Data/Ships/starter-carrier.json).
Сохранения содержат фактически установленные модули.

`PassengerCapacitySnapshot` (Total/Crew/Passenger/Reserved/FreeCabins, PrisonerCabins) вычисляется
на каждом snapshot из исправных Living quarters, живого экипажа, пассажиров и пленных; фиксированный Reserved равен нулю; он не сохраняется. `LifeSupportReason`: `cabins_overcommitted` или
`insufficient_food_rations` — пайков в трюме меньше, чем нужно на календарные
сутки всем людям на борту и каждой свободной каюте. Свободных кают не бывает меньше
нуля. Посаженные пассажиры занимают каюты и в резервировании кают носителя.

## Доска заказов

`PublishStationJobs` на каждом календарном шаге (и на паузе New Game) удаляет
истёкшие предложения и один раз публикует текущий bucket
`GameTimeMs / limits.publicationGameTimeMs`. Каждая станция кластера с известной
ролью дополняет свою доску до `limits.offers`; слот генерируется отдельным
named RNG stream `station-jobs/v1:{station}:{bucket}:{slot}` от master seed, поэтому
одинаковый seed даёт одинаковые доски, а повторные snapshots и открытие экрана
ничего не перегенерируют. Типы `passenger`, `urgent_passenger`, `cargo` выбираются
весами `roleFilters`; `bounty` не входит в генератор транспортных предложений: боевые заказы доступны отдельной вкладкой Contracts и имеют собственный ledger.
Назначение — прямой сосед графа кластера с известным `risk`. Первая публикация
гарантирует на стартовой станции обычный пассажирский заказ (`firstOffer`) на
кратчайший маршрут. Число пассажиров и тонн ограничено проектом носителя
(каюты без экипажа и пленных; фактические пассажиры учитываются при принятии; общий трюм), а не моментально свободным местом.

`JobOffer` хранит замороженные условия: ID `jobs:{station}:{bucket}:{slot}`,
маршрут, число/массу, товар груза, целые км маршрута, риск, выплату,
длительность срока и время окончания публикации (`limits.expiryGameTimeMs`).
Выплата: `ceil((base + load + perDistanceKm × км) × urgency‰ × risk‰ / 1 000 000)`
Tokens, все множители видны в `JobPayoutTerms`.

Срок (`StationJobDeadlineMs`, `deadlines` в jobs.json):
`max(minimumGameTimeMs, ceil((maneuverGameTimeMs + ceil(ETA × cruisePermille / 1000)) × type‰ / 1000))`,
где ETA — календарное время прямого полёта по маршруту на максимальной скорости
носителя (км / Vmax × 300), а `type‰` — `ordinaryPermille`, `urgentPermille` или
`cargoPermille`. `maneuverGameTimeMs` (отстыковка, разгон, разворот, синхронизация,
стыковка) и `cruisePermille` (крейсерская скорость обычного пилота) необязательны;
без них формула вырождается в прежний масштаб ETA. Срок начинает отсчёт при
принятии; сохранённые предложения и обязательства сохраняют замороженный срок.
Для внешних маршрутов демо текущая настройка `cruisePermille=5000` оценивает
крейсерский участок как пять ETA на максимальной скорости; манёвры добавляют
32400000 календарных мс, затем применяются множители типа заказа1300/1100/1300.
Это запас срока прямого рейса, а не гарантия успеть после боя или произвольного обхода.

Принятие (`StationJobBlocker`, тот же расчёт в команде и проекции): предложение
не истекло, носитель жив и подтверждённо пристыкован к станции отправления;
для пассажиров — жизнеобеспечение и свободные каюты с учётом уже обещанных мест,
для груза — свободный трюм за вычетом опечатанного груза. Отказы:
`offer_not_found`, `offer_already_accepted`, `offer_expired`,
`not_docked_at_origin`, `no_free_cabins`, `insufficient_food_rations`,
`cabins_overcommitted`, `insufficient_cargo_space`, общие коды кампании
(`stale_revision`, `battle_active`, `carrier_destroyed`, `unavailable`,
`unsupported` для bounty). Принятие удаляет предложение и добавляет в единый журнал
`JobObligation` `contract:{offerId}` со статусом `active` и календарным сроком.

## Пассажирские и грузовые контракты

Принятие пассажирского заказа в том же commit сажает `Count` реальных пассажиров
`{contractId}:pax-{n}` в `ship.Passengers`: они занимают каюты и едят пайки, ничего
не списывается. Принятие грузового заказа опечатывает `MassKg` товара
`ItemTypeId`. Опечатанный груз — запись кампании, а не стек модуля: торговля не
может его продать, а `PlayerFreeHoldKg` (свободное место модулей минус резерв)
ограничивает quote и исполнение Buy, legacy Buy и приём новых грузовых заказов.

Доставка выполняется на календарном шаге при подтверждённой стыковке (диалог
стыковки завершён, нет боя и незавершённой стыковки) у станции назначения.
Время прибытия — завершение рейса прибытия в voyage ledger, иначе текущее время;
прибытие не позже срока выигрывает. Доставка высаживает ровно пассажиров
контракта или освобождает трюм, закрывает обязательство `completed`, зачисляет
выплату один раз (сначала гасится долг по контрактам) и увеличивает
Revision/CommitSequence кампании, что запускает автосохранение. Другие станции,
повторные snapshots, Save/Load и дубликаты команд выплату не повторяют.
Состояния проекции: пассажиры `aboard`, `overdue`, `delivered`, `failed`; груз
`aboard`, `overdue`, `delivered`, `return_pending`, `returned`, `lost`.
`CargoContractsSnapshot` также публикует разбиение трюма: вместимость, опечатано,
свои товары, свободно (`FreeHoldKg`).

## Отмена, просрочка и потеря

Каждое активное транспортное обязательство закрывается ровно один раз:
`completed` (доставка), `cancelled` (`CancelTransportCommand`), `failed` с
причиной `expired` (календарное время позже срока; фиксируется `deadline + 1`,
пауза срок не двигает, разбиение времени не меняет исход) или `carrier_lost`
(носитель уничтожен, без штрафа). Штраф отмены и просрочки —
`ceil(payout × permille / 1000)` по `penalties.cancelPermille`/`expiryPermille`.
Доступные Tokens списываются (баланс не становится отрицательным), остаток —
долг по контрактам; он гасится из следующих выплат контрактов, начиная со старого
отказа, и не блокирует полёты, торговлю и новые заказы. `TransportFailuresSnapshot`
показывает до действия штраф отмены, штраф просрочки и срок каждого активного
обязательства (`TransportCancellationTerms`), после — исходы и общий долг.
Отказы отмены: `contract_not_found`, `contract_not_active`, `not_cancellable`
(не транспортный тип), а также `battle_active`/`carrier_destroyed`/`unavailable`.

Пассажиры проваленного контракта остаются на борту (`DisembarkPending`) до
следующей подтверждённой стыковки у любой станции и сходят без оплаты; при отмене
у пристыкованной станции — сразу. В открытом космосе людей не удаляют. Груз
отменённого или просроченного контракта остаётся в трюме (`return_pending`) до
подтверждённой стыковки с человеческой станцией (базы ИИ груз не принимают), при
отмене у такой станции — сразу; возврат без выплаты. Люди и груз потерянного
носителя остаются с обломками.

## Порядок шага, учёт и ревизия

Календарный шаг кампании выполняет по порядку: публикацию досок, доставку
пассажиров, доставку груза, отказы (потеря, просрочка, высадка) и возврат груза;
каждый шаг идемпотентен. Тиковые переходы контрактов (доставка, возврат, отказы,
высадка) увеличивают Revision/CommitSequence; команда, отправленная с прежней
revision, получает локализованный `stale_revision` и проходит при повторе с
актуальным snapshot. Публикация досок ревизию не меняет.

Voyage ledger получает выплаты `passenger:{contractId}` и `cargo:{contractId}`
в колонку `PassengerPayoutCredits` и штрафы `penalty:{contractId}` в
`PassengerPenaltyCredits` (одна проводка на posting ID; выплата — только в рейс
прибытия к этой станции, штраф — в последний рейс в состоянии
InTransit/AwaitingRealization; без такого рейса деньги меняются, проводки нет). Finance подписывает эти колонки как
«Выплаты/Штрафы по контрактам» (ключи `Finance.PassengerPayout`/`PassengerPenalty`).

## Сохранение и Load

Writer остаётся Save22, версия формата и fingerprint кампании не менялись
(отпечаток покрывает только cluster.json; jobs.json — опциональный каталог).
`DemoCampaignData` получает четыре необязательных раздела schema 1:
`stationJobs` (опубликованный bucket, предложения, журнал), `passengerContracts`
(ID пассажиров, posting, квитанция доставки), `transportFailures` (причина, штраф,
оплачено/погашено/долг, высадка) и `cargoContracts` (posting, квитанции доставки и
возврата). Старое демо-сохранение без разделов публикует доску текущего дня при
Load; принятые до посадки/опечатывания legacy-обязательства без записи контракта
доставляются и оплачиваются один раз без высадки несуществующих людей. Проекции (вместимость, вердикты, состояния, разбиение
трюма) не сохраняются. Load до замены сессии атомарно отвергает повреждённые
разделы: несогласованные статусы, posting ID, суммы, пассажиров вне носителя или
на борту после высадки/доставки, неизвестные товары.
[Формат сохранений](SaveFormat.md).

## Ограничения

- Выдача груза диалоговыми эффектами (`AddCargoItem`) учитывает только вместимость
  модуля, не резерв трюма; демо-контент этот эффект не использует.
- Повреждение трюма ниже резерва не уничтожает опечатанный груз; правила
  разрушения груза принадлежат боевым эпикам.
- Срок не учитывает направление и радиус разворота: обратные короткие маршруты
  получают щедрый срок, пилот на максимальной скорости может не уложиться из-за
  разворота. Калибровка — плейтест EP-0041.
- Если один интервал snapshot пересекает срок и время приёма пищи, пайки
  списываются до обработки просрочки; отклонение ограничено интервалом snapshot.
- Наличие кода и тестов не является нативной или продуктовой приёмкой; статус
  проверок — в [Board EP-0034](../../Board/EP-0034-demo-transport-contracts/ExecutionStatus.md).

Источники: [StationJobsContracts](../../../src/DeepSpaceSaga.Contracts/StationJobsContracts.cs), [PassengerCapacityContracts](../../../src/DeepSpaceSaga.Contracts/PassengerCapacityContracts.cs), [PassengerContractsContracts](../../../src/DeepSpaceSaga.Contracts/PassengerContractsContracts.cs), [TransportFailureContracts](../../../src/DeepSpaceSaga.Contracts/TransportFailureContracts.cs), [CargoContractsContracts](../../../src/DeepSpaceSaga.Contracts/CargoContractsContracts.cs), [StationJobs](../../../src/DeepSpaceSaga.Engine/SimulationEngine.StationJobs.cs), [PassengerCapacity](../../../src/DeepSpaceSaga.Engine/SimulationEngine.PassengerCapacity.cs), [PassengerContracts](../../../src/DeepSpaceSaga.Engine/SimulationEngine.PassengerContracts.cs), [CargoContracts](../../../src/DeepSpaceSaga.Engine/SimulationEngine.CargoContracts.cs), [TransportFailure](../../../src/DeepSpaceSaga.Engine/SimulationEngine.TransportFailure.cs), [DemoCampaignData](../../../src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs), [DemoCampaignDefinitions](../../../src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs).
