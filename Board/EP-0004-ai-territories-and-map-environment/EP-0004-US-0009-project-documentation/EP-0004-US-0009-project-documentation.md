---
epic: EP-0004-ai-territories-and-map-environment
story: EP-0004-US-0009-project-documentation
title: Актуальная документация полной карты
stage: done
dependencies: [EP-0004-US-0001-hostile-ai-bases, EP-0004-US-0002-moving-ai-territories, EP-0004-US-0003-trade-compatible-ai-placement, EP-0004-US-0004-informational-environment-fields, EP-0004-US-0005-known-points-of-interest, EP-0004-US-0006-readable-map-layers, EP-0004-US-0007-resume-territories-and-fields, EP-0004-US-0008-complete-map-evidence]
---

# Актуальная документация полной карты

Как разработчик, я хочу воспроизвести фактическое поведение полной карты, её ограничения и проверки по согласованным документам проекта.

История добавлена по прямому требованию раздела 6 пользовательского EpicExecutionPrompt. Выполняется последней. Статус approved означает разрешённый scope; проверка реализации остаётся открытой.

## Acceptance criteria

- AC-0001: По итоговому diff/API/config/schema составлен перечень всех затронутых документов внутри и вне Documentation с результатом проверки каждого.
- AC-0002: Требования, механики, экраны, архитектура, сохранения, конфигурация, сценарии, Board связанных эпиков и значимые комментарии согласованы с кодом. Датированные результаты сохранены как история; отменённые решения имеют ссылки на актуальные.
- AC-0003: Board, покрытие критериев, ссылки, количества и таблица публикаций точны; граф перестроен после окончательных изменений, его ограничения и фактические automated/native результаты доступны. Нет неопубликованных изменений эпика.

## Tickets

1. [TK-0001 — документация проекта](EP-0004-US-0009-TK-0001-sync-project-docs/EP-0004-US-0009-TK-0001-sync-project-docs.md).
2. [TK-0002 — итоговые evidence и граф](EP-0004-US-0009-TK-0002-final-evidence-and-graph/EP-0004-US-0009-TK-0002-final-evidence-and-graph.md).

Изменения, необходимые для актуальности документов, разрешены исходным запросом. Несвязанные переписывания исключены. Каждый тикет имеет отдельный коммит и push.

## Execution — 2026-10-08

TK-0001 project documentation synchronized; relative links/scoped format/diff checked. TK-0002 final Board/SHA registry/graph follows. Native performance OPEN in US8 is preserved, not blocked on documentation.

## Final story review — 2026-10-08

AC1 inventory and source cross-check complete; AC2 current documentation/historical handoffs synchronized; AC3 Board/SHA registry/links and final graph validated. Final publication SHA checked after commit. FPS acceptance remains OPEN in US8 TK3; story completion covers documentation, not independent runtime approval.
