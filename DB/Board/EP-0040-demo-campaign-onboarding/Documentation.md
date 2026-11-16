---
epic: EP-0040-demo-campaign-onboarding
title: Обучение и понятность кампании
stage: approved
created: 2026-11-16T19:49:59Z
source_request: "План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации."
current_review: complete
plan_status: approved
approval_scope: "Одобренный план; отдельное human approval детализации историй не заявляется"
epic_approval: "План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации."
epic_approval_utc: 2026-11-16T19:49:59Z
priority: P1
workstream: cross-cutting
dependencies: [EP-0034-demo-transport-contracts, EP-0037-demo-voyage-economy, EP-0035-demo-pirate-encounters, EP-0033-demo-company-services, EP-0039-demo-cluster-story, EP-0038-demo-pilot-progression]
implementation_status: not-started
---

# EP-0040 — Обучение и понятность кампании

Д10 одобренного плана. **Объём: 5 пользовательских историй, 9 тикетов.** Сквозная работа начинается с доступных историй, не ждёт завершения всех эпиков из aggregate dependencies.

## Исходный запрос и граница поручения

> План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации.

Запрос разрешает создание/уточнение Board. Ранее приложенный расширенный план — продуктовый материал, заменённый одобренной сокращённой версией; он не разрешает исполнение или публикацию. Исходное поручение ограничивало этап историями. Текущее поручение ниже разрешает тикеты и дополнительные истории; реализация и публикация не запрашивались.

## Grounding

`DB/Board/EP-0028-battle-tutorial/Documentation.md` — боевое обучение; `src/DeepSpaceSaga.Client/UI/Screens/Ship/ShipScreen.cs:265` — существующее представление корпуса и крыла. Отдельный поздний эпик полного UI не нужен.

## Переиспользование и граница нового объёма

Продолжение EP-0027/0028 с оболочкой EP-0026. Базовый UI входит в owning эпики; этот эпик связывает обучение и проверяет понятность, начиная с первого рейса.

