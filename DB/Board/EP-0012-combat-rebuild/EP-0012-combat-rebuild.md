---
epic: "EP-0012-combat-rebuild"
title: "Ребилд тактического боя"
stage: "in_progress"
created: "2026-10-20T07:48:46.640Z"
source_request: "Создать эпик, истории и тикеты по приложению, игнорируя внешние файлы"
revision: 4
depends_on_epic: "EP-0011-short-fixes"
story_count: 20
ticket_count: 80
validation_status: "us20-implemented-acceptance-open"
---

# EP-0012 — Ребилд тактического боя

## Current execution status — 2026-10-21

73/73 technical implementations and final documentation/graph work recorded in [ImplementationStatus](ImplementationStatus.md); publication ledger is [PublicationStatus](PublicationStatus.md). Production dodge balance96.6%/P907.4933realmin fails72–88%/<=5.5min. Native24+1instrumented GPU checks passed their stated assertions; physical/human/audio/accessibility/transport-latency gates remain OPEN. Historical planning statements and baseline source observations below do not override actual execution authorization, currentSave18 or measured results.

## Исполнение — 2026-10-20

По новому прямому поручению начато автономное исполнение по [EpicExecutionPrompt](../EpicExecutionPrompt.md). Текущие проверки, ограничения и публикации ведутся в [журнале исполнения](ImplementationStatus.md). Исходные planning-записи ниже сохраняют исторический статус подготовки; они не отменяют новых полномочий. Все73 технических тикета реализованы; итоговая документация и граф завершены. Баланс и ручная приёмка OPEN; эпик полностью принятым не объявляется.

Торпеды и противоракеты летят в выбранную точку; исход определяют геометрия взрыва, ручная ПРО и уклонение. Новый UI даёт прицеливание, панель угроз, состояние модулей и итоги боя.

[Полный контракт, D/A/G и grounding](Documentation.md) · [Порядок тикетов и трассировка](Tickets.md)

**20 историй / 80 тикетов**. Шесть тикетов US20 реализованы; US15-TK6 актуализирует документацию последним. Исходная декомпозиция была draft/planning-only; текущие частичные результаты — в журнале исполнения. Внешние файлы не требуются. UI feedback<=100мс; снаряд появляется на общих подтверждённых часах сцены (ответ пользователя «1»).

## Истории

| Story | Результат | Tickets |
|---|---|---:|
| [US-0001](EP-0012-US-0001-contracts-and-schema/EP-0012-US-0001-contracts-and-schema.md) | Контракты и схема данных | 5 |
| [US-0002](EP-0012-US-0002-combat-catalog-v3/EP-0012-US-0002-combat-catalog-v3.md) | Каталог v3 | 5 |
| [US-0003](EP-0012-US-0003-point-aim-torpedoes/EP-0012-US-0003-point-aim-torpedoes.md) | Торпеда в точку | 5 |
| [US-0004](EP-0012-US-0004-point-aim-countermissiles/EP-0012-US-0004-point-aim-countermissiles.md) | Противоракеты | 3 |
| [US-0005](EP-0012-US-0005-snapshot-privacy/EP-0012-US-0005-snapshot-privacy.md) | Сокрытие информации | 2 |
| [US-0006](EP-0012-US-0006-threats-and-evasion/EP-0012-US-0006-threats-and-evasion.md) | Угрозы и уклонение | 3 |
| [US-0007](EP-0012-US-0007-mukha-ai-v3/EP-0012-US-0007-mukha-ai-v3.md) | ИИ Мухи v3 | 3 |
| [US-0008](EP-0012-US-0008-encounter-and-time/EP-0012-US-0008-encounter-and-time.md) | Боевое столкновение и время | 3 |
| [US-0009](EP-0012-US-0009-scenarios-and-saves/EP-0012-US-0009-scenarios-and-saves.md) | Сценарии и сохранения | 5 |
| [US-0010](EP-0012-US-0010-tactical-map/EP-0012-US-0010-tactical-map.md) | Тактическая карта | 4 |
| [US-0011](EP-0012-US-0011-aiming-and-launch/EP-0012-US-0011-aiming-and-launch.md) | Прицеливание и пуск | 4 |
| [US-0012](EP-0012-US-0012-maneuver-and-threat-ui/EP-0012-US-0012-maneuver-and-threat-ui.md) | Манёвр и угрозы в UI | 3 |
| [US-0013](EP-0012-US-0013-combat-state-and-feedback/EP-0012-US-0013-combat-state-and-feedback.md) | Состояние, панели, обратная связь | 5 |
| [US-0014](EP-0012-US-0014-balance-and-acceptance/EP-0012-US-0014-balance-and-acceptance.md) | Приёмка и баланс | 3 |
| [US-0015](EP-0012-US-0015-project-documentation/EP-0012-US-0015-project-documentation.md) | Документация (обязательная завершающая история) | 6 |
| [US-0016](EP-0012-US-0016-combat-localization/EP-0012-US-0016-combat-localization.md) | Локализация боевого интерфейса | 1 |
| [US-0017](EP-0012-US-0017-event-and-command-integration/EP-0012-US-0017-event-and-command-integration.md) | Контракты событий, серверные доклады и доставка | 4 |
| [US-0018](EP-0012-US-0018-legacy-combat-cleanup/EP-0012-US-0018-legacy-combat-cleanup.md) | Удаление старой боевой модели и регрессии | 5 |
| [US-0019](EP-0012-US-0019-scenario-and-client-regressions/EP-0012-US-0019-scenario-and-client-regressions.md) | Совместимость сценариев и старых клиентских поверхностей | 5 |

