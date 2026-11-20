# Board: реестр эпиков

Board — единственное место эпиков, историй и тикетов DSS. Реестр показывает,
действует ли результат каждого эпика в текущей игре и где лежит evidence.
Сверка выполнена 2026-11-16 в [EP-0031](EP-0031-board-and-demo-scope/Documentation.md)
по `title`/`stage` эпика, числу коммитов с его ID в `git log` и его собственным отчётам.

**Категория — не продуктовая приёмка.** Automated, native и owner acceptance
каждого эпика записаны только в его отчётах; реестр их не подменяет.

| Категория | Значение |
|---|---|
| Действует | Результат есть в текущем коде и описан в `DB/Documentation` |
| Частично | Часть тикетов не выполнена или отчёт фиксирует открытый остаток |
| Заменён | Результат удалён или вытеснен решением другого эпика |
| Не начат | Коммитов с ID эпика нет, отчёта об исполнении нет |
| Постоянный | Сквозной эпик исправлений |

## Эпики

| Эпик | `stage` в карточке | Коммитов | Категория | Evidence и примечание |
|---|---|---:|---|---|
| [EP-0001](EP-0001-trading-system/Documentation.md) Межстанционная торговля | draft | 76 | Частично | [ReviewStatus](EP-0001-trading-system/ReviewStatus.md): 74 тикета поставлены; [ImplementationStatus](EP-0001-trading-system/ImplementationStatus.md) перечисляет незакрытые US-0009…US-0011 |
| [EP-0002](EP-0002-procedural-solar-system/Documentation.md) Процедурная система | draft | 30 | Действует | [ImplementationStatus](EP-0002-procedural-solar-system/ImplementationStatus.md) |
| [EP-0003](EP-0003-station-clusters-and-trade-geography/Documentation.md) Кластеры и торговая география | draft | 34 | Действует | [ImplementationStatus](EP-0003-station-clusters-and-trade-geography/ImplementationStatus.md); география затем переделана EP-0018 |
| [EP-0004](EP-0004-ai-territories-and-map-environment/Documentation.md) Территории ИИ и поля | in_progress | 0 | Частично | [ImplementationStatus](EP-0004-ai-territories-and-map-environment/ImplementationStatus.md): 25 из 26 тикетов done; коммиты без ID эпика |
| [EP-0005](EP-0005-optimization/Documentation.md) Оптимизация тактической карты | draft | 0 | Не начат | Отчёта исполнения нет |
| [EP-0006](EP-0006-basic-torpedo-combat/Documentation.md) Первый торпедный бой | draft | 19 | Заменён | Бой реального времени удалён по D-USER-01 [EP-0015](EP-0015-tactical-carrier-battle/Documentation.md) |
| [EP-0007](EP-0007-countermeasure-combat/Documentation.md) Противоракеты и операторы | draft | 1 | Заменён | План не исполнялся; D-USER-01 EP-0015 |
| [EP-0008](EP-0008-player-defense-and-hostile-combat/Documentation.md) Активный противник и защита | draft | 48 | Заменён | [ImplementationStatus](EP-0008-player-defense-and-hostile-combat/ImplementationStatus.md): 44 из 45 опубликованы; затем удалено D-USER-01 EP-0015 |
| [EP-0009](EP-0009-battle/Documentation.md) Модульный бой | — | 44 | Заменён | [ImplementationStatus](EP-0009-battle/ImplementationStatus.md); D-USER-01 EP-0015 |
| [EP-0010](EP-0010-combat-reports/Documentation.md) Боевые доклады | — | 32 | Заменён | [ImplementationStatus](EP-0010-combat-reports/ImplementationStatus.md); журнал и отчёты старого боя удалены D-USER-01 EP-0015 |
| [EP-0011](EP-0011-short-fixes/Documentation.md) Короткие исправления | — | 4 | Действует | [ImplementationStatus](EP-0011-short-fixes/ImplementationStatus.md): реализация DONE |
| [EP-0012](EP-0012-combat-rebuild/Documentation.md) Ребилд боя реального времени | in_progress | 95 | Заменён | [ImplementationStatus](EP-0012-combat-rebuild/ImplementationStatus.md): 73 из 73 тикетов; затем D-USER-01 EP-0015 |
| [EP-0013](EP-0013-debug-tuning-panel/Documentation.md) Панель F12 и ручной манёвр | in-progress | 24 | Действует | [ExecutionStatus](EP-0013-debug-tuning-panel/ExecutionStatus.md); F12 входит в текущие границы релиза |
| [EP-0014](EP-0014-module-panels/Documentation.md) Панели модулей для боя по точке | implementation-complete | 21 | Заменён | [ExecutionStatus](EP-0014-module-panels/ExecutionStatus.md); боевая часть удалена D-USER-01 EP-0015 |
| [EP-0015](EP-0015-tactical-carrier-battle/Documentation.md) Тактический бой V4 | in-progress | 117 | Частично | [ExecutionStatus](EP-0015-tactical-carrier-battle/ExecutionStatus.md): бой работает; открыт расхождением одной балансной серии |
| [EP-0016](EP-0016-tactical-battle-skins/Documentation.md) Скины боя | done | 16 | Действует | [ExecutionStatus](EP-0016-tactical-battle-skins/ExecutionStatus.md) |
| [EP-0017](EP-0017-pilot-initiative/Documentation.md) Инициатива пилотов | implemented | 18 | Действует | [ExecutionStatus](EP-0017-pilot-initiative/ExecutionStatus.md); ручная приёмка открыта |
| [EP-0018](EP-0018-map-geography-and-scale/Documentation.md) География и масштаб карты | approved | 33 | Действует | [ImplementationStatus](EP-0018-map-geography-and-scale/ImplementationStatus.md): 29 из 29 тикетов |
| [EP-0019](EP-0019-tactical-battle-ui-redesign/Documentation.md) UI/UX боя | done | 27 | Действует | [ExecutionStatus](EP-0019-tactical-battle-ui-redesign/ExecutionStatus.md) |
| [EP-0020](EP-0020-tactical-battle-mechanics/Documentation.md) Механики боя | done | 47 | Действует | [ExecutionStatus](EP-0020-tactical-battle-mechanics/ExecutionStatus.md): 44 из 44 тикетов |
| [EP-0021](EP-0021-drone-combat-economy/Documentation.md) Дроны в экономике кампании | draft | 0 | Заменён | Пустой старт отменён D-01 EP-0031; части перенесены в EP-0029 и демо-эпики (пометка в карточке) |
| [EP-0022](EP-0022-tactical-battle-xenon-star/Documentation.md) Скин Xenon Star | implemented | 9 | Действует | [ExecutionStatus](EP-0022-tactical-battle-xenon-star/ExecutionStatus.md); художественная приёмка открыта |
| [EP-0023](EP-0023-pilot-card-commands/Documentation.md) Команды в карточке пилота | implemented | 16 | Действует | [ExecutionStatus](EP-0023-pilot-card-commands/ExecutionStatus.md) |
| [EP-0024](EP-0024-battle-field-redesign/Documentation.md) Поле боя Tabletop | approved | 31 | Частично | [ExecutionStatus](EP-0024-battle-field-redesign/ExecutionStatus.md): 24 из 26 тикетов |
| [EP-0025](EP-0025-modular-character-portraits/Documentation.md) Составные портреты | draft | 0 | Не начат | Отчёта исполнения нет |
| [EP-0026](EP-0026-tabletop-client-ui/Documentation.md) Интерфейс Tabletop | implemented | 11 | Действует | [ExecutionStatus](EP-0026-tabletop-client-ui/ExecutionStatus.md) |
| [EP-0027](EP-0027-tutorial-start/Documentation.md) Обучающий старт | implemented | 0 | Действует | Карточка эпика; коммиты без ID эпика |
| [EP-0028](EP-0028-battle-tutorial/Documentation.md) Обучение в бою | implemented | 28 | Действует | Карточка эпика; приёмка владельцем по чек-листу US-0006-TK-0002 открыта |
| [EP-0029](EP-0029-unified-carrier-and-torpedoes/Documentation.md) Единый носитель и торпеды | draft | 76 | Действует | US-0001…US-0013 в `git log`; исправления — `stories/story-20261115-211353-ep-0029-remediation.md`; приёмка владельцем открыта |
| [EP-0030](EP-0030-debug-console/Documentation.md) Дебаг-консоль | implemented | 20 | Действует | Карточка эпика |
| [EP-0031](EP-0031-board-and-demo-scope/Documentation.md) Board и граница демо | approved | — | Действует | Этот реестр |
| [EP-0032](EP-0032-demo-campaign-continuity/Documentation.md) Непрерывная кампания и последствия боя | approved | 32 before final documentation commit | Действует | [ExecutionStatus](EP-0032-demo-campaign-continuity/ExecutionStatus.md): 28 tickets implemented; manual acceptance deferred to owner |
| [EP-0033](EP-0033-demo-company-services/Documentation.md) Обслуживание и восстановление компании | approved | 26 | Реализован; приёмка OPEN | 6 историй, 26 тикетов; EP-0034 passenger integration и product gates OPEN; [ExecutionStatus](EP-0033-demo-company-services/ExecutionStatus.md) |
| [EP-0034](EP-0034-demo-transport-contracts/Documentation.md) Контракты пассажирских и грузовых перевозок | approved | 38 (ветка `feature/ep-0034-demo-transport-contracts`) | Реализован; приёмка OPEN | 7 историй, 27 тикетов (включая исправляющий US-0006-TK-0006); [ExecutionStatus](EP-0034-demo-transport-contracts/ExecutionStatus.md): story-тесты PASS, полный прогон — запись оркестратора, native/product OPEN; [перевозки](../Documentation/04-Engineering/DemoTransportContracts.md) |
| [EP-0035](EP-0035-demo-pirate-encounters/Documentation.md) Пиратские встречи и избегание перехвата | approved | 0 | Не начат | Демо: 17 историй, 66 тикетов; аудит грумминга: сенсоры/fog/эскадры/recovery/integration; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0036](EP-0036-demo-bounties-and-loot/Documentation.md) Боевые заказы и пообъектная добыча | approved | 48 | Реализован; приёмка OPEN | 12 историй, 48 тикетов; [ExecutionStatus](EP-0036-demo-bounties-and-loot/ExecutionStatus.md), [Tickets](EP-0036-demo-bounties-and-loot/Tickets.md); native/product — владелец |
| [EP-0037](EP-0037-demo-voyage-economy/Documentation.md) Экономика рейса и первое улучшение | approved | 0 | Не начат | Демо: 5 историй, 14 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0038](EP-0038-demo-pilot-progression/Documentation.md) Ограниченное развитие пилотов | approved | 0 | Не начат | Демо: 4 историй, 15 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0039](EP-0039-demo-cluster-story/Documentation.md) Сюжет кластера и финал демо | approved | 0 | Не начат | Демо: 6 историй, 27 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0040](EP-0040-demo-campaign-onboarding/Documentation.md) Обучение и понятность кампании | approved | 0 | Не начат | Демо: 5 историй, 9 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0041](EP-0041-demo-validation-and-release/Documentation.md) Сквозная проверка и выпуск демо | approved | 0 | Не начат | Демо: 7 историй, 17 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0042](EP-0042-postbattle-results-and-loot/Documentation.md) Результаты боя и параллельный сбор трофеев | draft | 0 | В работе | 7 историй, 22 тикета; [реестр исполнения](EP-0042-postbattle-results-and-loot/Tickets.md) |
| [EP-maintenance](EP-maintenance/Documentation.md) Исправления и доделки | ongoing | 35 | Постоянный | Завершённые тикеты — `EP-maintenance/Done` |

