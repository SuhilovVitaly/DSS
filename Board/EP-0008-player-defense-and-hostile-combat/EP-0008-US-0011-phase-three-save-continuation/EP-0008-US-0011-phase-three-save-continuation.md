---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0011-phase-three-save-continuation
title: "Новые сохранения продолжают весь бой без миграции старых"
stage: draft
dependencies: ["EP-0008-US-0010-defeat-screen-and-recovery"]
created: 2026-10-07
revision: 1
current_review: complete
validation_status: planning-only
---

# Новые сохранения продолжают весь бой без миграции старых

## User story

Сохранение новой игры восстанавливает обе установки, активные снаряды и поведение противников. Старые сохранения явно отклоняются, а стартовые сценарии остаются доступными.

## Источник и границы

Решения Q14,Q25,Q28,Q42,Q49; полный контекст в [журнале решений](../Decisions.md) и [контракте эпика](../Documentation.md). Технические допущения A01–A12 отделены от пользовательских ответов. Scope ограничен результатом этой истории; смежные механики из Non-goals эпика не добавляются.

## Acceptance criteria

- **AC-0001:** Новый writer version16 на проверенном baseline15; старые save files отвергаются до mutation с сообщением, сценарии version0 New Game не блокируются.
- **AC-0002:** Roundtrip сохраняет mode/ttl/frozen55-5/chance/roll/RNG/shared attempt, две module states, команды/ID/журнал/selection, AI phase/handedness/next boundary/segments.
- **AC-0003:** Восстановление атомарно: invalid refs/phase/lifetime/ratings/AI/time/versions не меняют живой мир или slot bytes.
- **AC-0004:** Current-format pre-death saves продолжают физику/экономику без reroll, повторного пуска и replay UI; world maps/финансы v15 не теряются.

## Зависимости

- [EP-0008-US-0010-defeat-screen-and-recovery](../EP-0008-US-0010-defeat-screen-and-recovery/EP-0008-US-0010-defeat-screen-and-recovery.md): production-результат зависимости должен быть реализован и проверен до этой истории.

## Карта тикетов и покрытия

| Шаг | Тикет | Layer | Serves |
|---|---|---|---|
| 35 | [EP-0008-US-0011-TK-0001-new-save-version-gate](EP-0008-US-0011-TK-0001-new-save-version-gate/EP-0008-US-0011-TK-0001-new-save-version-gate.md) — Новая версия и явный отказ старым сохранениям | engine | AC-0001, AC-0003 |
| 36 | [EP-0008-US-0011-TK-0002-save-combat-validation](EP-0008-US-0011-TK-0002-save-combat-validation/EP-0008-US-0011-TK-0002-save-combat-validation.md) — Полная схема и preflight боя новой фазы | engine | AC-0002, AC-0003 |
| 37 | [EP-0008-US-0011-TK-0003-save-runtime-continuation](EP-0008-US-0011-TK-0003-save-runtime-continuation/EP-0008-US-0011-TK-0003-save-runtime-continuation.md) — Атомарное восстановление активного боя и AI | engine | AC-0002, AC-0003, AC-0004 |
| 38 | [EP-0008-US-0011-TK-0004-load-ui-and-transport](EP-0008-US-0011-TK-0004-load-ui-and-transport/EP-0008-US-0011-TK-0004-load-ui-and-transport.md) — Файловая загрузка и понятное сообщение несовместимости | client | AC-0001, AC-0002, AC-0003, AC-0004 |

## Completion evidence

Все AC доказаны связанными тикетами, проверками реального результата и review. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История draft и не является утверждением о пройденных тестах.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Текущая задача только создаёт план.