[Матрица соответствия старым эпикам](../EP-0031-board-and-demo-scope/DemoPlan.md#переиспользование-ep-0021-и-ep-0029) определяет владельцев без дублирования. Старые тикеты не копируются и не становятся автоматически разрешением на реализацию демо.

## Целевой результат

- Обучение первому мирному рейсу
- Понятность угрозы, боя и восстановления
- Самостоятельное прохождение всей демо

Результаты оцениваются в общем цикле рейса. [Полный сокращённый контракт](../EP-0031-board-and-demo-scope/DemoPlan.md) содержит лимиты, исключения и контрольные точки.

## Пользовательские истории и зависимости

| ID | Результат | Приоритет | Обязательные story dependencies |
|---|---|---|---|
| [EP-0040-US-0001-first-voyage-guidance](EP-0040-US-0001-first-voyage-guidance/EP-0040-US-0001-first-voyage-guidance.md) | Обучение первому мирному рейсу | P1 | EP-0034-US-0002-passenger-contract, EP-0037-US-0001-trade-opportunities |
| [EP-0040-US-0002-threat-guidance](EP-0040-US-0002-threat-guidance/EP-0040-US-0002-threat-guidance.md) | Понятность угрозы, боя и восстановления | P1 | EP-0035-US-0003-world-battle, EP-0033-US-0004-pilot-recruitment |
| [EP-0040-US-0003-independent-demo](EP-0040-US-0003-independent-demo/EP-0040-US-0003-independent-demo.md) | Самостоятельное прохождение всей демо | P1 | EP-0040-US-0001-first-voyage-guidance, EP-0040-US-0002-threat-guidance, EP-0039-US-0003-captain-finale, EP-0038-US-0003-crew-history, EP-0040-US-0005-demo-ru-en |
| [EP-0040-US-0004-documentation](EP-0040-US-0004-documentation/EP-0040-US-0004-documentation.md) | Документация проекта по итогам эпика | P1 | EP-0040-US-0001-first-voyage-guidance, EP-0040-US-0002-threat-guidance, EP-0040-US-0003-independent-demo, EP-0040-US-0005-demo-ru-en |
| [EP-0040-US-0005-demo-ru-en](EP-0040-US-0005-demo-ru-en/EP-0040-US-0005-demo-ru-en.md) | Демо полностью доступно на русском и английском | P0 | EP-0040-US-0001-first-voyage-guidance, EP-0040-US-0002-threat-guidance, EP-0039-US-0003-captain-finale |

Aggregate dependencies в frontmatter — перечень внешних источников результатов, не барьер «весь эпик завершён». Независимые истории могут разрабатываться после своих prerequisites. Наличие старого implemented статуса не закрывает сквозную приёмку.

## Ограничения, допущения и gaps

- **EP-0040-US-0001-first-voyage-guidance:** Начать до готовности сюжета; готовые подсказки боя EP-0028 не переписывать без причины.
- **EP-0040-US-0002-threat-guidance:** Обучение не гарантирует победу и не отменяет настоящие потери.
- **EP-0040-US-0003-independent-demo:** Native и человеческий плейтест нельзя заменить screenshot/unit проверками; не заявлять эту приёмку сейчас.

Общий backlog: пленные, сопровождение, VIP-спавн, полная верфь, пустой старт, фракционная эскалация, бой с базой, большая прокачка, универсальный квестовый движок и тактическое отступление. Тикеты уточняют прежние gaps рабочими A-assumptions; баланс и приёмочные gates остаются открытыми до исполнения.

## Review log

2026-11-16T19:49:59Z — зафиксирован дословный запрос выше (время записи, не время отправки). Уже данное одобрение плана учтено; повторный approval того же объёма не запрашивался. Истории нарезаны как draft-detail под approved план; индивидуальные подтверждения не выдуманы.

- EP-0040-US-0001-first-voyage-guidance: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0040-US-0002-threat-guidance: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0040-US-0003-independent-demo: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- 2026-11-16T20:02:53Z — сверка с сокращённым планом по поручению «на основе этого пройди по созданными эпикам и сторисам … и реши каких сторисов или эпиков нам не хватает. там же добавь недостающие» (время записи): добавлены истории без владельца в плане и обязательная завершающая документационная история; отдельное human review — не проводилось.


## Epic review log

2026-11-16T19:49:59Z — состав сопоставлен с Д10, сокращённым объёмом и существующими EP-0021/0029. Тикеты не созданы. Проверка ID, локальных ссылок и ацикличности выполняется для всего набора; результаты фиксируются в [общем плане](../EP-0031-board-and-demo-scope/DemoPlan.md).

## Статусы поставки

Planning: подготовлено по одобренному плану. Implementation: NOT STARTED. Automated/runtime/native/product acceptance: NOT RUN / OPEN. Никаких новых свидетельств готовности игры этот документ не создаёт.


## Тикетная декомпозиция первого демо

2026-11-16T20:32:46.441Z — «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Workflow approval, без приписывания human review деталей. Подготовлено 9 тикетов; implementation NOT STARTED.

| Story | Тикетов | Текущие prerequisites |
|---|---:|---|
| [EP-0040-US-0001-first-voyage-guidance](EP-0040-US-0001-first-voyage-guidance/EP-0040-US-0001-first-voyage-guidance.md) | 2 | EP-0034-US-0002-passenger-contract, EP-0037-US-0001-trade-opportunities |
| [EP-0040-US-0002-threat-guidance](EP-0040-US-0002-threat-guidance/EP-0040-US-0002-threat-guidance.md) | 2 | EP-0035-US-0003-world-battle, EP-0033-US-0004-pilot-recruitment |
| [EP-0040-US-0003-independent-demo](EP-0040-US-0003-independent-demo/EP-0040-US-0003-independent-demo.md) | 1 | EP-0040-US-0001-first-voyage-guidance, EP-0040-US-0002-threat-guidance, EP-0039-US-0003-captain-finale, EP-0038-US-0003-crew-history, EP-0040-US-0005-demo-ru-en |
| [EP-0040-US-0004-documentation](EP-0040-US-0004-documentation/EP-0040-US-0004-documentation.md) | 2 | EP-0040-US-0001-first-voyage-guidance, EP-0040-US-0002-threat-guidance, EP-0040-US-0003-independent-demo, EP-0040-US-0005-demo-ru-en |
| [EP-0040-US-0005-demo-ru-en](EP-0040-US-0005-demo-ru-en/EP-0040-US-0005-demo-ru-en.md) | 2 | EP-0040-US-0001-first-voyage-guidance, EP-0040-US-0002-threat-guidance, EP-0039-US-0003-captain-finale |

Порядок ниже — локальная проекция глобального dependency order (не числовая сортировка): EP-0040-US-0001-first-voyage-guidance → EP-0040-US-0002-threat-guidance → EP-0040-US-0005-demo-ru-en → EP-0040-US-0003-independent-demo → EP-0040-US-0004-documentation. Документационная история исполняется после всех функциональных, включая добавленные с большим ID.

[Глобальная карта, полные пути, release gates и открытые допущения](../EP-0041-demo-validation-and-release/TicketPlan.md). Новые API/файлы в тикетах — проект, а не текущая реализация.
