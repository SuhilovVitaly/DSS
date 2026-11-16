---
epic: EP-0041-demo-validation-and-release
title: Сквозная проверка и выпуск демо
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
dependencies: [EP-0032-demo-campaign-continuity, EP-0033-demo-company-services, EP-0037-demo-voyage-economy, EP-0040-demo-campaign-onboarding, EP-0039-demo-cluster-story, EP-0034-demo-transport-contracts, EP-0035-demo-pirate-encounters, EP-0036-demo-bounties-and-loot, EP-0038-demo-pilot-progression]
implementation_status: not-started
---

# EP-0041 — Сквозная проверка и выпуск демо

Д11 одобренного плана. **Объём: 7 пользовательских историй, 17 тикетов.** Сквозная работа начинается с доступных историй, не ждёт завершения всех эпиков из aggregate dependencies.

## Исходный запрос и граница поручения

> План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации.

Запрос разрешает создание/уточнение Board. Ранее приложенный расширенный план — продуктовый материал, заменённый одобренной сокращённой версией; он не разрешает исполнение или публикацию. Исходное поручение ограничивало этап историями. Текущее поручение ниже разрешает тикеты и дополнительные истории; реализация и публикация не запрашивались.

## Grounding

`DB/Documentation/01-Requirements/EngineRequirements.md` — 80 FPS является целевым требованием, не фактом; `DB/Documentation/00-Process/AGENTS.md` — native запуск только после проверки меньшего монитора. EP-0029 Documentation.md оставляет native/owner acceptance открытой.

## Переиспользование и граница нового объёма

Сквозные gates EP-0029, EP-0024/0026 и нового цикла. Это приёмка готовых функций, а не поздняя реализация журналов или persistence.

