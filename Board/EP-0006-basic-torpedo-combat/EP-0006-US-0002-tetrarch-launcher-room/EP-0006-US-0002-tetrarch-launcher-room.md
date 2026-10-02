---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0002-tetrarch-launcher-room
title: "Тетрарх получает помещение торпедного аппарата"
stage: draft
depends_on: ["EP-0006-US-0001-combat-configuration"]
ticket_count: 3
created: 2026-10-02T08:22:58Z
revision: 1
current_review: complete
validation_status: planning-only
---

# US-0002 — Тетрарх получает помещение торпедного аппарата

## Входное техническое задание

Покрывает решения **D01, D03, D17** из [эпика](../Documentation.md). Исходный запрос — создать план первой реализации согласованного торпедного боя. Новые технические имена являются проектным предложением.

## User story

Как игрок, я хочу получить Тетрарх с установленным торпедным аппаратом в новом помещении схемы корпуса. Существующий сценарий Player Ship and Pirate сохраняет два корабля и прямолинейное движение пирата. Пират получает тот же модуль, но не стреляет.

## Acceptance criteria

- **AC-0001** — В canonical hull grid появляется новая structural cell (3,2) и один launcher; старые модули остаются на местах.
- **AC-0002** — PlayerShipOnly сохраняет исходные позиции, скорость 0,4 км/с и курс 120° пирата; оба корабля явно ship.tetrarch и имеют полные 450 HP.
- **AC-0003** — Стандартные Тетрархи других существующих сценариев используют согласованную комплектацию; специализированный MarketProfiles с трёхклеточным корпусом не переклассифицируется по изображению.

## Completion evidence

- В canonical hull grid появляется новая structural cell (3,2) и один launcher; старые модули остаются на местах.
- PlayerShipOnly сохраняет исходные позиции, скорость 0,4 км/с и курс 120° пирата; оба корабля явно ship.tetrarch и имеют полные 450 HP.
- Стандартные Тетрархи других существующих сценариев используют согласованную комплектацию; специализированный MarketProfiles с трёхклеточным корпусом не переклассифицируется по изображению.

Проверка производится именованными тестами ниже и, для визуальных изменений, native smoke. Наличие файлов плана не подтверждает реализацию.

## Dependencies

[EP-0006-US-0001-combat-configuration](../EP-0006-US-0001-combat-configuration/EP-0006-US-0001-combat-configuration.md). Предыдущая история предоставляет проверенные контракты и поведение для этой поставки.

Внутри — последовательность TK-0001→TK-0003; первая задача зависит от последнего тикета предыдущей истории. Перед реализацией проверить наличие runtime результатов зависимостей, а не только Board status. Проходить тикеты по порядку; следующий — после review/STATUS: APPROVED предыдущего. Стадия draft текущего пакета не подменяет этот gate.

## Карта тикетов — draft

| ID | Результат | Layer | Depends on | Serves |
|---|---|---|---|---|
| [EP-0006-US-0002-TK-0001-combat-bootstrap](EP-0006-US-0002-TK-0001-combat-bootstrap/EP-0006-US-0002-TK-0001-combat-bootstrap.md) | Инициализация класса, HP и аппарата в Engine | engine | EP-0006-US-0001-TK-0004-tetrarch-class-content | AC-0001, AC-0002, AC-0003 |
| [EP-0006-US-0002-TK-0002-primary-scenario-loadout](EP-0006-US-0002-TK-0002-primary-scenario-loadout/EP-0006-US-0002-TK-0002-primary-scenario-loadout.md) | Аппарат в боевом и основном сценариях | content-data | EP-0006-US-0002-TK-0001-combat-bootstrap | AC-0001, AC-0002 |
| [EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts](EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts/EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts.md) | Согласование остальных стандартных Тетрархов | content-data | EP-0006-US-0002-TK-0002-primary-scenario-loadout | AC-0003 |

## Non-goals и инварианты

Не расширять story до NPC-боя, защиты, вероятностей, дальности, боезапаса, реальных операторов, самоуничтожения, звука, salvage или полной переработки ShipScreen. Все ограничения [общего контракта эпика](../Documentation.md) действуют. Source of truth gameplay — Engine; Client показывает snapshots и shared Motion prediction.

Каждый тикет ограничен одним production layer и максимум пятью файлами вместе с тестами. Работы вне Code context не включены. Существующий Approach, clock domains, strict save validation и knowledge masking сохраняются.

## Gaps and backlog

Согласованные правила перечислены в D01, D03, D17; технические A01–A12 и риски описаны в эпике и конкретных тикетах. Нет блокирующего вопроса для draft. Новые APIs/files в карте ещё не реализованы. Если текущий код к моменту реализации расходится с baseline, сначала актуализировать границы, а не обходить их.

## Review log

- 2026-10-02T08:22:58Z: история и 3 тикета подготовлены по прямому поручению пользователя; отдельного «утверждаю» для этих артефактов не получено.
- `current_review: complete` означает проверенную полноту planning package. Runtime/tests/native smoke не выполнялись.

