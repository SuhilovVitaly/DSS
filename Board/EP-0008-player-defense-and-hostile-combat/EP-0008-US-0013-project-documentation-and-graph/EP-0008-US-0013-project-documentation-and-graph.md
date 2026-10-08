---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0013-project-documentation-and-graph
title: "Актуальная документация всего проекта"
stage: draft
dependencies: ["EP-0008-US-0001-module-owned-weapon-rules","EP-0008-US-0002-dual-defense-loadout","EP-0008-US-0003-ranged-hostile-torpedo-fire","EP-0008-US-0004-hostile-pursuit-and-orbit","EP-0008-US-0005-manual-countermeasure-fire","EP-0008-US-0006-lifetime-and-auto-arbitration","EP-0008-US-0007-independent-defense-panels","EP-0008-US-0008-manual-preview-and-map","EP-0008-US-0009-authoritative-player-defeat","EP-0008-US-0010-defeat-screen-and-recovery","EP-0008-US-0011-phase-three-save-continuation","EP-0008-US-0012-phase-three-acceptance"]
created: 2026-10-07
revision: 1
current_review: complete
validation_status: planning-only
---

# Актуальная документация всего проекта

## User story

Последняя история синхронизирует документы с фактическим результатом третьей фазы. Она охватывает Documentation и релевантные материалы в других папках, затем обновляет навигационный граф.

## Источник и границы

Решения Обязательная завершающая история по запросу пользователя и EpicExecutionPrompt; полный контекст в [журнале решений](../Decisions.md) и [контракте эпика](../Documentation.md). Технические допущения A01–A12 отделены от пользовательских ответов. Scope ограничен результатом этой истории; смежные механики из Non-goals эпика не добавляются.

## Acceptance criteria

- **AC-0001:** Есть impact inventory всех релевантных документов проекта с причиной изменения и результатом проверки.
- **AC-0002:** Обновлены канонические требования, механики, UI, схемы/save16, Board/README и документы рядом с кодом/тестами/tools; исторические отчёты сохранены с superseded ссылками.
- **AC-0003:** После итоговых правок обновлён Graphify при принятом workflow, проверены ссылки/версии/coverage; готовность реализации/native/push не выдумана.

## Зависимости

- [EP-0008-US-0001-module-owned-weapon-rules](../EP-0008-US-0001-module-owned-weapon-rules/EP-0008-US-0001-module-owned-weapon-rules.md): production-результат зависимости должен быть реализован и проверен до этой истории.
- [EP-0008-US-0002-dual-defense-loadout](../EP-0008-US-0002-dual-defense-loadout/EP-0008-US-0002-dual-defense-loadout.md): production-результат зависимости должен быть реализован и проверен до этой истории.
- [EP-0008-US-0003-ranged-hostile-torpedo-fire](../EP-0008-US-0003-ranged-hostile-torpedo-fire/EP-0008-US-0003-ranged-hostile-torpedo-fire.md): production-результат зависимости должен быть реализован и проверен до этой истории.
- [EP-0008-US-0004-hostile-pursuit-and-orbit](../EP-0008-US-0004-hostile-pursuit-and-orbit/EP-0008-US-0004-hostile-pursuit-and-orbit.md): production-результат зависимости должен быть реализован и проверен до этой истории.
- [EP-0008-US-0005-manual-countermeasure-fire](../EP-0008-US-0005-manual-countermeasure-fire/EP-0008-US-0005-manual-countermeasure-fire.md): production-результат зависимости должен быть реализован и проверен до этой истории.
- [EP-0008-US-0006-lifetime-and-auto-arbitration](../EP-0008-US-0006-lifetime-and-auto-arbitration/EP-0008-US-0006-lifetime-and-auto-arbitration.md): production-результат зависимости должен быть реализован и проверен до этой истории.
- [EP-0008-US-0007-independent-defense-panels](../EP-0008-US-0007-independent-defense-panels/EP-0008-US-0007-independent-defense-panels.md): production-результат зависимости должен быть реализован и проверен до этой истории.
- [EP-0008-US-0008-manual-preview-and-map](../EP-0008-US-0008-manual-preview-and-map/EP-0008-US-0008-manual-preview-and-map.md): production-результат зависимости должен быть реализован и проверен до этой истории.
- [EP-0008-US-0009-authoritative-player-defeat](../EP-0008-US-0009-authoritative-player-defeat/EP-0008-US-0009-authoritative-player-defeat.md): production-результат зависимости должен быть реализован и проверен до этой истории.
- [EP-0008-US-0010-defeat-screen-and-recovery](../EP-0008-US-0010-defeat-screen-and-recovery/EP-0008-US-0010-defeat-screen-and-recovery.md): production-результат зависимости должен быть реализован и проверен до этой истории.
- [EP-0008-US-0011-phase-three-save-continuation](../EP-0008-US-0011-phase-three-save-continuation/EP-0008-US-0011-phase-three-save-continuation.md): production-результат зависимости должен быть реализован и проверен до этой истории.
- [EP-0008-US-0012-phase-three-acceptance](../EP-0008-US-0012-phase-three-acceptance/EP-0008-US-0012-phase-three-acceptance.md): production-результат зависимости должен быть реализован и проверен до этой истории.

## Карта тикетов и покрытия

| Шаг | Тикет | Layer | Serves |
|---|---|---|---|
| 43 | [EP-0008-US-0013-TK-0001-update-project-documents](EP-0008-US-0013-TK-0001-update-project-documents/EP-0008-US-0013-TK-0001-update-project-documents.md) — Обновить релевантные документы во всех папках | documentation | AC-0001, AC-0002, AC-0003 |
| 44 | [EP-0008-US-0013-TK-0002-refresh-graph-and-links](EP-0008-US-0013-TK-0002-refresh-graph-and-links/EP-0008-US-0013-TK-0002-refresh-graph-and-links.md) — Перестроить граф и проверить навигацию финального проекта | tooling | AC-0003 |

## Completion evidence

Все AC доказаны связанными тикетами, проверками реального результата и review. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История draft и не является утверждением о пройденных тестах.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Текущая задача только создаёт план.
