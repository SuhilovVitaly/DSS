---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0003-officer-lessons
title: Офицер «Скитальца» и уроки боя
stage: approved
dependencies: [EP-0028-US-0001-hint-framework, EP-0028-US-0002-comms-panel]
created: 2026-11-14T15:04:43Z
source_request: "US-0003 Офицер и уроки: officer character (name RU/EN, portrait, tone); T1 Move; Fire & salvo; contextual lessons; debrief. RU/EN parity."
current_review: complete
revision: 1
---

# Офицер «Скитальца» и уроки боя

[Эпик](../Documentation.md) · [Тикеты](../Tickets.md)

## Входное техническое задание

Черновик уроков из утверждённого дизайна: T1 Move — выбрать карточку пилота → развернуть путь (конец пути/стрелки) → Enter. T1 Fire — выбрать цель (клик по врагу / список, «Не стрелять») → Enter. Первый залп: Enter пропускает, «Повтор» повторяет. T2: Tab переключает пилотов, зона досягаемости. Контекстные одноразовые: угроза → уклонение/ловушка; урон; низкое сознание; аномалия; потеря дрона. Итог: разбор победы/поражения → «На карту». Принципы: одна механика за ход, вовремя; учить «зачем», а не только кнопки; без стен текста. Решения D-01, D-02, D-06 [эпика](../Documentation.md#решения-пользователя-обязательные).

## User story

Как новый игрок, я слышу по связи одного персонажа — офицера тактической связи носителя «Скиталец». В первом ходу она коротко объясняет, как выбрать пилота, проложить курс и отправить ход, затем — как выбрать цель и пережить залп. Во втором ходу добавляет переключение пилотов и зону досягаемости, а в опасных ситуациях напоминает про уклонение, ловушки, сознание и аномалии. После боя она подводит итог и направляет на карту. Все реплики короткие, на языке игры и помещаются в панель.

## Acceptance criteria

- **AC-01.** Офицер: имя и должность RU/EN, портрет из существующих ассетов через композитор, гайд тона; приветствие в первом ходу.
- **AC-02.** Уроки движения T1 (выбор пилота, курс, отправка) и T2 (Tab, зона досягаемости).
- **AC-03.** Уроки стрельбы (цель, «Не стрелять», отправка) и залпа (пропуск, «Повтор», итоги).
- **AC-04.** Контекстные уроки: под огнём, урон, низкое сознание, аномалия, потеря дрона.
- **AC-05.** Разбор итога: победа, поражение, иное → «На карту».
- **AC-06.** Все реплики есть в RU и EN (паритет), ≤ 2 строк панели в обоих языках, цитируемые надписи UI совпадают с реальными метками текущего языка.

## Non-goals

Новый код (вся логика — US-0001/US-0002); новые бинарные арты; подсказки в боях F8; «Повторить обучение» (D-05).

## Гайд по тону офицера

- **Кто.** Лейтенант Ирина Вольская (A-05), офицер тактической связи «Скитальца». Спокойная, собранная, доброжелательная; уважает командира и не опекает.
- **Обращение.** «Командир» / "Commander"; на «вы» в RU.
- **Форма.** Одна мысль на реплику, ≤ 2 строк панели (≈110 символов). Сначала действие, потом короткое «зачем». Повелительное наклонение без восклицаний, кроме победы.
- **Названия.** Кнопки и метки — дословно как в UI текущего языка («Не стрелять»/"Hold fire", «Повтор»/"Replay", «На карту»/"Back to map", «Уклониться»/"Evade", «Ловушка»/"Decoy").
- **Запреты.** Без стен текста, без жаргона, без шуток в бою (лёгкая теплота — только в разборе), без чисел баланса (они меняются в `rules.json`), без упоминания клавиши E до подтверждения G-01.
- **EN.** Тот же смысл, а не дословный перевод; носитель — "Skitalets" (A-05).

## Каталог уроков

| Шаг | Тикет | when | complete | anchor | prio |
|---|---|---|---|---|---|
| `intro` | TK-0001 | Move, turn 1 | pilot-selected | `pilot:{own}` | 100 |
| `move.steer` | TK-0002 | Move, turn 1, after intro | path-edited | `endpoint:{selected}` | 90 |
| `move.submit` | TK-0002 | Move, turn 1, after move.steer | phase-left | `button:primary` | 80 |
| `move.cycle` | TK-0002 | Move, turn 2 | pilot-cycled | `pilot:{own}` | 70 |
| `move.reachable` | TK-0002 | Move, minTurn 2, after move.cycle | path-edited | `reachable` | 60 |
| `fire.target` | TK-0003 | Fire, turn 1 | target-chosen | `candidate:{any}` | 90 |
| `fire.submit` | TK-0003 | Fire, turn 1, after fire.target | phase-left | `button:primary` | 80 |
| `salvo.watch` | TK-0003 | Salvo, turn 1 | phase-left | `button:primary` | 90 |
| `results.read` | TK-0003 | Results, turn 1 | phase-left | `button:replay` | 80 |
| `ctx.under-fire` | TK-0004 | Fire, minTurn 2, signal under-fire | evade-or-decoy | `pilot:{signal}` | 85 |
| `ctx.damage` | TK-0004 | Results, minTurn 1, signal own-damaged | acknowledged | `pilot:{signal}` | 70 |
| `ctx.low-mind` | TK-0004 | Fire, minTurn 2, signal low-consciousness | acknowledged | `pilot:{signal}` | 75 |
| `ctx.anomaly` | TK-0004 | Move, minTurn 2, signal anomaly-present | acknowledged | `pilot:{signal}` | 65 |
| `ctx.drone-lost` | TK-0004 | Results, minTurn 1, signal drone-lost | acknowledged | `pilot:{signal}` | 95 |
| `over.victory` | TK-0005 | Outcome, signal outcome-victory | acknowledged | `button:primary` | 100 |
| `over.defeat` | TK-0005 | Outcome, signal outcome-defeat | acknowledged | `button:primary` | 100 |
| `over.other` | TK-0005 | Outcome, signal outcome-other | acknowledged | `button:primary` | 100 |

Ключ реплики — `Tutorial.Line.<PascalCase id>` (например `Tutorial.Line.MoveSteer`, `Tutorial.Line.CtxUnderFire`, `Tutorial.Line.OverVictory`).

## Dependencies

US-0001 (формат, директор, сигналы, интеграция) и US-0002 (панель с шириной текста `TutorialCommsPanel.TextWidth`, подсветка, правила тишины, тест паритета).

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](EP-0028-US-0003-TK-0001-officer/EP-0028-US-0003-TK-0001-officer.md) | Офицер «Скитальца» | content-data | US-0002-TK-0002, US-0002-TK-0001 | AC-01, AC-06 |
| [TK-0002](EP-0028-US-0003-TK-0002-move-lessons/EP-0028-US-0003-TK-0002-move-lessons.md) | Уроки движения | content-data | TK-0001, US-0002-TK-0003, US-0002-TK-0004 | AC-02, AC-06 |
| [TK-0003](EP-0028-US-0003-TK-0003-fire-lessons/EP-0028-US-0003-TK-0003-fire-lessons.md) | Уроки стрельбы и залпа | content-data | TK-0002 | AC-03, AC-06 |
| [TK-0004](EP-0028-US-0003-TK-0004-context-lessons/EP-0028-US-0003-TK-0004-context-lessons.md) | Контекстные уроки | content-data | TK-0003 | AC-04, AC-06 |
| [TK-0005](EP-0028-US-0003-TK-0005-debrief/EP-0028-US-0003-TK-0005-debrief.md) | Разбор итога боя | content-data | TK-0004 | AC-05, AC-06 |

Карта принята автоматическим workflow.

## Gaps and backlog

- G-01: клавиша E в репликах не упоминается до нативного подтверждения (US-0006-TK-0002).
- Тексты — предложение; narrative-designer может уточнить формулировки в пределах гайда и тестов без смены ключей.

## Completion evidence

`TutorialContentTests` и `TutorialLessonFlowTests` зелёные; паритет RU/EN; растровые снимки панели в RU и EN для шагов каждого тикета в `D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/`.

## Журнал

2026-11-14T15:04:43Z — история создана по утверждённому плану.
