---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0015-localization
title: "Язык тактического боя: покрытие"
stage: draft
dependencies: [EP-0015-US-0014-legacy-foundation-removal]
---

# Язык тактического боя: покрытие

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как игрок, я вижу все подписи подготовки, боя, результатов, журнала и прогноза на русском; ключи добавлялись вместе с каждым UI-тикетом, эта история проверяет покрытие.

## Зависимости

[EP-0015-US-0014-legacy-foundation-removal](../EP-0015-US-0014-legacy-foundation-removal/EP-0015-US-0014-legacy-foundation-removal.md)

## Completion evidence

- **AC-01.** Тест покрытия: каждая строка экранов боя берётся из локали (поиск литералов в `UI/Screens/TacticalBattle`); все ключи боя есть в `Russian.json` и (черновиком) в `English.json` — наборы ключей совпадают; ключей старого боя нет. Имена рам, модулей, пилотов, юнитов, тактик, ролей и сценариев показываются из данных V4 (по-русски) по id (D-USER-11).

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Вычитка английского, английские имена данных и нативная проверка на английском — вне эпика (D-USER-11).

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0015-TK-0001-locale-coverage](EP-0015-US-0015-TK-0001-locale-coverage/EP-0015-US-0015-TK-0001-locale-coverage.md) | client | AC-01 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.
