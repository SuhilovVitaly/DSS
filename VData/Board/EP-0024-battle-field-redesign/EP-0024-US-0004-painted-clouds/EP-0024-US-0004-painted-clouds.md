---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0004-painted-clouds
title: Облака жара и пыли «краской»
stage: approved
dependencies: [EP-0024-US-0001]
created: 2026-11-12T13:00:00Z
source_request: "Рисовать в режиме облака аномалий из сгустков боя слоями размывки и ядра с шумом и зерном, с кэшем."
current_review: epic
revision: 1
---

# Облака жара и пыли «краской»

## Входное техническое задание

D-01, A-07. Сейчас — `AnomalyFieldLayer.cs`, `InteractiveAnomalyField.cs`, вызов `BattleMapRenderer.cs:51-52`, `:265-269`; повтор облаков в секторе — `BattleMapRenderer.Fire.cs:22-24`. Прототип — `DB/Board/EP-0024-battle-field-redesign/Materials/Prototype/src`/BattleMapRenderer.PaintedClouds.cs (без кэша).

## User story

Как игрок, я хочу видеть жар и пыль мягкими облаками в стиле поля, чтобы они читались как местность, не споря с кораблями.

## Dependencies

[EP-0024-US-0001](../EP-0024-US-0001-skin-pack/EP-0024-US-0001-skin-pack.md)

## Completion evidence

- Облака по UiSpec §4.1 с кэшем §4.2.

## Acceptance criteria

- **US4-AC1.** В режиме облака рисуются по UiSpec §4.1 из `Battle.Anomalies` (пыль, затем жар; размывка и ядро с шумом; зерно на ядре); пробы §10.5 проходят; облака внутри сектора повторно не рисуются; наведение на облако и карточка аномалии работают как сейчас.
- **US4-AC2.** Облака кэшируются по ключу (камера, масштаб, сгустки): повторный кадр без изменений не пересобирает кэш; при смене камеры кэш пересобирается не чаще раза за кадр; время пересборки при 1920×1080 ≤ 12 ms записано в ExecutionStatus.
- **US4-AC3.** В Classic, Contrast и Xenon Star облака рисует прежний `AnomalyFieldLayer`/`InteractiveAnomalyField` без изменений.

## Non-goals

Правила аномалий и их данные.

## Invariants

Сгустки, их параметры и поглощение не меняются.

## Решения и согласование

Решения пользователя — [D-01–D-11](../Documentation.md#решения-пользователя), допущения — [A-01–A-19](../Documentation.md#технические-допущения). Эпик утверждён пользователем 2026-11-12 ([review log](../Documentation.md#review-log)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0024-US-0004-TK-0001-clouds-render](EP-0024-US-0004-TK-0001-clouds-render/EP-0024-US-0004-TK-0001-clouds-render.md) | Отрисовка облаков в режиме | client-ui | `EP-0024-US-0001-TK-0002-tabletop-mode` | US4-AC1, US4-AC3 |
| [EP-0024-US-0004-TK-0002-clouds-cache](EP-0024-US-0004-TK-0002-clouds-cache/EP-0024-US-0004-TK-0002-clouds-cache.md) | Кэш облаков и бюджет времени | client-ui | `EP-0024-US-0004-TK-0001-clouds-render` | US4-AC2 |


## Итог исполнения

Продолжение US4-TK2 уменьшило первую сборку камеры движения с20.609ms до7.49ms. Центр облака13.7655ms и обзор177 сгустков81.275ms превышают бюджет; история остаётся PARTIAL. Проверки32/32, native46/46; подробности — [CloudOptimization](../CloudOptimization.md).

PARTIAL: rendering/cache работают; бюджет12ms не выполнен. Критерии и границы приёмки — [AcceptanceCoverage](../AcceptanceCoverage.md), проверки и SHA — [ExecutionStatus](../ExecutionStatus.md). Физический ввод и owner acceptance отдельно, не заменены автоматизацией.
