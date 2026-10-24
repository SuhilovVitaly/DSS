---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0006-lifetime-and-auto-arbitration
title: "Ограниченный полёт и согласованная автоматика двух установок"
stage: implemented
dependencies: ["EP-0008-US-0005-manual-countermeasure-fire"]
created: 2026-10-07
revision: 1
current_review: complete
validation_status: automated-pass; native-open
---

## Актуализация EP-0008 — 2026-10-12

Функциональные тикеты реализованы, проверены и опубликованы; self-review завершено. [Фактическое evidence](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/04-Engineering/PhaseThreeCombatAcceptance.md), [ticket → SHA → push](../PublicationStatus.md). `stage: draft` — исходная метка планирования, не утверждение о пользовательском APPROVED. Дальнейшие game/native запуски отложены пользователем.


# Ограниченный полёт и согласованная автоматика двух установок

## User story

Все противоракеты имеют ограниченное время полёта из своего модуля. Автоматика выбирает лучший по времени перехвата доступный аппарат и не дублирует автоматическую попытку.

## Источник и границы

Решения Q08–Q09,Q12–Q15,Q35,Q37,Q47–Q48; полный контекст в [журнале решений](../Decisions.md) и [контракте эпика](../Documentation.md). Технические допущения A01–A12 отделены от пользовательских ответов. Scope ограничен результатом этой истории; смежные механики из Non-goals эпика не добавляются.

## Acceptance criteria

- **AC-0001:** Lifetime60s captured на старте действует у всех ПРО, включая MissedCoast; timeout без контакта не делает draw, удаляет снаряд и начинает10s reload.
- **AC-0002:** При равном времени hull impact раньше interception, interception раньше timeout; несколько ПРО разрешаются стабильно, потеря цели не даёт второго draw.
- **AC-0003:** Auto candidate выбирается по времени встречи, затем большему шансу, затем module ID; одна общая attempt при любом первом spawn.
- **AC-0004:** Ручной0%/100% контакт потребляет один draw; repeat/timeout/lost target не перебрасывают результат; полёт/coast/reload/pause разделены.

## Зависимости

- [EP-0008-US-0005-manual-countermeasure-fire](../EP-0008-US-0005-manual-countermeasure-fire/EP-0008-US-0005-manual-countermeasure-fire.md): production-результат зависимости должен быть реализован и проверен до этой истории.

## Карта тикетов и покрытия

| Шаг | Тикет | Layer | Serves |
|---|---|---|---|
| 20 | [EP-0008-US-0006-TK-0001-global-lifetime](EP-0008-US-0006-TK-0001-global-lifetime/EP-0008-US-0006-TK-0001-global-lifetime.md) — Абсолютный deadline полёта во всех фазах ПРО | engine | AC-0001, AC-0002, AC-0004 |
| 21 | [EP-0008-US-0006-TK-0002-auto-launcher-selection](EP-0008-US-0006-TK-0002-auto-launcher-selection/EP-0008-US-0006-TK-0002-auto-launcher-selection.md) — Выбор одного аппарата для автоматического перехвата | engine | AC-0003, AC-0004 |
| 22 | [EP-0008-US-0006-TK-0003-combat-journal-facts](EP-0008-US-0006-TK-0003-combat-journal-facts/EP-0008-US-0006-TK-0003-combat-journal-facts.md) — Журнал ручных пусков и истечения времени полёта | engine | AC-0001, AC-0002, AC-0004 |

## Completion evidence

Покрытие AC и фактические ограничения приведены в текущем execution status и acceptance report; наличие запланированных проверок само по себе не является evidence. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История реализована; фактические проверки и первоначальные failures описаны в карточках.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Автономное исполнение выполнено; публикация сверяется через Git.

## Story execution self-review — 2026-10-11

AC-0001: captured absolute lifetime covers Guiding/MissedCoast, configurable/full60s tests, no-roll timeout and exact reload. AC-0002: hull/contact/expiry ties and stable concurrent target resolution tested. AC-0003: earliest threat then actual encounter/chance/ordinal module candidate comparison and successful-spawn-only shared attempt tested. AC-0004: actual0%/100% contact draws, manual repeats, two concurrent flights, pause, terminal removal facts and save journal continuation tested. No unresolved finding in story scope. This is self-review, not user approval; new-format validation, UI presentation, final corpus and native acceptance remain assigned downstream.