[Матрица соответствия старым эпикам](../EP-0031-board-and-demo-scope/DemoPlan.md#переиспользование-ep-0021-и-ep-0029) определяет владельцев без дублирования. Старые тикеты не копируются и не становятся автоматически разрешением на реализацию демо.

## Целевой результат

- Ранняя проверка потерь и восстановления
- Первый плейтест смешанного рейса
- Устойчивая сборка для внешнего игрока
- Приёмка демо на 60–120 минут

Результаты оцениваются в общем цикле рейса. [Полный сокращённый контракт](../EP-0031-board-and-demo-scope/DemoPlan.md) содержит лимиты, исключения и контрольные точки.

## Пользовательские истории и зависимости

| ID | Результат | Приоритет | Обязательные story dependencies |
|---|---|---|---|
| [EP-0041-US-0001-early-continuity-check](EP-0041-US-0001-early-continuity-check/EP-0041-US-0001-early-continuity-check.md) | Ранняя проверка потерь и восстановления | P1 | EP-0032-US-0003-persistent-outcomes, EP-0033-US-0003-drone-replacement, EP-0033-US-0004-pilot-recruitment |
| [EP-0041-US-0002-first-gameplay-check](EP-0041-US-0002-first-gameplay-check/EP-0041-US-0002-first-gameplay-check.md) | Первый плейтест смешанного рейса | P1 | EP-0041-US-0001-early-continuity-check, EP-0037-US-0004-recovery-playtest |
| [EP-0041-US-0003-release-candidate](EP-0041-US-0003-release-candidate/EP-0041-US-0003-release-candidate.md) | Устойчивая сборка для внешнего игрока | P1 | EP-0041-US-0002-first-gameplay-check, EP-0041-US-0005-demo-audio |
| [EP-0041-US-0004-public-demo-gate](EP-0041-US-0004-public-demo-gate/EP-0041-US-0004-public-demo-gate.md) | Приёмка демо на 60–120 минут | P1 | EP-0041-US-0003-release-candidate, EP-0040-US-0003-independent-demo, EP-0039-US-0004-optional-events, EP-0032-US-0006-documentation, EP-0033-US-0006-documentation, EP-0034-US-0007-documentation, EP-0035-US-0006-documentation, EP-0036-US-0005-documentation, EP-0037-US-0005-documentation, EP-0038-US-0004-documentation, EP-0039-US-0006-documentation, EP-0040-US-0004-documentation |
| [EP-0041-US-0005-demo-audio](EP-0041-US-0005-demo-audio/EP-0041-US-0005-demo-audio.md) | Звук демо | P1 | EP-0041-US-0007-licensed-audio-content, EP-0035-US-0003-world-battle, EP-0036-US-0002-bounty-payment |
| [EP-0041-US-0006-documentation](EP-0041-US-0006-documentation/EP-0041-US-0006-documentation.md) | Документация проекта по итогам эпика | P1 | EP-0041-US-0001-early-continuity-check, EP-0041-US-0002-first-gameplay-check, EP-0041-US-0003-release-candidate, EP-0041-US-0004-public-demo-gate, EP-0041-US-0005-demo-audio, EP-0041-US-0007-licensed-audio-content |
| [EP-0041-US-0007-licensed-audio-content](EP-0041-US-0007-licensed-audio-content/EP-0041-US-0007-licensed-audio-content.md) | Звуковой комплект первого демо | P0 | none |

Aggregate dependencies в frontmatter — перечень внешних источников результатов, не барьер «весь эпик завершён». Независимые истории могут разрабатываться после своих prerequisites. Наличие старого implemented статуса не закрывает сквозную приёмку.

## Ограничения, допущения и gaps

- **EP-0041-US-0001-early-continuity-check:** Будущие test runs имеют process-tree cap 300000 ms и абсолютный results-directory в workspace artifacts.
- **EP-0041-US-0002-first-gameplay-check:** Протокол — evidence плейтеста, не автоматическая имитация человеческого мнения.
- **EP-0041-US-0003-release-candidate:** G-02: целевая ОС, способ упаковки и матрица оборудования должны быть зафиксированы до тикетов; конкретный установщик и сетевой crash upload не навязываются.
- **EP-0041-US-0004-public-demo-gate:** Запуск только на проверенном меньшем мониторе с DSS_VALIDATION_SMALL_MONITOR=1; без такой возможности headless не закрывает native gate. Публикация требует отдельного поручения.

Общий backlog: пленные, сопровождение, VIP-спавн, полная верфь, пустой старт, фракционная эскалация, бой с базой, большая прокачка, универсальный квестовый движок и тактическое отступление. Тикеты уточняют прежние gaps рабочими A-assumptions; баланс и приёмочные gates остаются открытыми до исполнения.

## Review log

2026-11-16T19:49:59Z — зафиксирован дословный запрос выше (время записи, не время отправки). Уже данное одобрение плана учтено; повторный approval того же объёма не запрашивался. Истории нарезаны как draft-detail под approved план; индивидуальные подтверждения не выдуманы.

- EP-0041-US-0001-early-continuity-check: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0041-US-0002-first-gameplay-check: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0041-US-0003-release-candidate: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0041-US-0004-public-demo-gate: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- 2026-11-16T20:02:53Z — сверка с сокращённым планом по поручению «на основе этого пройди по созданными эпикам и сторисам … и реши каких сторисов или эпиков нам не хватает. там же добавь недостающие» (время записи): добавлены истории без владельца в плане и обязательная завершающая документационная история; отдельное human review — не проводилось.

- 2026-11-16T20:31:00Z — проверка достаточности EP-0032…EP-0041 для первой демо (время записи): добавлена история без владельца в плане, документационная история перенумерована на последнюю позицию; отдельное human review — не проводилось.


## Epic review log

2026-11-16T19:49:59Z — состав сопоставлен с Д11, сокращённым объёмом и существующими EP-0021/0029. Тикеты не созданы. Проверка ID, локальных ссылок и ацикличности выполняется для всего набора; результаты фиксируются в [общем плане](../EP-0031-board-and-demo-scope/DemoPlan.md).

## Статусы поставки

Planning: подготовлено по одобренному плану. Implementation: NOT STARTED. Automated/runtime/native/product acceptance: NOT RUN / OPEN. Никаких новых свидетельств готовности игры этот документ не создаёт.


## Тикетная декомпозиция первого демо

2026-11-16T20:32:46.441Z — «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Workflow approval, без приписывания human review деталей. Подготовлено 17 тикетов; implementation NOT STARTED.

| Story | Тикетов | Текущие prerequisites |
|---|---:|---|
| [EP-0041-US-0001-early-continuity-check](EP-0041-US-0001-early-continuity-check/EP-0041-US-0001-early-continuity-check.md) | 1 | EP-0032-US-0003-persistent-outcomes, EP-0033-US-0003-drone-replacement, EP-0033-US-0004-pilot-recruitment |
| [EP-0041-US-0002-first-gameplay-check](EP-0041-US-0002-first-gameplay-check/EP-0041-US-0002-first-gameplay-check.md) | 1 | EP-0041-US-0001-early-continuity-check, EP-0037-US-0004-recovery-playtest |
| [EP-0041-US-0003-release-candidate](EP-0041-US-0003-release-candidate/EP-0041-US-0003-release-candidate.md) | 2 | EP-0041-US-0002-first-gameplay-check, EP-0041-US-0005-demo-audio |
| [EP-0041-US-0004-public-demo-gate](EP-0041-US-0004-public-demo-gate/EP-0041-US-0004-public-demo-gate.md) | 3 | EP-0041-US-0003-release-candidate, EP-0040-US-0003-independent-demo, EP-0039-US-0004-optional-events, EP-0032-US-0006-documentation, EP-0033-US-0006-documentation, EP-0034-US-0007-documentation, EP-0035-US-0006-documentation, EP-0036-US-0005-documentation, EP-0037-US-0005-documentation, EP-0038-US-0004-documentation, EP-0039-US-0006-documentation, EP-0040-US-0004-documentation |
| [EP-0041-US-0005-demo-audio](EP-0041-US-0005-demo-audio/EP-0041-US-0005-demo-audio.md) | 4 | EP-0041-US-0007-licensed-audio-content, EP-0035-US-0003-world-battle, EP-0036-US-0002-bounty-payment |
| [EP-0041-US-0006-documentation](EP-0041-US-0006-documentation/EP-0041-US-0006-documentation.md) | 2 | EP-0041-US-0001-early-continuity-check, EP-0041-US-0002-first-gameplay-check, EP-0041-US-0003-release-candidate, EP-0041-US-0004-public-demo-gate, EP-0041-US-0005-demo-audio, EP-0041-US-0007-licensed-audio-content |
| [EP-0041-US-0007-licensed-audio-content](EP-0041-US-0007-licensed-audio-content/EP-0041-US-0007-licensed-audio-content.md) | 4 | none |

Порядок ниже — локальная проекция глобального dependency order (не числовая сортировка): EP-0041-US-0001-early-continuity-check → EP-0041-US-0002-first-gameplay-check → EP-0041-US-0007-licensed-audio-content → EP-0041-US-0005-demo-audio → EP-0041-US-0003-release-candidate → EP-0041-US-0004-public-demo-gate → EP-0041-US-0006-documentation. Документационная история исполняется после всех функциональных, включая добавленные с большим ID.

[Глобальная карта, полные пути, release gates и открытые допущения](TicketPlan.md). Новые API/файлы в тикетах — проект, а не текущая реализация.
