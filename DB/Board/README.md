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
| [EP-maintenance](EP-maintenance/Documentation.md) Исправления и доделки | ongoing | 35 | Постоянный | Завершённые тикеты — `EP-maintenance/Done` |

## Дорожная карта первой демо-версии

Граница демо и решения владельца D-01…D-04 записаны в
[требованиях](../Documentation/01-Requirements/FirstReleaseRequirements.md#первая-демо-версия).
Номера EP-0032…EP-0044 зарезервированы под порядок реализации; папки эпиков
создаются при нарезке по поручению владельца.

| Эпик | Название | Приоритет | Фаза | Цель | Зависит от |
|---|---|---|---|---|---|
| EP-0032 | Встречи на карте и уклонение | P0 | 2 | Бой возникает из мира; уход при верном манёвре и превосходстве в скорости (D-02) | EP-0031 |
| EP-0033 | Трофеи и награды за бой | P0 | 2 | Победа приносит Tokens, торпеды, модули, пленных | EP-0032 |
| EP-0034 | Ремонт, верфь и пополнение крыла | P0 | 2 | Потери восполняются за деньги; включает истории EP-0021 US-0001/0002/0006/0009 | EP-0033 |
| EP-0035 | Пассажирские контракты | P0 | 3 | Перевозки — второй источник дохода; VIP привлекает пиратов | EP-0031 |
| EP-0036 | Боевые и грузовые контракты | P0 | 3 | Станции дают задания: награда, сопровождение, доставка | EP-0032, EP-0035 |
| EP-0037 | Наём и содержание экипажа | P1 | 4 | Замена погибших пилотов (D-03), зарплаты и жизнеобеспечение | EP-0034 |
| EP-0038 | Развитие пилотов и гибель навсегда | P1 | 4 | Опыт, уровни, перки; потеря опытного пилота ощущается | EP-0037 |
| EP-0039 | Пиратская фракция и рост угрозы | P1 | 4 | Типы противников, рост активности по дням, репутация | EP-0032 |
| EP-0040 | Сюжетная линия кластера | P1 | 5 | Несколько квестов с финалом в одном кластере (D-04), экраны победы и поражения | EP-0035, EP-0036, EP-0039 |
| EP-0041 | Онбординг кампании | P2 | 6 | Обучение после боя: стыковка, сделка, контракт, встреча | EP-0040 |
| EP-0042 | Баланс кампании | P2 | 6 | Первые 10–20 дней напряжённые, но проходимые | EP-0032…EP-0040 |
| EP-0043 | Экран корабля, журнал и уведомления | P2 | 6 | Состояние отряда и обязательства видны игроку | EP-0035, EP-0037 |
| EP-0044 | Сборка и полировка демо | P2 | 6 | Сборка для внешних игроков, F8/F12/Ctrl+Alt+P выключены | все |

Фазы: 1 — фундамент (EP-0031), 2 — боевой цикл, 3 — контракты, 4 — отряд и
угроза, 5 — сюжет, 6 — готовность. Контрольные точки:

1. **Плейтест 1** после EP-0034: цикл «рейс → встреча → бой → трофеи → ремонт» держит интерес. Если нет — сначала дорабатываются встречи и награды.
2. **Плейтест 2** после EP-0040: демо проходится от старта до финала.
3. **Демо-релиз** после EP-0044.
