---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0001-module-owned-weapon-rules
title: "Характеристики оружия и понятный шанс перехвата"
stage: draft
dependencies: []
created: 2026-10-07
revision: 1
current_review: complete
validation_status: planning-only
---

# Характеристики оружия и понятный шанс перехвата

## User story

Игрок видит параметры установок и понимает шанс сбития. Одинаковые модули обеих сторон работают одинаково, а навыки пока не меняют вероятность.

## Источник и границы

Решения Q14,Q17,Q20–Q25,Q41,Q46,Q48; полный контекст в [журнале решений](../Decisions.md) и [контракте эпика](../Documentation.md). Технические допущения A01–A12 отделены от пользовательских ответов. Scope ограничен результатом этой истории; смежные механики из Non-goals эпика не добавляются.

## Acceptance criteria

- **AC-0001:** Точность ПРО55 и манёвренность торпеды5 дают50%; clamp0..100 без +50 и без skill multiplier, при обязательном операторе.
- **AC-0002:** Параметры дальности/скорости/поворота/lifetime принадлежат типу модуля; runtime captured данные отделены от конфигурации.
- **AC-0003:** Contracts передают обе характеристики, provenance пуска, модульную адресацию и preview target ID; roundtrip не теряет числовые значения.

## Зависимости

- Реализованный runtime EP-0006/EP-0007 и текущий baseline проекта; проверить готовность по коду, не только наличию карточек.

## Карта тикетов и покрытия

| Шаг | Тикет | Layer | Serves |
|---|---|---|---|
| 1 | [EP-0008-US-0001-TK-0001-weapon-contract](EP-0008-US-0001-TK-0001-weapon-contract/EP-0008-US-0001-TK-0001-weapon-contract.md) — Контракт характеристик торпеды | contracts | AC-0001, AC-0002, AC-0003 |
| 2 | [EP-0008-US-0001-TK-0002-defense-contract](EP-0008-US-0001-TK-0002-defense-contract/EP-0008-US-0001-TK-0002-defense-contract.md) — Контракт пуска ПРО, времени жизни и preview | contracts | AC-0002, AC-0003 |
| 3 | [EP-0008-US-0001-TK-0003-weapon-content-schema](EP-0008-US-0001-TK-0003-weapon-content-schema/EP-0008-US-0001-TK-0003-weapon-content-schema.md) — Схема и валидация новых параметров оружия | engine | AC-0001, AC-0002 |
| 4 | [EP-0008-US-0001-TK-0004-weapon-catalog](EP-0008-US-0001-TK-0004-weapon-catalog/EP-0008-US-0001-TK-0004-weapon-catalog.md) — Базовые установки 55/5 и их дальности | content-data | AC-0001, AC-0002 |
| 5 | [EP-0008-US-0001-TK-0005-chance-resolution](EP-0008-US-0001-TK-0005-chance-resolution/EP-0008-US-0001-TK-0005-chance-resolution.md) — Новая формула и captured параметры при пуске | engine | AC-0001, AC-0002, AC-0003 |

## Completion evidence

Все AC доказаны связанными тикетами, проверками реального результата и review. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История draft и не является утверждением о пройденных тестах.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Текущая задача только создаёт план.
