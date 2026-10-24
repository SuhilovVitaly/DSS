---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0008-basic-combat-proof
title: "Полный бой в существующем сценарии"
stage: draft
depends_on: ["EP-0006-US-0007-combat-save-load"]
ticket_count: 2
created: 2026-10-02T08:22:58Z
revision: 1
current_review: complete
validation_status: planning-only
---

# US-0008 — Полный бой в существующем сценарии

## Входное техническое задание

Покрывает решения **D01–D17** из [эпика](../Documentation.md). Исходный запрос — создать план первой реализации согласованного торпедного боя. Новые технические имена являются проектным предложением.

## User story

Как игрок, я прохожу весь первый бой в Player Ship and Pirate: выбираю цель, выполняю три ручных пуска и получаю врек. Могу двигать свой корабль, менять скорость симуляции и продолжать сохранённый бой. Готовность этой истории означает подтверждённую совместную работу всех частей эпика.

## Acceptance criteria

- **AC-0001** — Сквозной прогон использует реальный scenario/content, PlayerCommand и session boundary; никаких teleport, direct HP edits или подмены торпед.
- **AC-0002** — Зафиксированы три пуска, три первых контакта, HP 450/300/150/0, единственный stationary wreck и готовый launcher после взрывов.
- **AC-0003** — Проведён native/UI smoke движения, паузы, ускорения, gray preview, цветов после restart, selection, real-time effects и save/load; headless success не заменяет UI smoke.

## Completion evidence

- Сквозной прогон использует реальный scenario/content, PlayerCommand и session boundary; никаких teleport, direct HP edits или подмены торпед.
- Зафиксированы три пуска, три первых контакта, HP 450/300/150/0, единственный stationary wreck и готовый launcher после взрывов.
- Проведён native/UI smoke движения, паузы, ускорения, gray preview, цветов после restart, selection, real-time effects и save/load; headless success не заменяет UI smoke.

Проверка производится именованными тестами ниже и, для визуальных изменений, native smoke. Наличие файлов плана не подтверждает реализацию.

## Dependencies

[EP-0006-US-0007-combat-save-load](../EP-0006-US-0007-combat-save-load/EP-0006-US-0007-combat-save-load.md). Предыдущая история предоставляет проверенные контракты и поведение для этой поставки.

Внутри — последовательность TK-0001→TK-0002; первая задача зависит от последнего тикета предыдущей истории. Перед реализацией проверить наличие runtime результатов зависимостей, а не только Board status. Проходить тикеты по порядку; следующий — после review/STATUS: APPROVED предыдущего. Стадия draft текущего пакета не подменяет этот gate.

## Карта тикетов — draft

| ID | Результат | Layer | Depends on | Serves |
|---|---|---|---|---|
| [EP-0006-US-0008-TK-0001-three-hit-integration](EP-0006-US-0008-TK-0001-three-hit-integration/EP-0006-US-0008-TK-0001-three-hit-integration.md) | Регрессионное доказательство трёх пусков | engine | EP-0006-US-0007-TK-0003-combat-presentation-resume | AC-0001, AC-0002 |
| [EP-0006-US-0008-TK-0002-native-combat-acceptance](EP-0006-US-0008-TK-0002-native-combat-acceptance/EP-0006-US-0008-TK-0002-native-combat-acceptance.md) | Проверка интерфейса и инструкция воспроизведения | client | EP-0006-US-0008-TK-0001-three-hit-integration | AC-0001, AC-0002, AC-0003 |

## Non-goals и инварианты

Не расширять story до NPC-боя, защиты, вероятностей, дальности, боезапаса, реальных операторов, самоуничтожения, звука, salvage или полной переработки ShipScreen. Все ограничения [общего контракта эпика](../Documentation.md) действуют. Source of truth gameplay — Engine; Client показывает snapshots и shared Motion prediction.

Каждый тикет ограничен одним production layer и максимум пятью файлами вместе с тестами. Работы вне Code context не включены. Существующий Approach, clock domains, strict save validation и knowledge masking сохраняются.

## Gaps and backlog

Согласованные правила перечислены в D01–D17; технические A01–A12 и риски описаны в эпике и конкретных тикетах. Нет блокирующего вопроса для draft. Новые APIs/files в карте ещё не реализованы. Если текущий код к моменту реализации расходится с baseline, сначала актуализировать границы, а не обходить их.

## Review log

- 2026-10-02T08:22:58Z: история и 2 тикета подготовлены по прямому поручению пользователя; отдельного «утверждаю» для этих артефактов не получено.
- `current_review: complete` означает проверенную полноту planning package. Runtime/tests/native smoke не выполнялись.

