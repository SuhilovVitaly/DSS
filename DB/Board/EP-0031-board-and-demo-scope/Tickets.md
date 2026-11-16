# EP-0031 — Карта 6 тикетов

[Эпик](Documentation.md). Очередь исполнения совпадает с порядком строк.
Все тикеты — `layer: documentation`, тестового проекта нет (A-04).

| Story / Ticket | Layer | Files | Prerequisites | Criteria | Status |
|---|---|---|---|---|---|
| [US-0001-TK-0001 — Реестр эпиков Board](EP-0031-US-0001-epic-registry/EP-0031-US-0001-TK-0001-board-registry/EP-0031-US-0001-TK-0001-board-registry.md) | documentation | 2 | none | US-0001 AC-01…AC-03 | implemented |
| [US-0002-TK-0001 — Статус EP-0029](EP-0031-US-0002-stale-epic-headers/EP-0031-US-0002-TK-0001-ep0029-status-sync/EP-0031-US-0002-TK-0001-ep0029-status-sync.md) | documentation | 2 | US-0001-TK-0001 | US-0002 AC-01 | implemented |
| [US-0002-TK-0002 — Пометка EP-0021](EP-0031-US-0002-stale-epic-headers/EP-0031-US-0002-TK-0002-ep0021-demo-supersede/EP-0031-US-0002-TK-0002-ep0021-demo-supersede.md) | documentation | 1 | US-0001-TK-0001 | US-0002 AC-02 | pending |
| [US-0003-TK-0001 — Граница демо в требованиях](EP-0031-US-0003-demo-boundary/EP-0031-US-0003-TK-0001-demo-requirements/EP-0031-US-0003-TK-0001-demo-requirements.md) | documentation | 1 | US-0001-TK-0001 | US-0003 AC-01, AC-02 | pending |
| [US-0003-TK-0002 — Дорожная карта демо](EP-0031-US-0003-demo-boundary/EP-0031-US-0003-TK-0002-demo-roadmap/EP-0031-US-0003-TK-0002-demo-roadmap.md) | documentation | 1 | US-0003-TK-0001 | US-0003 AC-03 | pending |
| [US-0004-TK-0001 — Инвентаризация и навигация](EP-0031-US-0004-documentation/EP-0031-US-0004-TK-0001-docs-impact-and-links/EP-0031-US-0004-TK-0001-docs-impact-and-links.md) | documentation | 4 | все тикеты выше | US-0004 AC-01…AC-03 | pending |

Соответствие тикетов коммитам проверяется через `git log --oneline --grep "EP-0031"`;
SHA собственного коммита тикета в карточку заранее не записывается.
