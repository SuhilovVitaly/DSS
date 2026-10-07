---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0010-defeat-screen-and-recovery
title: "Окно поражения и загрузка живого состояния"
stage: draft
dependencies: ["EP-0008-US-0009-authoritative-player-defeat"]
created: 2026-10-07
revision: 1
current_review: complete
validation_status: planning-only
---

# Окно поражения и загрузка живого состояния

## User story

Игрок получает понятное окно поражения с двумя действиями. Загрузка старого по времени, но совместимого сохранения до гибели создаёт нормальную живую сессию; выход возвращает главное меню.

## Источник и границы

Решения Q27–Q28,Q49; полный контекст в [журнале решений](../Decisions.md) и [контракте эпика](../Documentation.md). Технические допущения A01–A12 отделены от пользовательских ответов. Scope ограничен результатом этой истории; смежные механики из Non-goals эпика не добавляются.

## Acceptance criteria

- **AC-0001:** Окно открывается один раз по authoritative outcome, имеет только Загрузить/Главное меню; Escape/фон/Resume не закрывают его в живой gameplay.
- **AC-0002:** Сохранение/quick-save недоступны после гибели; существующие слоты не повреждаются. Cancel/ошибка Load возвращает к поражению.
- **AC-0003:** Load совместимого pre-death save заменяет сессию и сбрасывает terminal/UI/pending/effects; main menu освобождает сессию.

## Зависимости

- [EP-0008-US-0009-authoritative-player-defeat](../EP-0008-US-0009-authoritative-player-defeat/EP-0008-US-0009-authoritative-player-defeat.md): production-результат зависимости должен быть реализован и проверен до этой истории.

## Карта тикетов и покрытия

| Шаг | Тикет | Layer | Serves |
|---|---|---|---|
| 32 | [EP-0008-US-0010-TK-0001-defeated-session-save-guard](EP-0008-US-0010-TK-0001-defeated-session-save-guard/EP-0008-US-0010-TK-0001-defeated-session-save-guard.md) — Защита файлового сохранения и session lifecycle | engine-localclient | AC-0002, AC-0003 |
| 33 | [EP-0008-US-0010-TK-0002-defeat-modal](EP-0008-US-0010-TK-0002-defeat-modal/EP-0008-US-0010-TK-0002-defeat-modal.md) — Экран поражения с двумя действиями | client | AC-0001 |
| 34 | [EP-0008-US-0010-TK-0003-defeat-navigation](EP-0008-US-0010-TK-0003-defeat-navigation/EP-0008-US-0010-TK-0003-defeat-navigation.md) — Открытие поражения, загрузка и выход через реальную оболочку | client | AC-0001, AC-0002, AC-0003 |

## Completion evidence

Все AC доказаны связанными тикетами, проверками реального результата и review. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История draft и не является утверждением о пройденных тестах.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Текущая задача только создаёт план.
