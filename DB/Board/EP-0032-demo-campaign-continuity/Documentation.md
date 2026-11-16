---
epic: EP-0032-demo-campaign-continuity
title: Непрерывная кампания и последствия боя
stage: approved
created: 2026-11-16T19:49:59Z
source_request: "План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации."
current_review: complete
plan_status: approved
approval_scope: "Одобренный план; отдельное human approval детализации историй не заявляется"
epic_approval: "План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации."
epic_approval_utc: 2026-11-16T19:49:59Z
priority: P0
workstream: feature
dependencies: [EP-0031-board-and-demo-scope]
implementation_status: not-started
---

# EP-0032 — Непрерывная кампания и последствия боя

Д2 одобренного плана. **Объём: 7 пользовательских историй, 28 тикетов.** Порядок определяется зависимостями конкретных историй, а не номером эпика.

## Исходный запрос и граница поручения

> План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации.

Запрос разрешает создание/уточнение Board. Ранее приложенный расширенный план — продуктовый материал, заменённый одобренной сокращённой версией; он не разрешает исполнение или публикацию. Исходное поручение ограничивало этап историями. Текущее поручение ниже разрешает тикеты и дополнительные истории; реализация и публикация не запрашивались.

## Grounding

`src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs:17` — CommitSortieOutcome переносит потери дронов и корпус; ветка destroyed на строке 35 не изменяет roster экипажа. `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:40` — writer версии 21. EP-0029 Documentation.md отмечает implementation implemented, native/owner acceptance открыты.

## Переиспользование и граница нового объёма

EP-0029: постоянный носитель, owned wing, rearm, settlement и Save21 переиспользуются. Новый объём — смертность пилотов и сквозная приёмка; повторная реализация этих систем не требуется.

