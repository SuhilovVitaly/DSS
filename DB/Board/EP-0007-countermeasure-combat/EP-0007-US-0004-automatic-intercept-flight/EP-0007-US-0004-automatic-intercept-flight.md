---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0004-automatic-intercept-flight
title: "Пират автоматически отвечает противоракетой"
stage: draft
created: 2026-10-04
revision: 1
current_review: complete
---

# Пират автоматически отвечает противоракетой

## Вход и user story

Как атакующий игрок, я вижу ответный запуск защиты при входе торпеды в радиус100км. Противоракета летит с упреждением к моей торпеде. Аппарат занят до окончания полёта, затем перезаряжается10секунд.

Источник: интервью по противоракетам, [решения и assumptions эпика](../Documentation.md). Числовые ответы трактуются в контексте вопроса, не как самостоятельные спецификации.

## Acceptance criteria

- **AC-0001:** Только incoming torpedo.TargetObjectId==defenderId, расстояние<=100км, шанс>0, operator/ready/autoEnabled и встреча до прогнозируемого попадания.
- **AC-0002:** Пуск только при продвижении MotionTimeMs; pending intruder после reload обрабатывается сразу, пока ещё внутри радиуса.
- **AC-0003:** PR стартует из центра по курсу носителя,12км/с,90°/с; преследует за пределами радиуса; другие объекты не сталкиваются с PR.
- **AC-0004:** Одна попытка резервируется при пуске навсегда для этой торпеды; нет range/TTL/ammo, запрет второго simultaneous PR на аппарат.

## Ticket map

План draft, не результат реализации. Зависимости и порядок приведены для каждого тикета; они могут пересекать истории.

| Тикет | Шаг | Layer | Критерии |
|---|---|---|---|
| [EP-0007-US-0004-TK-0001-interceptor-guidance](EP-0007-US-0004-TK-0001-interceptor-guidance/EP-0007-US-0004-TK-0001-interceptor-guidance.md) — Общая геометрия перехвата движущейся торпеды | 17 | motion | AC-0001, AC-0003 |
| [EP-0007-US-0004-TK-0002-chance-and-named-rng](EP-0007-US-0004-TK-0002-chance-and-named-rng/EP-0007-US-0004-TK-0002-chance-and-named-rng.md) — Формула и воспроизводимый именованный поток бросков | 18 | engine | AC-0001, AC-0004 |
| [EP-0007-US-0004-TK-0003-automatic-defense-scheduler](EP-0007-US-0004-TK-0003-automatic-defense-scheduler/EP-0007-US-0004-TK-0003-automatic-defense-scheduler.md) — Авторитетная автозащита и перезарядка | 19 | engine | AC-0001, AC-0002, AC-0003, AC-0004 |

## Dependencies и invariants

Исполнять по [общему порядку](../Tickets.md), после реальной готовности зависимостей. Engine authoritative; physical иreal time разделены; результаты не перебрасываются приSaveLoad. Каждое AC покрыто named tests тикетов.

## Non-goals и backlog

Только результат этой истории; соседние реализации не добавлять внеallowlist. Общие исключения и технические допущения находятся в эпике. Native acceptance подтверждается отдельно в US9; статус draft не означает готовность к релизу.