## Дорожная карта первой демо-версии

Текущая ревизия — [сокращённый одобренный план](EP-0031-board-and-demo-scope/DemoPlan.md). Он заменяет прежний расширенный резерв EP-0032…EP-0044. Демо: 60–120 минут, один кластер, торговля + перевозки + бой, готовый Tetrarch, настоящие потери и восстановление.

| План | Эпик | Приоритет | Истории | Внешние зависимости по историям |
|---|---|---|---:|---|
| Д1 | [EP-0031](EP-0031-board-and-demo-scope/Documentation.md), новая US-0005 | P0 | 1 новая | Существующая основа EP-0031 |
| Д2 | [EP-0032](EP-0032-demo-campaign-continuity/Documentation.md) — Непрерывная кампания и последствия боя | P0 | 7 | EP-0031 |
| Д3 | [EP-0033](EP-0033-demo-company-services/Documentation.md) — Обслуживание и восстановление компании | P0 | 6 | EP-0032, EP-0034 |
| Д4 | [EP-0034](EP-0034-demo-transport-contracts/Documentation.md) — Контракты пассажирских и грузовых перевозок | P0 | 7 | EP-0032 |
| Д5 | [EP-0035](EP-0035-demo-pirate-encounters/Documentation.md) — Пиратские встречи и избегание перехвата | P0 | 17 | EP-0032, EP-0033, EP-0034 |
| Д6 | [EP-0036](EP-0036-demo-bounties-and-loot/Documentation.md) — Боевые заказы и пообъектная добыча | P0 | 12 | EP-0034, EP-0035 |
| Д7 | [EP-0037](EP-0037-demo-voyage-economy/Documentation.md) — Экономика рейса и первое улучшение | P0 | 5 | EP-0034, EP-0032, EP-0033, EP-0036 |
| Д8 | [EP-0038](EP-0038-demo-pilot-progression/Documentation.md) — Ограниченное развитие пилотов | P1 | 4 | EP-0032, EP-0033 |
| Д9 | [EP-0039](EP-0039-demo-cluster-story/Documentation.md) — Сюжет кластера и финал демо | P1 | 6 | EP-0034, EP-0037, EP-0035, EP-0036, EP-0032 |
| Д10 | [EP-0040](EP-0040-demo-campaign-onboarding/Documentation.md) — Обучение и понятность кампании | P1 / сквозной | 5 | EP-0034, EP-0037, EP-0035, EP-0033, EP-0039, EP-0038 |
| Д11 | [EP-0041](EP-0041-demo-validation-and-release/Documentation.md) — Сквозная проверка и выпуск демо | P1 / сквозной | 7 | EP-0032, EP-0033, EP-0037, EP-0040, EP-0039, EP-0034, EP-0035, EP-0036, EP-0038 |

