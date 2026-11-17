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
| [EP-0033](EP-0033-demo-company-services/Documentation.md) Обслуживание и восстановление компании | approved | 0 | Не начат | Демо: 6 историй, 26 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0034](EP-0034-demo-transport-contracts/Documentation.md) Контракты пассажирских и грузовых перевозок | approved | 0 | Не начат | Демо: 7 историй, 26 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0035](EP-0035-demo-pirate-encounters/Documentation.md) Пиратские встречи и избегание перехвата | approved | 0 | Не начат | Демо: 6 историй, 21 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0036](EP-0036-demo-bounties-and-loot/Documentation.md) Боевые заказы и простая добыча | approved | 0 | Не начат | Демо: 5 историй, 17 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0037](EP-0037-demo-voyage-economy/Documentation.md) Экономика рейса и первое улучшение | approved | 0 | Не начат | Демо: 5 историй, 14 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0038](EP-0038-demo-pilot-progression/Documentation.md) Ограниченное развитие пилотов | approved | 0 | Не начат | Демо: 4 историй, 15 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0039](EP-0039-demo-cluster-story/Documentation.md) Сюжет кластера и финал демо | approved | 0 | Не начат | Демо: 6 историй, 27 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0040](EP-0040-demo-campaign-onboarding/Documentation.md) Обучение и понятность кампании | approved | 0 | Не начат | Демо: 5 историй, 9 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-0041](EP-0041-demo-validation-and-release/Documentation.md) Сквозная проверка и выпуск демо | approved | 0 | Не начат | Демо: 7 историй, 17 тикетов; workflow ready; [DemoPlan](EP-0031-board-and-demo-scope/DemoPlan.md) |
| [EP-maintenance](EP-maintenance/Documentation.md) Исправления и доделки | ongoing | 35 | Постоянный | Завершённые тикеты — `EP-maintenance/Done` |

## Дорожная карта первой демо-версии

Текущая ревизия — [сокращённый одобренный план](EP-0031-board-and-demo-scope/DemoPlan.md). Он заменяет прежний расширенный резерв EP-0032…EP-0044. Демо: 60–120 минут, один кластер, торговля + перевозки + бой, готовый Tetrarch, настоящие потери и восстановление.

| План | Эпик | Приоритет | Истории | Внешние зависимости по историям |
|---|---|---|---:|---|
| Д1 | [EP-0031](EP-0031-board-and-demo-scope/Documentation.md), новая US-0005 | P0 | 1 новая | Существующая основа EP-0031 |
| Д2 | [EP-0032](EP-0032-demo-campaign-continuity/Documentation.md) — Непрерывная кампания и последствия боя | P0 | 7 | EP-0031 |
| Д3 | [EP-0033](EP-0033-demo-company-services/Documentation.md) — Обслуживание и восстановление компании | P0 | 6 | EP-0032, EP-0034 |
| Д4 | [EP-0034](EP-0034-demo-transport-contracts/Documentation.md) — Контракты пассажирских и грузовых перевозок | P0 | 7 | EP-0032 |
| Д5 | [EP-0035](EP-0035-demo-pirate-encounters/Documentation.md) — Пиратские встречи и избегание перехвата | P0 | 6 | EP-0032, EP-0034 |
| Д6 | [EP-0036](EP-0036-demo-bounties-and-loot/Documentation.md) — Боевые заказы и простая добыча | P0 | 5 | EP-0034, EP-0035 |
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