| [US-0020](EP-0012-US-0020-bugs/EP-0012-US-0020-bugs.md) | Баги: манёвр, информация корабля, NPC-торпеды, завершение прицеливания, перезарядка и скорости боя | 6 |

## Границы и статус

Базовая модель HP/брони/гибели, determinism, physical clock, EP10 reports и наблюдаемые NPC trajectories переиспользуются; оружие/AI/бой/save18/UI перепроектируются в пределах AC. Basic engine не меняется, Tetrarch получает отдельный; CombatShmel удаляется при реализации, CombatSim не меняется; HostileCombat пока не скрывается.

Полномочия исходного planning-поручения ограничивались Board; приведённые в исходных карточках NOT STARTED/NOT RUN относились к тому этапу. Новое поручение исполнения и фактические частичные результаты описаны выше и в журнале. Исторический planning status: automated/native/product/balance gates были OPEN. Фактические итоговые automated/native проверки и отдельные balance/human ограничения находятся в ImplementationStatus.md. Breaking consumers сверяются по G-02 на каждом шаге; полный execution-ready статус не заявляется.

## Дополнение D-USER-20 — Баги, 2026-10-21

По прямому запросу добавлена [US-0020 «Баги»](EP-0012-US-0020-bugs/EP-0012-US-0020-bugs.md) с шестью draft тикетами. Всего 20 историй / 79 тикетов; исходные 73 имеют записанные результаты, новые 6 NOT STARTED. D20-01–04 и US20-AC1–4 задают полный выбранный манёвр до точки с прямым продолжением за viewport, возврат карточки корабля/перенос всех модульных сведений влево, NPC-торпеду точкой с коротким хвостом и тёмной наблюдённой историей без будущей линии, выход из aiming после отправки. В этих границах US20 приоритетна над конфликтующими прежними UI AC; остальная механика сохраняется. Подробные assumptions/gaps, evidence и allowlists находятся в истории. Исторические 73/73 и acceptance evidence не распространяются на US20. Текущее поручение — только Board planning.

## D20-05/06 — дополнение требований, 2026-10-21

**План, ещё не реализовано.** По завершении каждого цикла перезарядки торпедного аппарата игрока Engine формирует подтверждённое событие с идентификатором аппарата и физическим временем завершения. Игрок один раз слышит звуковой сигнал и получает сообщение «Торпедный аппарат <имя> перезаряжен» в боевом журнале и Combat reports. Событие не повторяется при новых кадрах, повторах доставки, паузе или загрузке уже завершённого цикла. Незавершённый сохранённый цикл уведомляет один раз после завершения. Готовность аппарата при старте новой игры сама по себе не является завершением перезарядки. Уведомления не раскрывают перезарядку NPC.

В активном бою доступны пауза и скорости 1×, 2×, 5×, 10×. Это реальные множители базового темпа: за 4 реальные секунды продвигается соответственно 1/2/5/10 физических секунд. Engine принимает эти значения, UI показывает выбранное подтверждённое значение; 20× и 100× в бою недоступны. Пауза не продвигает бой, AI и перезарядку. Переключение не меняет физические длительности/правила боя и порядок событий; countdown/ETA пересчитываются по текущему множителю. Загрузка боя с любой разрешённой скоростью корректна. После боя сохраняется прежнее правило восстановления добоевой скорости.

Это заменяет прежние O2/D15 и AC о pause/x1-only. Точное сохранение старых enum IDs обязательно. Физическая статистика боя сохраняет единицы; physical×4 допустимо только как эквивалент x1, а не фактически прошедшее реальное время при смене скоростей. Имеющиеся итоговые balance/native evidence относятся к старому поведению и не подтверждают новые скорости/звук. Добавлены два сквозных тикета в US20 по прямому запросу; шесть тикетов в истории, всего 79 в эпике.

## US20 current execution — 2026-10-21

Six bug fixes implemented/published; final US15-TK6 documentation follows them. [Current execution, validation, publication and remaining acceptance](EP-0012-US-0020-bugs/ExecutionStatus.md). The preceding73-ticket records are historical base delivery, not proof of US20.