10 новых эпиков и 55 историй (39 исходных и 16 добавленных сверками 2026-11-16, см. [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md#сверка-покрытия-плана-2026-11-16)); ещё одна новая история US-0005 продолжает EP-0031. Тикеты не создавались. Новые эпики: approved plan / implementation not-started; детализация историй draft без выдуманного отдельного human approval. Приоритет P1 обязателен для публичной демо, не означает optional.

Порядок: контракт и непрерывность → минимальное обслуживание/найм → мирный рейс → встречи/награды → смешанный плейтест → ограниченное развитие и сюжет → выпуск. Реальные prerequisites заданы на уровне stories; сквозные EP-0040/0041 начинаются с ранних историй и не ждут финального сюжета.

Подробности: [переиспользование EP-0021/0029](EP-0031-board-and-demo-scope/DemoPlan.md#переиспользование-ep-0021-и-ep-0029), [замена номеров](EP-0031-board-and-demo-scope/DemoPlan.md#замена-предварительных-номеров), [условия приёмки](EP-0031-board-and-demo-scope/DemoPlan.md#граница-приёмки).

## Декомпозиция EP-0032…EP-0041 в тикеты

2026-11-16T20:32:46.441Z — по текущему поручению подготовлены 58 историй и 200 тикетов; добавлены EP-0032-US-0007, EP-0040-US-0005, EP-0041-US-0007. [Текущий dependency order, карта всех файлов и release gates](EP-0041-demo-validation-and-release/TicketPlan.md). Ранние формулировки «без тикетов» относятся к предыдущему этапу; текущая очередь определяется картами историй. Implementation NOT STARTED; native/audio/product OPEN.

2026-11-17 — ревью декомпозиции (поручение «пройди по всем, реши нужно ли что-то изменить и добавить. делай это автономно»):
- EP-0031-US-0005 получила два документационных тикета. Она prerequisite корневых тикетов EP-0032, но была без тикетов, и граф не мог стартовать.
- В 40 клиентских тикетах `src/DeepSpaceSaga.Client/Program.cs` заменён на `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs`: экраны и их transport создаются в `SkiaWindow.Open*Async`, а `Program.cs` только запускает окно.
- EP-0034-US-0006-TK-0004 получил `GameSessionScreen.cs` и шаг 4: без этого журнал обязательств нельзя было открыть с карты (AC-03).
- Колонка «Коммитов» для EP-0032…EP-0041 показывала число тикетов, исправлено на 0.
Подробности — в [TicketPlan](EP-0041-demo-validation-and-release/TicketPlan.md#ревью-декомпозиции-2026-11-17).

## D-35-21: один автоматический доклад на группу за кампанию
Пользователь: «Да, один автоматический доклад на группу за кампанию».
Сохранить consumedAutomaticReport по campaignId/stable groupId. Только первое реальное sensor acquisition создаёт modal и atomic Speed0; dismissal сохраняет pause. Recontact, повтор snapshot, Save/Load, debug fog reveal не открывают modal и не меняют speed. Эскадра — одна группа, не два независимых автоматических доклада. Flag и report creation сохраняются атомарно, не терять pending report при Load.
G-35-04 CLOSED для once/group и batching D-35-33; только точный UX-order/manual reopen — A. Немодальное уведомление при recontact тоже не обязательное D.
Tests NOT RUN: FirstGroupContact_PausesOnce; Recontact_NoModalNoPause; SaveLoad_ConsumedPersists; TwoSquadMembers_OneGroupReport; FogReveal_NoDiscovery.
## Текущее расширение эскадр — D-35-21…26
D-35-22: одна фишка группы, Vmax=min operational Vmax её носителей.
D-35-23: все носители и дроны эскадры одновременно на одном поле.
D-35-24: шанс эскадры10%, меняется debug console. Предлагаемый синтаксис pirate_squad_chance [0..100], без аргумента current; help/валидация обязательны.
D-35-25 supersedes раннее2–3/mixed DEMO: «так по поводу боя. пока эскадры только два логова и оба выводим в бой». Ровно два Логова, по6 дронов =12, плюс носитель игрока (всего3 carriers на поле). Танкред остаётся одиночным типом. Без повреждений squad Vmax=1.10 frozen start player maximum, operational min сохраняется.
D-35-26: «при уничтожении всех носителей дроны получают статус выведены из строя как при стрельбе». Гибель первого из двух не отключает дроны. После последнего носителя стороны все её surviving drones становятся существующим Disabled, speed0; Destroyed остаётся Destroyed, отключение не означает смерть пилота. Симметрично для player и pirate side. Atomic/idempotent transition, затем outcome и skip последующих shots по existing invariants.
Grounding: TacticalBattleState.cs:9 TacticalDroneStatus Air/Disabled/Destroyed; TacticalBattleSalvo.cs:237–239 shot disable задаёт Disabled,Speed0,statistics; :152–153 skip disabled/destroyed shooter.
A-35-11: независимый seeded draw на каждое создание новой группы,90% singleton/10% squad; chance действует только future creation, не sensor/load; RNG/result сохраняются без reroll.
G-35-12 CLOSED D-35-32: одна точка группы, max operational radius без сложения; report layout — A.
G-35-13 lifetime CLOSED D-35-40; ordinary group count4 CLOSED D-35-36; singleton weights CLOSED D-35-35; initial/norespawn CLOSED D-35-34.
G-35-14 CLOSED D-35-26; orphan policy больше не открыта.
G-35-15 normal retreat CLOSED/excluded D-35-46: exit onlywin/loss; crash recovery CLOSED D-35-47.
Ранние2–3/mixed варианты, выбор2vs3 и orphan proposals — superseded; не scope демо и не текущие G.
Текущий объём EP-0035: **17 историй / 66 тикетов**. Добавлены US0012 (5),US0013 (4),US0014 (3),US0015 (4). Исторические11/43 выше сохранены как журнал; текущая карта supersedes. US0012 после US1/11; US13 после US12/3; US14 после US13; US15 после US14/4; US6 documentation последняя. Board only, implementation NOT STARTED.
## D-35-27: независимые классы дронов 50/50
Пользователь: «Давай пока независимые 50/50 без ограничений. Добавляй в тикеты».
Для КАЖДОГО drone slot отдельный независимый seeded Bernoulli: light=0.5, heavy=0.5. Нет cap, минимумов или гарантии смешанного крыла; полностью light и полностью heavy допустимы. Actual totals не случайны: Логово6,Танкред10,эскадра2xЛогово12 (6 у каждого stable carrierId).
G-35-07 CLOSED в части class weights/ограничений. Equipment/layout pool CLOSED D-35-39 (четыре exact layouts по25%); existing light frame используется по D-35-38; G-35-08 CLOSED. Эти параметры не утверждены вероятностью50/50. Starter bounds закрыты D-35-28; progression/другие уровни — остаток G-35-05.
A-35-06 сохраняется как рекомендация: материализация один раз на реальном player sensor acquisition, сохранение результата/no reroll при recontact/Load. D о50/50 не переименовывает эту A в пользовательское одобрение persistence policy. Использовать именованный seeded composition RNG stream кампании, не UI/global RNG; каждый слот получает draw и сохраняет origin carrier identity.
Content: явно задать0.5/0.5 weights и отсутствие forced-mix/cap; schema validate finite normalized weights. Runtime не использовать общий draw на корабль/эскадру.
Tests NOT RUN: FixedSeed_ReproducesSlotClasses; EachSlot_ConsumesIndependentDraw; AllLight_Allowed; AllHeavy_Allowed; Totals6_10_AndTwoTimes6Preserved; SquadDrones_KeepCarrierOwnership; SaveLoad_NoCompositionReroll (A-35-06). Проверки deterministic seeds/controlled RNG, без flaky статистических частот.
## D-35-28: точность стартового доклада Ирины
Пользователь: «Да, принимаем ±1 дрон на носитель и ±2 для эскадры. Добавляй в тикеты».
Стартовый sensor skill Ирины даёт одно целое приблизительное reported total: ошибка относительно actual total не более1 для одиночного носителя, не более2 для demo squad2xЛогово. В докладе «около N», точный тип носителя / типы обоих Логов; light/heavy классы и equipment скрыты. Не показывать симметричный диапазон вокруг actual total, из которого восстанавливается скрытый true count.
Допустимые значения: одиночное Логово5..7 при actual6; Танкред9..11 при actual10; эскадра10..14 при actual12. Это границы допустимого одного числа, НЕ UI-диапазон и НЕ случайное actual число дронов.
G-35-05 CLOSED для starter Irina accuracy bounds и формата одного приблизительного числа. Открытый остаток: прогрессия навыка/другие уровни/другие операторы; distribution ошибки не утверждён пользователем. A-35-12: равномерная целочисленная ошибка в допустимых границах, независимый named estimate RNG stream; если принята при реализации, явно сохранить как assumption. Стабильная сохранённая оценка/no reroll — рекомендованная A, не новое пользовательское D.
+10% дальности от module base — отдельный эффект навыка; не трактовать как10% точности. Module радиусы200/240/264км и capture20км без изменений. Squad report один на группу, approximate aggregate total±2, не два самостоятельных forced reports.
G-35-03 CLOSED D-35-29: до20км по текущим observed vectors, real duration при1×; историческое обсуждение цели/часов superseded.
Tests NOT RUN: IrinaSingleEstimate_WithinOne_Integer; IrinaSquadEstimate_WithinTwo_Integer; Report_OneApproximateNumberAndExactTypes; Report_NoClassOrHiddenLoadout; AccuracyDoesNotAlterActual6_10_12; RangeBonusIsNotAccuracyPercent; StableEstimateOnLoad (A). Проверять разрешённый observation DTO, не переносить private composition в client.
## D-35-29: ETA до20км по текущим курсам при1×
Пользователь: «Да, принимаем до 20 км по текущим курсам и время при 1×».
ETA — earliest nonnegative время входа в distance<=20км при постоянных НАБЛЮДАЕМЫХ текущих world velocities двух объектов. r=observed target position-player position; v=observed target velocity-player velocity; найти минимальный t>=0 с |r+v*t|<=200world units, motion units согласовать. Не использовать будущие waypoint/orders, скрытый курс, мгновенный поворот/ускорение pursuit. Поле evaluation timestamp привязано к наблюдению, UI не пересчитывает скрытые данные.
Уже внутри/на20км =>0 («на дистанции боя»). Нет пересечения/расхождение => «при текущих курсах сближения до дистанции боя нет», не infinity/NaN/выдуманное время. Zero relative speed снаружи=>нет прогноза; tangent включается; numerical near-zero/discriminant обработать устойчиво, результат finite/nonnegative.
Показывать ожидаемое реальное время ожидания при игре1×, явно пометить «при1×». Grounding EngineRequirements.md:21–31: на1× одна motion second проходит за4 real seconds; поэтому display duration=4*t_motion. Calendar factor300 здесь НЕ используется. Current pause/speed не меняет этот прогноз; pause не создаёт division by zero.
G-35-03 CLOSED: цель, формула и шкала согласованы. US0010 contracts фиксирует nullable forecast/reason/observation timestamp, telemetry считает authoritative value, dialogue content/client отображает готовое значение и единицы.
Tests NOT RUN: Closing_EntryTo20km; Tangent_Included; AlreadyInside_Zero; Diverging_NoForecast; ZeroRelativeVelocity_InsideOutside; NearZero_Finite; FirstEntry_NotClosestApproachTime; MotionSecond_DisplaysFourRealSecondsAt1x; PauseAndCurrentSpeed_DoNotChange1xForecast; FutureOrdersNotRead.
## D-35-30: скрипт вместо недоступной Ирины
Пользователь: «Вместо нее работает скрипт с 80% эффективностью».
D: если Ирина недоступна, работу выполняет скрипт с эффективностью80%. D-35-31 утвердил192км и±2/±4; G-35-06 остаётся только для unavailable/замены/module gate.
A-35-13 SUPERSEDED D-35-31: fallback192км=0.8*240км base, а не0.8*264км.
A-35-14: unavailable=отсутствует/не назначена/недееспособна по существующим crew состояниям; конкретную проверку согласовать с реальными supported statuses, не создавать фиктивный health API. Неработающий sensor module отдельно: скрипт не ремонтирует и не обходит physical module gate.
Нет фиктивной реплики/портрета Ирины: системный sender для script. A-35-15: сохраняются один automatic report/group и pause/dismissal policy; fallback/replacement не сбрасывает discovery/consumed flag. Способ presentation пока предложен как A.
G-35-06F CLOSED D-35-31: script±2 singleton/±4 squad; Irina±1/±2 сохраняется.
## D-35-31: численные параметры скрипта
Пользователь: «Да, принимаем для скрипта 192 км и погрешность ±2/±4».
Fallback radius=192км=1920world units=0.8*240км BASE установленного стартового player sensor; не0.8*264км с бонусом Ирины. A-35-13 pending superseded этим D.
Script report содержит одно целое «около N»: actual total±2 для singleton, actual aggregate total±4 для squad2xЛогово. Допустимые single values Логово4..8,Танкред8..12; squad8..16. Это НЕ отображаемые диапазоны и не изменение actual total6/10/12. Точные типы раскрываются, light/heavy/equipment остаются скрыты.
G-35-06F numerical accuracy CLOSED; G-35-06 radius base CLOSED. Starter Irina264км и±1/±2 сохраняются. ETA D-35-29 вычисляется по observed vectors до20км и отображается при1×;80% не искажает пеленг/дальность/ETA автоматически.
A-35-14 criteria unavailable/module gate и A-35-15 system speaker/once-report pause продолжают быть A: пользователь сейчас подтвердил только radius/error. Script не изображается Ириной; конкретная недоступность/замена и physical module behavior требуют согласования существующих crew/module states.
Distribution ошибки и stable sampled estimate/no reroll — прежние A, не новые D.
Tests NOT RUN: FallbackRadius1920WorldUnits; FallbackDoesNotMultiplyIrina264; SingleErrorWithinTwo; SquadErrorWithinFour; EstimateIntegerOnlyAndNoHiddenClasses; FallbackDoesNotDistortEta; SwitchingOperator_DoesNotChangeActualLoadout.
## D-35-32: общая точка и лучший действующий сенсор эскадры
Пользователь: «Да, принимаем одну общую точку эскадры и лучший действующий сенсор без сложения радиусов».
Одна authoritative group position — центр map token. Её используют world range, bearing, ETA по D-35-29 и capture<=20км по D-35-19; tactical позиции отдельных carrier не становятся отдельными map contacts. Все расчёты в world units, zoom/DPI/window не меняют distance.
Squad sensor range=max operational radius её carriers, НЕ сумма. Два исправных Логова=>200км, один неработающий sensor=>200км от другого; оба неработающие=>0. Нулевая дальность означает отсутствие detection coverage, а не detection при coincident point. Не суммировать площадь/радиусы и не создавать дополнительный sensor origin для tactical member positions.
G-35-12 geometry/reference/sensor aggregation CLOSED. A-report-shape: один отчёт группы с двумя точными типами и одним approximate aggregate total; два automatic окна по членам недопустимы вследствие D-35-21 one/group. UI layout списка членов — implementation choice.
G-35-04 simultaneous batching CLOSED D-35-33: одна сводка всех новых групп текущего observation step; сортировка/selection/scroll — A.
Tests NOT RUN: TwoLairs_Max200NotSum400; OneSensorDisabled_UsesOther; AllSensorsDisabled_NoCoverage; GroupReference_SharedByRangeBearingEtaCapture; MemberTacticalPositions_NotMapContacts; CaptureAndSensorIndependentOfZoom.
## D-35-33: одна сводка новых групп за один шаг
Пользователь: «Да, одна сводка для всех новых групп за один шаг симуляции».
В одном authoritative observation/simulation step собрать ВСЕ первые реальные sensor acquisitions ещё не reported stable groups, создать один batch report и одну autopause. Не останавливать обработку шага после первой группы; не продвигать дополнительные ticks ради ожидания новых контактов. Squad2xЛогово — одна группа. Debug visibility и уже consumed groups не входят в batch.
Persist batchId/content/pending presentation и consumed flags всех включённых groupIds атомарно: Save/Load не теряет записи и не создаёт повторный modal. Pending batch после Load доступен ровно один раз; flags не должны молча поглотить ещё не показанный доклад. Закрытие сводки сохраняет Speed0; новые контакты другого шага после explicit resume образуют новую сводку только для новых groups.
G-35-04 simultaneous batching CLOSED. Детерминированный порядок обязателен для replay; A-order: ETA затем range затем stable groupId; конкретный сортировочный UX/selection/scroll не является отдельным D. Manual reopening/recontact notice остаются необязательными A/отложенными, не повод повторять automatic modal.
US0010 contracts: collection group reports+batch identity; telemetry: collect/commit once after full current observation step; dialogue: один batch speaker/report и stay-paused; content/client: список новых групп без раскрытия hidden composition. Owning save хранит pending batch целиком.
Tests NOT RUN: TwoNewGroupsOneStep_OneSummaryOnePause; SquadIsOneGroup; ConsumedGroupExcluded; DifferentResumedSteps_SeparateNewSummaries; PendingBatchLoad_NoLossNoDuplicate; ClosingBatch_StaysPaused; NoExtraTicksForBatch; FogReveal_NoBatch.
## D-35-34: все группы на старте, без возрождения
Пользователь: «Да, все группы создаются на старте кампании, без возрождения в демо».
Ordinary pirate world groups материализуются seeded при New Game: stable group/member IDs, типы, position и patrol route существуют и движутся до player observation. Sensor detection не создаёт world group; он отдельно материализует скрытую light/heavy composition по D-35-13/27 и формирует доклад. Не путать эти моменты RNG.
В демо нет respawn. Save/Load/revisit/уничтожение группы не создают новые группы/копии. Initial group draw использует squad chance10% (или допустимый override при старте); уже созданные группы не меняются после console command. G-35-13 initial cadence CLOSED; lifetime/pre-NewGame применения debug override остаётся открытым. Conditional singleton50/50 утверждено D-35-35; default45/45/10.
Region/safe routes исходного US1 — A-baseline. «Два обычных состава» НЕ количество world groups: ordinary group count теперь4 по D-35-36. Проверить safe start station separation/route bypass. Position/route не скрытое знание player или pirate AI; fog off не acquisition.
Deferred EP39 integration note (не blocker EP35): EP39-US0003-TK0003:52 планирует story-gated captain; пользователь вынес сюжет из текущего грумминга. EP39 не изменён; варианты появления не утверждены.
Tests NOT RUN: NewCampaign_AllOrdinaryGroupsExistBeforeObservation; UndetectedGroups_MoveOnPatrol; FirstAcquisitionDoesNotSpawnWorldGroup; FirstAcquisition_MaterializesOnlyHiddenComposition; DestroyedGroup_NoRespawn; LoadRevisit_NoDuplicates; ConsoleChange_DoesNotMutateExistingGroups; SameCampaignSeed_ReproducesInitialGroups.
## D-35-35: распределение45/45/10
Пользователь: «Да, принимаем45/45/10».
Default initial world group draw: singleton Логово45%, singleton Танкред45%, squad2xЛогово10%. При изменении squadChance=p условный выбор singleton остаётся50/50, поэтому вероятности (1-p)/2,(1-p)/2,p. Речь о независимом seeded выборе для каждой группы при старте кампании, не о точной квоте групп.
p=0 =>50/50/0; p=1 =>0/0/100%. Не повторять draw на sensor acquisition, Load/revisit. Сохранять stable chosen group/member identities; separate hidden light/heavy slot draws50/50 при первом acquisition не меняют carrier type.
G-35-13 singleton weights CLOSED. Lifetime console override CLOSED D-35-40; ordinary world group count4 утверждён D-35-36. Фраза старого плана «два обычных состава» означает два типа/набора, не одобренное число spawn groups; не подставлять автоматически две группы.
Tests NOT RUN: Defaults45_45_10; ConditionalSingletonHalfForChangedP; ChanceZero_NoSquads; ChanceHundred_OnlyTwoLairSquads; FixedSeed_KnownChoiceSequence; LoadRevisit_NoTypeReroll; IndependentGroups_NotExactQuota. Без flaky frequency tests.
## D-35-36: четыре ordinary группы на демо-кластер
Пользователь: «Да, берём четыре обычные группы на демо-кластер. Добавляй в тикеты».
Создать ровно4 ordinary world groups при старте кампании. Каждая независимо выбирается по D-35-35 (45/45/10 default); squad2xЛогово считается ОДНОЙ группой, а не двумя. Поэтому четыре группы не означает четыре carrier и не задаёт квоту типов. No respawn, Load/revisit не увеличивают число initial instances.
G-35-13 total ordinary group count CLOSED. A-35-16: named finale captain — отдельная сюжетная встреча EP39, не входит в4 ordinary; его initial existence/story activation deferred EP39, вне EP35 grooming; не утверждать пятую группу или способ появления.
Количество станций/география и ограничение выбора маршрутов пользователем пока обсуждаются; нового D о сокращении станций нет. Safe initial locations/не на стартовой станции/доступный обход остаются прежними A до конкретной балансировки.
Tests NOT RUN: NewCampaign_ExactlyFourOrdinaryGroups; SquadCountsAsOneGroup; FourIndependentTypeDraws_NoQuota; LoadRevisit_NoAdditionalGroups; DefeatedGroup_NotRespawned; SafeInitialPositions_RespectConfiguredRegion.
## D-35-37: player fog включён, owner debug обзор выключает fog
Пользователь: «туман войны для игрока будет включен. я пока хочу видеть без тумана войны. Давай в этом эпике не будем обсудать сюжет».
Normal player demo default FogOfWarEnabled=true. Owner/developer использует явный debug override false через console для обзора карты. Прежние D-35-03 universal defaultfalse и A-35-02 resetfalse SUPERSEDED; unconditional false при Load/NewGame запрещён. Без явного override normal/legacy/new session safe default=true.
No-fog меняет только разрешённую debug projection, не sensor acquisition/AI knowledge/pursuit/доклады/генерацию loadout. Returntrue скрывает out-of-sensor live positions/tooltip/hit targets. Console показывает actual value/override status.
G-35-17 CLOSED D-35-40: app process memory, survives NewGame/Load, discarded on exit; defaults true/10.
Сюжет и named captain вне текущего EP35 grooming. Прежний G-35-16 перенесён в deferred integration note EP39, НЕ обязательный blocker EP35; EP39 не изменён. Не утверждено, что captain является пятой группой, появляется заранее или является исключением. Текущее D остаётся4 ORDINARY группы на старте/no respawn.
Tests NOT RUN: NormalNewGame_FogTrue; ExplicitDebugFalse_RevealsOnlyProjection; ReturnTrue_HidesUndetectedLiveContacts; LegacyAndNewSession_DefaultTrueWithoutOverride; FogOverride_NoSensorEventsOrAiKnowledge; OverrideLifetime_AccordingToResolvedG35_17.
## D-35-38: только лёгкие и тяжёлые дроны
Пользователь: «оставим только тяжелых и легких».
Текущий pool классов light/heavy; прежнее medium/heavy SUPERSEDED. Сохраняются independent50/50 per slot, отсутствие caps/forced mix, actual totals6/10/12, скрытый composition на first actual sensor acquisition. Оператор не раскрывает class distribution.
G-35-08 CLOSED: новый medium frame не нужен. Использовать existing Frames/light.json и Frames/heavy.json. Grounding layouts.json:3,16 — heavy sword/spear с2 shields; :29,41 — light sword/spear с1 shield. Equipment/layout IDs и weights теперь CLOSED D-35-39: два existing sword/spear варианта каждого класса,50/50 equipment.
Stable ticket EP-0035-US-0011-TK-0003-medium-content сохраняет исторический slug/ID, но теперь владеет существующим light/heavy layout pool; medium frame creation удалена из allowlist/steps.
## D-35-39: четыре существующих layouts, по25%
Пользователь подтвердил предложенный pool: «да берем. дальше».
Каждый drone slot независимо: class light/heavy50/50, затем sword/spear50/50. Ровно четыре разрешённых ID с marginal probability25% каждый: light.sword.shield,light.spear,heavy.sword.shield,heavy.spear.shield. Нет forced mix, caps или квот на класс/оружие; actual totals6/10/12 неизменны.
Оборудование брать из существующего layouts.json: gun.sword.t1/gun.spear.t1,plate.medium,shield.t1,два drive.standard,decoy.t1; light один щит,heavy два. Никаких новых frames/modules/layout вариантов. Grounding Client/Data/TacticalBattle/layouts.json:2–53. Existing light.spear ID сохраняется несмотря на отсутствие слова shield в имени.
G-35-07 equipment pool/weights CLOSED D-35-39; G-35-08 CLOSED D-35-38. Schema/content валидируют exact IDs и ссылки, runtime использует seeded draws class+equipment per slot, сохраняет per-carrier origin. Hidden loadout не попадает в сенсорный доклад; actual materialization timing/persistence прежние D/A.
Tests NOT RUN: ExactFourAllowedLayoutIds; ClassThenWeapon_IndependentDeterministicDraws; EachSlotUsesItsOwnDraws; LightOneShield_HeavyTwoShields; ExistingEquipmentReferencesValid; AllSameLayoutAllowed; Totals6_10_12Unchanged; PublicReport_NoLayoutDisclosure. Проверять deterministic RNG mapping, не случайные observed frequency.
Debug override lifetime CLOSED D-35-40.
## D-35-40…42: overrides, оператор и состоявшийся перехват
D-35-40 пользователь: «Да, настройки действуют до закрытия приложения, между запусками не сохраняются».
Host хранит explicit overrides только в app process memory: fog немедленно и после NewGame/Load; chance только следующий NewGame. Console main menu read/set без session допустим. Restart=>fogtrue/chance10; saves/profiles не содержат overrides. Failed/stale active application не выдаётся за effective success. Options применяются до первого engine tick/snapshot. G-35-13 lifetime и G-35-17 CLOSED.
D-35-41 пользователь: «Да, принимаем по наличию в экипаже и назначению, без смен и усталости».
Irina activecrew+sensor assignment=>264км,±1/±2; absent/unassigned=>script192км,±2/±4. Broken/disabled sensor=>0 coverage независимо от operator/script. No shifts/fatigue/injury schema. Reassignment восстанавливает бонус, не сбрасывает consumed groups; новые контакты расширенного радиуса идут через standard batch/autopause. G-35-06 CLOSED.
D-35-42 пользователь: «Да, принимаем: если перехват состоялся, после доклада и паузы начинается неизбежный бой».
При реальном pirate sensor contact и distance<=20км authoritative atomic capture фиксирует encounter. Player detection alone не capture. Для новой группы report+pause по batch policy; dismissal сохраняет Speed0; explicitcontinue начинает бой. Уже captured нельзя отменить сменой курса на паузе. Previously reported group не создаёт повторный automatic modal; используется существующий battle transition. Save/Load сохраняет pending capture, не даёт free escape. G-35-09 normal single-group inside-capture CLOSED; остаток: simultaneous capture CLOSED D-35-45: select earliest, no merge; recovery CLOSED D-35-47.
Tests NOT RUN: MainMenuOverrides_NoSession; OverridesSurviveNewGameLoadUntilExit; Restart_NormalDefaults; Save_NoOverrides; Chance_ExistingGroupsUnchanged; IrinaAbsentUnassigned_Fallback; AssignedIrina_Bonus; BrokenSensor_NoDetection; Reassignment_NoDuplicateReport; NewlyRevealedGroup_NormalBatch; CaptureThenReport_StaysPaused; ExplicitContinue_StartsUnavoidableBattle; PausedCourse_CannotUndoCapture; LoadPendingCapture_NoEscape; PlayerDetectionOnly_NoCapture.
Placement: all human station spawn400км A-35-17, START hard800км D-35-43/A-35-19; конкретные patrol routes ещё балансируются.
## A-35-17: делегированный выбор exclusion400км
Пользователь: «я думаю что пираты не могут быть заспавнены в определенных зонах от станций. выбери разумное значение».
Пользователь делегировал численный выбор; выбранный baseline400км=4000world units вокруг КАЖДОЙ человеческой станции demo, включая стартовую. Это A-35-17 исторический делегированный выбор, затем explicit D-35-44 подтвердил400. Обоснование: максимальный стартовый player sensor264км плюс136км separation margin; pirate sensor200км тоже меньше400. Это не гарантия безопасного рейса.
При initial generation4ordinary groups: для каждой общей group point и каждой станции distance>=400км; distance<400 запрещён, boundary400 допустима. Squad2xЛогово — одна world point. Использовать station positions на момент генерации, не runtime repulsion.
Другие stations: patrol/pursuit могут входить в400км spawn exclusion. START station — исключение D-35-43/A-35-19: hard800км no-entry и pursuit abort при safe player entry.
Seeded generator проверяет все станции; bounded retry затем понятная generation failure без partial world. Никогда молча уменьшать exclusion для размещения4groups. Сохраняются initial-only/no respawn и45/45/10.
A-35-18 proposed: если actual player initial position не совпадает со стартовой станцией, дополнительный clearance>264км от игрока; это отдельная рекомендация, не утверждённое400км правило для player. При таком scenario mismatch сначала явно разрешить G start clearance, не выдать unseen старт за доказанный.
Tests NOT RUN: AllFourGroups_OutsideEveryHumanStation400km; Boundary400_Allowed; Inside400_Rejected; SquadUsesCommonPoint; FixedSeed_ReproduciblePositions; ImpossibleLayout_FailsAtomically; ExclusionNotRelaxed; OtherStationPatrolMayEnter_StartSafezoneProtected.
Safe route/patrol balance отдельно; hard no-entry800км только START station по D-35-43, остальные stations spawn-only400.
## D-35-43 / A-35-19: hard safezone стартовой станции800км
Пользователь: «Принимаем безопасную зону300км вокруг стартовой станции, в которую пираты не заходят даже при погоне? Да. Может быть даже800. Максимум потом исправим».
D-35-43: hard safezone START station, действует patrol/pursuit. A-35-19 исторический tuning800км; число затем явно подтверждено D-35-44,8000world units.
Остальные человеческие станции сохраняют только initial spawn exclusion400км A-35-17, НЕ runtime safezone. Start initial spawn minimum=max(400,800)=800км. Config хранит отдельные spawnExclusionRadius и startSafezoneRadius; изменение балансировки не требует новой механики.
Pirate group center всегда distance>=800км от start station. Player distance<=800км считается safe: никакого нового capture внутри/на boundary, даже если другая distance<=20км. При входе игрока в safezone текущая pursuit прекращается и pirates возвращаются patrol; это не телепорт и не мгновенное изменение физических скоростей. Уже committed capture D-35-42 не отменяется последующим приказом/телепортом; после capture ordinary movement не продолжается до разрешения боя.
No-entry проверяет actual swept motion segment целого шага, не только waypoint/endpoints; маршрут/поворот/торможение планировать заранее с действующими motion constraints. Нельзя пропустить пересечение круга большим dt, clamp/teleport корабль обратно или остановить инерцию мгновенно. Для невозможного patrol path нужен validated обход либо явный отказ content; радиус не уменьшать молча.
Start zone привязана к stable startstation identity; для текущего stationary demo используется её world position. Если станция станет подвижной — отдельное будущие правило, не вводить repulsion. Station turrets не входят в задачу.
Supersedes прежнее A-35-17 утверждение «нет hard safezone»: теперь исключение START station800. Для других stations это утверждение сохраняется: только initial400, позднее patrol/chase могут входить.
Tests NOT RUN: StartSpawnOutside800; OtherStationSpawnOutside400; PatrolSweptSegment_NeverEntersStart800; PursuitSweptSegment_NeverEntersStart800; SafePlayerEntry_AbortPursuitReturnPatrol; Boundary800_PlayerSafePirateNotInside; OtherStation_NoRuntimeSafehaven; CommittedCapture_CannotBeUndone; ConfigurableRadii; ImpossiblePlacementOrRoute_FailsWithoutRelaxation.
Owning US1 content/runtime validates initial placement/route; US2 motion/pursuit enforces segment/state; US4 save preserves safe return/pending capture without freeescape. Existing one-layer/max5file allowlists unchanged; при motion-owner drift сначала корректировать Board scope.
## D-35-44: явно утверждённые радиусы800/400
Пользователь: «Да, оставляем800км у стартовой и400км для спавна у остальных станций. Добавляй в тикеты».
Численные значения теперь explicit D, supersede provisional/delegated A-35-17/A-35-19: START station hard800км (spawn/patrol/pursuit no-entry); остальные человеческие станции initial spawn exclusion400км, без runtime защиты. Оба значения content parameters. Boundary/segment/safe-entry/committed capture invariants D-35-43 сохраняются.
G-35-18 CLOSED D-35-45: earliest continuous capture, deterministic tie, последовательные бои без merge; world frozen then paused, resume без immunity.
Остаток G-35-09/15: abnormal/partial battle outcomes и корректное world continuation без новой механики тактического отступления. Normal single capture/report/continue уже D-35-42.
A-35-18 стартовая позиция игрока отдельно от станции требует проверки actual scenario; отдельный clearance не утверждён. При обычном старте у start station800 уже покрывает sensor separation.
Прочие продуктовые decisions (radii,skill/ETA,equipment,spawn count/weights/norespawn,operator,fog,lifetime,batching) закрыты. Routine module physical stats/конкретные patrol waypoint coordinates остаются implementation/balance деталями; narrative deferred внеscope.
## D-35-45/46: последовательные бои и только победа/поражение
Пользователь: «Да, принимаем последовательные бои без присоединения соседних групп. Добавляй в тикеты. Давай теперь разберём выход из незавершённого боя, загрузку и сохранение последствий. Из боя выйти можно проиграв или победив».
D-35-45: несколько разных groups пересекают capture threshold=>выбрать earliest continuous crossing event; exact tie решается детерминированно по stable groupId (технический deterministic tiebreak). Только выбранная группа входит в бой, максимум2enemy carriers при squad2Логово; соседи не присоединяются/не подкрепляют. World motion frozen в бою. После результата world остаётся paused; explicitresume может позволить другой группе захватить игрока по обычным условиям. Нет postwin immunity.
G-35-18 CLOSED. Не путать arbitration с batch first-discovery: одна сводка шага может перечислить несколько новых групп, но только selected captured group участвует в текущем бою.
D-35-46: normal EP35 campaign battle заканчивается только win/loss. Нет retreat/abort/обычного выхода или загрузки другого мира, позволяющих уйти из ongoing battle. UI/command guard authoritative, не только скрытая кнопка. Debug/tutorial exit_battle существующие semantics — отдельный явный context; не разрешать ему незаметно обходить normal campaign capture и не менять tutorial tooling без owner scope.
G-35-19 CLOSED D-35-47: durable pending same-battle restart; no perturn resume, no freeworld.
Grounding LocalGameSessionConnection.cs:145–180: accepted settlement receipt cached by BattleId, повтор retry только continuation write; SortieSettlement.cs:17 применяет outcome до clear session. Переиспользовать existing exactly-once consequences; не считать Save failure повторной наградой.
Tests NOT RUN: EarliestContinuousCapture_WinsArbitration; ExactTie_StableGroupId; NeighborGroup_NotMerged; Battle_FreezesWorldMotion; ReturnWorld_Paused; Resume_AnotherCaptureAllowedNoImmunity; OngoingCampaignBattle_RejectNormalExitAndEscapeLoad; TutorialDebugContext_ExplicitSeparation; SettlementReplay_ExactlyOnce.
## D-35-47: восстановление captured боя с начала
Пользователь: «Да, принимаем восстановление того же боя с начала после закрытия приложения или сбоя».
Durable prebattle pending checkpoint ДО tactical start: committed capture, fixed group/member/drone composition, exact player/company prebattle state, stable encounter/battleId и replay seed. Continue после close/crash запускает тот же бой с начала, не navigable world. Ordinary midbattle save/load/menu retreat запрещены; perturn resume не вводится.
Win/loss settlement exactly once, затем durable finished checkpoint до map/end. Failed write удерживает pending result/retry storage, не restart/reapply. Crash при atomic persistence даёт либо old durable pending battle replay, либо durable finished state. Intermediate damage persistence не обещается.
G-35-19 CLOSED. US0016 owns engine checkpoint, local-client durable storage и client recovery. Legacy aborted settlement не используется для ordinary EP35 unfinished battle; debug/tutorial context отдельно.
Tests NOT RUN: checkpoint-before-start; same composition/playerstate/seed replay; no navigable escape; failedwrite retryonly; atomicoldpending-orfinished; exactlyonce rewards/losses; no pending loss via autosave.
## D-35-48: ничья считается поражением игрока
Пользователь: «ничья равно поражению. игрок проиграл».
Existing Draw, включая NoCombatCapability и simultaneousdefeat, отображается/учитывается как player defeat для EP35 campaign outcome. Сохранить actual hull/ammo/disabled/destroyed/pilot state; не фабриковать hull0/смерть пилотов ради label. Победные награды не выдавать; exactly-once settlement/durable boundary D-35-47 сохраняются. Поражение боя само по себе не вводит новый gameover при живом player carrier; existing destroyed-player terminal semantics сохраняются.
G-35-20 Draw CLOSED D-35-48; G-35-20T Timeout CLOSED D-35-49. Interactive SeriesTurnCap также player defeat.
Tests NOT RUN: DrawNoCombatCapability_PlayerDefeat; SimultaneousDefeat_PlayerDefeat; DrawDoesNotFabricateDamageOrPilotDeath; Defeat_NoVictoryReward; OutcomeReceiptIdempotent. Timeout policy CLOSED D-35-49; проверки только запланированы, не PASS реализации.
## D-35-49: истечение лимита ходов — поражение игрока
Пользователь ответил «да» на вопрос «Истечение лимита ходов тоже считаем поражением?».
EP35 campaign outcome/UI считает existing Timeout/SeriesTurnCap поражением игрока, как Draw по D-35-48. Сохранить actual hull/ammo/disabled/destroyed/pilot state; не фабриковать уничтожение носителя/дронов или смерть пилотов. Применить штатные defeat consequences exactlyonce, без victory rewards, затем durable finished checkpoint до map/end по D-35-47. Сам label defeat не вводит новый gameover при живом carrier.
G-35-20T CLOSED. Продуктовый грумминг EP35 завершён; технические A/allowlist/schema drift проверяет owning implementation, это не новые user blockers. Implementation NOT STARTED, tests/build/native/product acceptance NOT RUN/OPEN.
Tests NOT RUN: SeriesTurnCap_PlayerDefeat; TimeoutPreservesActualDamageAndPilotState; Timeout_NoVictoryRewards; TimeoutSettlement_ExactlyOnce; TimeoutFinishedCheckpoint_DurableBeforeRelease; TimeoutReplayReceipt_NoReapply.

## Аудит EP-0035 — 2026-11-17

Итог: 17 историй / 66 тикетов. Добавлена [US0017 — интеграция мира](EP-0035-demo-pirate-encounters/EP-0035-US-0017-world-integration/EP-0035-US-0017-world-integration.md), два engine-тикета: ранний campaign dispatcher и интеграция motion шага. Полная актуальная карта и зависимости — в [эпике](EP-0035-demo-pirate-encounters/Documentation.md); [исправления аудита](EP-0035-demo-pirate-encounters/GroomingAudit.md). D-35-50: после завершённого боя участвовавшая группа удаляется с карты; actual damage и durable recovery сохраняются. Старые численные карты выше — история изменений, не текущий состав. Реализация не начата.

## EP-0035 implementation update — 2026-11-17

17 stories /66 tickets implemented, including final project documentation. Current execution, publication and remaining validation gates: [EP-0035 execution](EP-0035-demo-pirate-encounters/ExecutionStatus.md). Previous not-started counts/notes are planning history. Native/product acceptance awaits owner.
