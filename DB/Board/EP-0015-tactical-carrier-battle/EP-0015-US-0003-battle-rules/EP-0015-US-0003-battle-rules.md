---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0003-battle-rules
title: "Правила боя: сборка, движение, залп, урон, исход"
stage: done
dependencies: [EP-0015-US-0002-deterministic-core, EP-0015-US-0001-content-and-loader]
---

# Правила боя: сборка, движение, залп, урон, исход

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как игрок, я веду бой по правилам V4: рамы с модулями, лимиты от массы, одновременный залп с шансом в стиле Battle Brothers, броня-пул, корма, выведение из строя, повреждения систем и исход — ровно как в `VData/Planning/V4.0/Spec/Combat.md`.

## Зависимости

[EP-0015-US-0002-deterministic-core](../EP-0015-US-0002-deterministic-core/EP-0015-US-0002-deterministic-core.md), [EP-0015-US-0001-content-and-loader](../EP-0015-US-0001-content-and-loader/EP-0015-US-0001-content-and-loader.md)

## Completion evidence

- **AC-01.** Сборка боя: порядок объектов, поверхностное переопределение компоновки (без повторной проверки слотов, как `wing4.py:203-204`), броски навыков NPC, разброс, стартовая скорость, начальные роли (Generation §2). После сборки «Начального боя» — `Mulberry32.Calls == 42`; таблица «Сборка» `trace-seed-1.json` совпадает.

- **AC-02.** Движение по подшагам с лимитами Motion совпадает с позициями и курсами `trace-seed-1.json` при эталонных приказах; disabled/destroyed стоят.

- **AC-03.** Залп: `dry` обновляется в начале стрельбы для всех дронов в бою; кандидаты; шанс — `TacticalHitChance`; броски 1/2/3 на выстрел; бросок гибели при любом попадании в корму; урон по целям (порядок целей — упорядоченная уникализация), split брони, гибель в корму; состояния destroyed/disabled (скорость 0); системы — отдельно на каждое подходящее попадание, «ничего не изменилось» без записи; носитель — корпус и манёвр рамы `carrier`, `hull` компоновки игнорируется. Совпадает с залпами `trace-seed-1.json`.

- **AC-04.** Ход и исход: проверки (носитель первым); причины — типы `CarrierDestroyed(carrierId)`, `NoDrones(carrierId)`, `Timeout(cap)`; ничья — список причин (сначала A, `wing4.py:383`); timeout по `cap or seriesTurnCap` (`wing4.py:364,368`); в бою игрока предела ходов нет (Combat.md:221); потери, статистика §7.1 и `roleChanges` — **модульными тестами на собранных вручную ситуациях** (сверка с полными боями — US-0004). Поэтапный внутренний API `Planning`/`Movement`/`CheckVictory`/`Firing` (`InternalsVisibleTo` Engine.Tests) повторяет `v4_reference.py:103-125`.

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Правила V4 — канон; расхождение — дефект порта.

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0003-TK-0001-battle-assembly](EP-0015-US-0003-TK-0001-battle-assembly/EP-0015-US-0003-TK-0001-battle-assembly.md) | engine | AC-01 |
| [EP-0015-US-0003-TK-0002-movement](EP-0015-US-0003-TK-0002-movement/EP-0015-US-0003-TK-0002-movement.md) | engine | AC-02 |
| [EP-0015-US-0003-TK-0003-salvo](EP-0015-US-0003-TK-0003-salvo/EP-0015-US-0003-TK-0003-salvo.md) | engine | AC-03 |
| [EP-0015-US-0003-TK-0004-turn-and-outcome](EP-0015-US-0003-TK-0004-turn-and-outcome/EP-0015-US-0003-TK-0004-turn-and-outcome.md) | engine | AC-04 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.

## Execution result

Four rules tickets complete. Targeted source slices and 6443 fresh regression cases PASS; detailed evidence is in ticket execution sections. AI/full-battle parity and real session/UI integration remain the dependent US-0004/0005 work; no playable-screen acceptance is claimed.

## Supersession — 2026-11-07

Инициативная последовательная стрельба EP-0017 заменяет прежний одновременный залп и допуск всего урона в конце анимации. Исходные критерии и результаты ниже сохранены как история. Текущий [контракт](../../../Documentation/04-Engineering/TacticalBattle.md) и [исполнение EP-0017](../../EP-0017-pilot-initiative/ExecutionStatus.md) описывают действующее поведение.
