---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0006-defense-tactical-ui
title: "Защита видна на тактической карте"
stage: draft
created: 2026-10-04
revision: 1
current_review: complete
---

# Защита видна на тактической карте

## Вход и user story

Как игрок, я вижу полёт противоракеты, шанс перехвата и состояние аппаратов; могу отключить автоматическую защиту.

Источник: интервью по противоракетам, [решения и assumptions эпика](../Documentation.md). Числовые ответы трактуются в контексте вопроса, не как самостоятельные спецификации.

## Acceptance criteria

- **AC-0001:** Голубая точка5px с glow, сплошной след, пунктирный прогноз и отметка встречи; цвета из JSON с применением после перезапуска.
- **AC-0002:** Шанс0.1% рядом с PR и точкой встречи; tooltip и info раскрывают базы, навыки, рейтинги. PR выбирается, но не является целью торпедного пуска.
- **AC-0003:** Панели обоих аппаратов показывают операторов и навыки; защита имеет переключатель auto и состояния; пиратский статус рядом с label, радиус при выборе пирата.
- **AC-0004:** Успех/промах показан в точке встречи2real seconds; успех имеет единственное красное кольцо. Обычные скрытые следы торпед не возвращаются.

## Ticket map

План draft, не результат реализации. Зависимости и порядок приведены для каждого тикета; они могут пересекать истории.

| Тикет | Шаг | Layer | Критерии |
|---|---|---|---|
| [EP-0007-US-0006-TK-0001-defense-palette](EP-0007-US-0006-TK-0001-defense-palette/EP-0007-US-0006-TK-0001-defense-palette.md) — Редактируемые цвета противоракет | 22 | content-data | AC-0001 |
| [EP-0007-US-0006-TK-0002-defense-visual-settings](EP-0007-US-0006-TK-0002-defense-visual-settings/EP-0007-US-0006-TK-0002-defense-visual-settings.md) — Загрузка настроек изображения защиты | 23 | client | AC-0001 |
| [EP-0007-US-0006-TK-0003-countermeasure-rendering](EP-0007-US-0006-TK-0003-countermeasure-rendering/EP-0007-US-0006-TK-0003-countermeasure-rendering.md) — Полёт, след и результат перехвата | 24 | client | AC-0001, AC-0004 |
| [EP-0007-US-0006-TK-0004-defense-command-panels](EP-0007-US-0006-TK-0004-defense-command-panels/EP-0007-US-0006-TK-0004-defense-command-panels.md) — Панели аппаратов и информация противоракеты | 25 | client | AC-0002, AC-0003 |
| [EP-0007-US-0006-TK-0005-defense-map-annotations](EP-0007-US-0006-TK-0005-defense-map-annotations/EP-0007-US-0006-TK-0005-defense-map-annotations.md) — Шанс, подсказка, радиус и статус пирата | 26 | client | AC-0002, AC-0003 |

## Dependencies и invariants

Исполнять по [общему порядку](../Tickets.md), после реальной готовности зависимостей. Engine authoritative; physical иreal time разделены; результаты не перебрасываются приSaveLoad. Каждое AC покрыто named tests тикетов.

## Non-goals и backlog

Только результат этой истории; соседние реализации не добавлять внеallowlist. Общие исключения и технические допущения находятся в эпике. Native acceptance подтверждается отдельно в US9; статус draft не означает готовность к релизу.