[Матрица соответствия старым эпикам](../EP-0031-board-and-demo-scope/DemoPlan.md#переиспользование-ep-0021-и-ep-0029) определяет владельцев без дублирования. Старые тикеты не копируются и не становятся автоматически разрешением на реализацию демо.

## Целевой результат

- Продолжение кампании после настоящего боя
- Необратимая гибель пилота
- Сохранение последствий и понятный конец кампании

Результаты оцениваются в общем цикле рейса. [Полный сокращённый контракт](../EP-0031-board-and-demo-scope/DemoPlan.md) содержит лимиты, исключения и контрольные точки.

## Пользовательские истории и зависимости

| ID | Результат | Приоритет | Обязательные story dependencies |
|---|---|---|---|
| [EP-0032-US-0001-campaign-roundtrip](EP-0032-US-0001-campaign-roundtrip/EP-0032-US-0001-campaign-roundtrip.md) | Продолжение кампании после настоящего боя | P0 | EP-0031-US-0005-reduced-demo-contract, EP-0032-US-0007-consistent-campaign-actions |
| [EP-0032-US-0002-permanent-pilot-loss](EP-0032-US-0002-permanent-pilot-loss/EP-0032-US-0002-permanent-pilot-loss.md) | Необратимая гибель пилота | P0 | EP-0032-US-0001-campaign-roundtrip |
| [EP-0032-US-0003-persistent-outcomes](EP-0032-US-0003-persistent-outcomes/EP-0032-US-0003-persistent-outcomes.md) | Сохранение последствий и понятный конец кампании | P0 | EP-0032-US-0002-permanent-pilot-loss |
| [EP-0032-US-0004-demo-campaign-start](EP-0032-US-0004-demo-campaign-start/EP-0032-US-0004-demo-campaign-start.md) | Новая кампания в демо-кластере | P0 | EP-0031-US-0005-reduced-demo-contract, EP-0032-US-0007-consistent-campaign-actions |
| [EP-0032-US-0005-autosave-and-continue](EP-0032-US-0005-autosave-and-continue/EP-0032-US-0005-autosave-and-continue.md) | Автосохранение и продолжение кампании | P0 | EP-0032-US-0003-persistent-outcomes, EP-0032-US-0004-demo-campaign-start |
| [EP-0032-US-0006-documentation](EP-0032-US-0006-documentation/EP-0032-US-0006-documentation.md) | Документация проекта по итогам эпика | P0 | EP-0032-US-0001-campaign-roundtrip, EP-0032-US-0002-permanent-pilot-loss, EP-0032-US-0003-persistent-outcomes, EP-0032-US-0004-demo-campaign-start, EP-0032-US-0005-autosave-and-continue, EP-0032-US-0007-consistent-campaign-actions |
| [EP-0032-US-0007-consistent-campaign-actions](EP-0032-US-0007-consistent-campaign-actions/EP-0032-US-0007-consistent-campaign-actions.md) | Согласованные действия и сохранённое состояние кампании | P0 | EP-0031-US-0005-reduced-demo-contract |

Aggregate dependencies в frontmatter — перечень внешних источников результатов, не барьер «весь эпик завершён». Независимые истории могут разрабатываться после своих prerequisites. Наличие старого implemented статуса не закрывает сквозную приёмку.

## Ограничения, допущения и gaps

- **EP-0032-US-0001-campaign-roundtrip:** Не создавать новый параллельный носитель или боевой inventory.
- **EP-0032-US-0002-permanent-pilot-loss:** Постоянная смерть — новое правило демо, не считать её уже доказанной EP-0029; без пленных и спасения пилотов.
- **EP-0032-US-0003-persistent-outcomes:** Сохранение посреди тактического боя не вводится; политика checkpoint уточнена в US-0003/0005 и общей карте TicketPlan.

Общий backlog: пленные, сопровождение, VIP-спавн, полная верфь, пустой старт, фракционная эскалация, бой с базой, большая прокачка, универсальный квестовый движок и тактическое отступление. Тикеты уточняют прежние gaps рабочими A-assumptions; баланс и приёмочные gates остаются открытыми до исполнения.

## Review log

2026-11-16T19:49:59Z — зафиксирован дословный запрос выше (время записи, не время отправки). Уже данное одобрение плана учтено; повторный approval того же объёма не запрашивался. Истории нарезаны как draft-detail под approved план; индивидуальные подтверждения не выдуманы.

- EP-0032-US-0001-campaign-roundtrip: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0032-US-0002-permanent-pilot-loss: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0032-US-0003-persistent-outcomes: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- 2026-11-16T20:02:53Z — сверка с сокращённым планом по поручению «на основе этого пройди по созданными эпикам и сторисам … и реши каких сторисов или эпиков нам не хватает. там же добавь недостающие» (время записи): добавлены истории без владельца в плане и обязательная завершающая документационная история; отдельное human review — не проводилось.

- 2026-11-16T20:31:00Z — проверка достаточности EP-0032…EP-0041 для первой демо (время записи): добавлена история без владельца в плане, документационная история перенумерована на последнюю позицию; отдельное human review — не проводилось.


## Epic review log

2026-11-16T19:49:59Z — состав сопоставлен с Д2, сокращённым объёмом и существующими EP-0021/0029. Тикеты не созданы. Проверка ID, локальных ссылок и ацикличности выполняется для всего набора; результаты фиксируются в [общем плане](../EP-0031-board-and-demo-scope/DemoPlan.md).

## Статусы поставки

Planning: подготовлено по одобренному плану. Implementation: NOT STARTED. Automated/runtime/native/product acceptance: NOT RUN / OPEN. Никаких новых свидетельств готовности игры этот документ не создаёт.


## Тикетная декомпозиция первого демо

2026-11-16T20:32:46.441Z — «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Workflow approval, без приписывания human review деталей. Подготовлено 28 тикетов; implementation NOT STARTED.

| Story | Тикетов | Текущие prerequisites |
|---|---:|---|
| [EP-0032-US-0001-campaign-roundtrip](EP-0032-US-0001-campaign-roundtrip/EP-0032-US-0001-campaign-roundtrip.md) | 4 | EP-0031-US-0005-reduced-demo-contract, EP-0032-US-0007-consistent-campaign-actions |
| [EP-0032-US-0002-permanent-pilot-loss](EP-0032-US-0002-permanent-pilot-loss/EP-0032-US-0002-permanent-pilot-loss.md) | 4 | EP-0032-US-0001-campaign-roundtrip |
| [EP-0032-US-0003-persistent-outcomes](EP-0032-US-0003-persistent-outcomes/EP-0032-US-0003-persistent-outcomes.md) | 5 | EP-0032-US-0002-permanent-pilot-loss |
| [EP-0032-US-0004-demo-campaign-start](EP-0032-US-0004-demo-campaign-start/EP-0032-US-0004-demo-campaign-start.md) | 5 | EP-0031-US-0005-reduced-demo-contract, EP-0032-US-0007-consistent-campaign-actions |
| [EP-0032-US-0005-autosave-and-continue](EP-0032-US-0005-autosave-and-continue/EP-0032-US-0005-autosave-and-continue.md) | 3 | EP-0032-US-0003-persistent-outcomes, EP-0032-US-0004-demo-campaign-start |
| [EP-0032-US-0006-documentation](EP-0032-US-0006-documentation/EP-0032-US-0006-documentation.md) | 2 | EP-0032-US-0001-campaign-roundtrip, EP-0032-US-0002-permanent-pilot-loss, EP-0032-US-0003-persistent-outcomes, EP-0032-US-0004-demo-campaign-start, EP-0032-US-0005-autosave-and-continue, EP-0032-US-0007-consistent-campaign-actions |
| [EP-0032-US-0007-consistent-campaign-actions](EP-0032-US-0007-consistent-campaign-actions/EP-0032-US-0007-consistent-campaign-actions.md) | 5 | EP-0031-US-0005-reduced-demo-contract |

Порядок ниже — локальная проекция глобального dependency order (не числовая сортировка): EP-0032-US-0007-consistent-campaign-actions → EP-0032-US-0001-campaign-roundtrip → EP-0032-US-0002-permanent-pilot-loss → EP-0032-US-0003-persistent-outcomes → EP-0032-US-0004-demo-campaign-start → EP-0032-US-0005-autosave-and-continue → EP-0032-US-0006-documentation. Документационная история исполняется после всех функциональных, включая добавленные с большим ID.

[Глобальная карта, полные пути, release gates и открытые допущения](../EP-0041-demo-validation-and-release/TicketPlan.md). Новые API/файлы в тикетах — проект, а не текущая реализация.
