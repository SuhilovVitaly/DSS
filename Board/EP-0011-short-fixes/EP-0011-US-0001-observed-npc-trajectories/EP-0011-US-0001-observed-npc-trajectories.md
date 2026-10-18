---
epic: EP-0011-short-fixes
story: EP-0011-US-0001-observed-npc-trajectories
title: Прогноз движения НПС по наблюдениям игрока
stage: approved
dependencies: []
created: 2026-10-18T07:47:19+00:00
source_request: "Два одинаковых секундных интервала подтверждают тенденцию; иначе прямая по текущему курсу"
current_review: complete
revision: 1
---

# US-0001 — Прогноз движения НПС по наблюдениям игрока

## Входное техническое задание

[Эпик](../EP-0011-short-fixes.md) · [Grounding, исходные сообщения D-01–D-03 и assumptions A-01–A-08](../Documentation.md).

## User story

Как игрок, я хочу видеть прогноз NPC по наблюдавшемуся на карте движению. Два одинаковых секундных изменения курса подтверждают продолжение поворота, два прямых интервала — прямую. До подтверждения либо при несовпадении тенденции прогноз идёт прямо по текущему курсу. Скрытые решения NPC не раскрываются этой линией до появления их последствий в наблюдениях.

## Acceptance criteria

- **AC-01**: Одинаковые наблюдения дают одинаковую NPC-линию при любых скрытых phase/mode/controls/route и будущих пакетах.
- **AC-02**: Два последовательных равных signed изменения курса за физическую секунду подтверждают продолжение поворота с тем же знаком/rate.
- **AC-03**: Два прямых интервала дают прямую; недостаток наблюдений или несовпадение немедленно дают прямую по текущему курсу, вариант А.
- **AC-04**: Пауза/FPS/скорость симуляции/частота packets не подменяют секунды; wrap 359→0 и разрывы обработаны по A-01–A-03.
- **AC-05**: Линия начинается в отображаемой позе с текущей наблюдаемой скоростью; горизонт, шаг, стиль и viewport поведение соответствуют A-04/A-05.
- **AC-06**: Lifecycle reset соответствует A-06; selection/камера не влияют, память ограничена A-07.
- **AC-07**: Игрок, орбиты/станции, реальное движение NPC, AI/RNG/saves/guidance и combat forecast/cancellation не меняются.
- **AC-08**: AC покрыты независимыми headless unit/integration tests; native/human gate отдельно NOT RUN до фактической проверки.

## Non-goals / invariants

Только Client. Без Engine/Contracts/Motion changes, AI, guidance, save schema, оружия, прогнозов попадания, орбитальных объектов и player navigation. Будущая линия не изменяет подтверждённую сцену/боевые исходы. Игру автоматически не запускать.

## Dependencies

Внешних story dependencies нет; фактический baseline в Documentation.md. Открытые native/balance gates предшественников не считаются закрытыми этой историей.

## Approved ticket map

| Order / ID | Layer | Dependencies | Served criteria |
|---|---|---|---|
| 1 / [EP-0011-US-0001-observed-npc-trajectories-TK-0001-observation-history](EP-0011-US-0001-observed-npc-trajectories-TK-0001-observation-history/EP-0011-US-0001-observed-npc-trajectories-TK-0001-observation-history.md) | client | none | AC-01–04, AC-06 |
| 2 / [EP-0011-US-0001-observed-npc-trajectories-TK-0002-map-forecast-integration](EP-0011-US-0001-observed-npc-trajectories-TK-0002-map-forecast-integration/EP-0011-US-0001-observed-npc-trajectories-TK-0002-map-forecast-integration.md) | client | TK-0001 | AC-01–07 |
| 3 / [EP-0011-US-0001-observed-npc-trajectories-TK-0003-regression-coverage](EP-0011-US-0001-observed-npc-trajectories-TK-0003-regression-coverage/EP-0011-US-0001-observed-npc-trajectories-TK-0003-regression-coverage.md) | client | TK-0001, TK-0002 | AC-01–08 |

## Assumptions

A-01–A-08 из Documentation.md обязательны для реализации; решения пользователя отдельно D-01–D-03. Source signatures перепроверить перед execution, расхождения/необходимый дополнительный scope фиксировать явно.

## Gaps and backlog

Блокирующих вопросов нет. Параметры выборки/допуска и постоянная скорость — технические assumptions. Согласование прогнозов оружия с наблюдаемой целью, persist observation history и глобальная privacy DTO вне scope. Native smoke требует отдельного разрешения на запуск игры и остаётся OPEN/NOT RUN.

## Decision / review log

2026-10-18T07:47:19+00:00: прямое разрешение пользователя на Board без requirements-engineer, вариант А подтверждён. Grounding и карта трёх тикетов готовы. current_review=complete относится к планированию. Все тикеты NOT STARTED; implementation/acceptance не выполнены.
