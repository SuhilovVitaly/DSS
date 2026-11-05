---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0004-battle-ai
title: "ИИ сторон и сверка с эталонными боями"
stage: done
dependencies: [EP-0015-US-0003-battle-rules]
---

# ИИ сторон и сверка с эталонными боями

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как игрок, я воюю против ИИ, который ведёт себя как справочная реализация: тактики и роли, погоня, заходы ударников, заслон перехватчиков, командир по угрозам, выход в хвост и выбор цели — и каждый эталонный бой в игре идёт так же, бросок в бросок.

## Зависимости

[EP-0015-US-0003-battle-rules](../EP-0015-US-0003-battle-rules/EP-0015-US-0003-battle-rules.md)

## Completion evidence

- **AC-01.** steer (привязь меняет только точку; точка середины для привязи, `wing4.py:320`), slowForTurn (шаги из ai.json; `vmax ≤ vmin → turn`, `wing4.py:273`), predict, pursue (2 броска: x, затем y), «действующая роль» (истребитель/перехватчик без врагов-дронов — ударник) — Generation §3.1–3.2. Каждый пункт — свой тест.

- **AC-02.** Ударник (гистерезис in/out и точка отхода на vmax, `wing4.py:501-508`); истребитель (extend, break, `hunt` переживает смену роли, цель не в бою отбрасывается); выход в хвост (сетка ns/nt, строго меньший score, 1 бросок только при находке, у заклинившего — ничего, флаг `takesTail`, `wing4.py:415`) — §3.3, §3.5, §3.7.

- **AC-03.** Угрозы (радиус или курс + 5 ходов на vmax; порядок — D, затем порядок списка, `wing4.py:522`), перехватчик (враг у носителя ≤ `patrolRadius` → погоня за ближайшим, `engageFactor`, `screenHold` → носом к угрозе на vmin, `wing4.py:531-548`), заслон (знак по чётности индекса), патруль (шаг ≤ 0.8 предела; предел при «сырой» `patrolSpeed` без `clamp_speed`, `wing4.py:550`), командир (`startTactic`, `minRoleTurns`, ключ распределения), переключение «только перехватчики» с записью журнала, выбор цели (группа по роли самого дрона) — §3.4, §3.6, §3.8. Выведенные и сбитые ИИ не видит.

- **AC-04.** **Все 90 боёв `battles.json`** (исход, структурная причина, ходы, потери, корпус носителей, вся статистика, `roleChanges`, броски после сборки и после каждого этапа) и **весь `trace-seed-1.json`** (каждый этап, бит в бит) совпадают на Windows и Linux. Сообщение о расхождении называет бой (сид, тактики), ход и этап — по первому расхождению числа бросков; для сида 1 — ещё объект и поле.

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Зависит от G-03 (US-0002 TK-0006).

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0004-TK-0001-steering](EP-0015-US-0004-TK-0001-steering/EP-0015-US-0004-TK-0001-steering.md) | engine | AC-01 |
| [EP-0015-US-0004-TK-0002-striker-fighter-tail](EP-0015-US-0004-TK-0002-striker-fighter-tail/EP-0015-US-0004-TK-0002-striker-fighter-tail.md) | engine | AC-02 |
| [EP-0015-US-0004-TK-0003-interceptor-commander](EP-0015-US-0004-TK-0003-interceptor-commander/EP-0015-US-0004-TK-0003-interceptor-commander.md) | engine | AC-03 |
| [EP-0015-US-0004-TK-0004-golden-battles](EP-0015-US-0004-TK-0004-golden-battles/EP-0015-US-0004-TK-0004-golden-battles.md) | engine | AC-04 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.

## Execution result

Four tickets complete. All 90 independent summaries and continuous 19-turn seed-one trace pass on Windows and Linux under D-USER-14/15 source precision. Evidence: TK-0004 execution sections and PlatformValidation.md. No new expected outcomes were generated from the Engine; session/UI work remains dependent.
