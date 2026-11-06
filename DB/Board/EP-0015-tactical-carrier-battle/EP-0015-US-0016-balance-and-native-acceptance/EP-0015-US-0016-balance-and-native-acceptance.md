---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0016-balance-and-native-acceptance
title: "Приёмка баланса, производительности и нативная проверка"
stage: in-progress
dependencies: [EP-0015-US-0015-localization]
---

# Приёмка баланса, производительности и нативная проверка

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как автор, я убеждаюсь, что игра даёт тот же баланс, что V4, бой работает быстро, а реальным окном с клавиатурой и мышью можно провести бой от F8 до возврата на карту.

## Зависимости

[EP-0015-US-0015-localization](../EP-0015-US-0015-localization/EP-0015-US-0015-localization.md)

## Completion evidence

- **AC-01.** Серия цели в C# (goal.seeds, все тактики стороны игрока) совпадает с `VData/Planning/V4.0/Reference/series.json` по целым счётчикам точно. Тест длинный: категория `LongRunning` (вводится этим тикетом — `[Trait("Category", "LongRunning")]`, фильтр запуска записан), по тактике на запуск.

- **AC-02.** Производительность на VDI исполнения (Release): шаг хода (планирование + движение + залп) p95 ≤ 2 мс; `aimAt` на движении мыши ≤ 1 мс; кадр экрана боя ≤ 16 мс при 1920×1080 (замер по образцу `TacticalMapFrameProfile`/`GameSessionScreen.Profiling`); серия 1000 боёв ≤ 60 с. Фактические значения записаны.

- **AC-03.** Нативная проверка (после всех UI-историй и удаления старого боя): реальное окно 1280×720 и 1920×1080, uiScale 1 и 1.5, русский язык; бой «Начальный бой» от F8 до возврата; все клавиши и мышь AC US-0006…US-0010; клавиши игры во время боя; F5/F9 после выхода. Протокол — `FinalValidation.md`; по возможности — native evidence по образцу `tools/DeepSpaceSaga.Performance/*NativeEvidence.cs`; headless/raster не выдаются за нативную проверку; human/product gates отдельно.

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Баланс в эпике не меняется: правки чисел — только в V4.0 и экспорт (US-0001).

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0016-TK-0001-series-parity](EP-0015-US-0016-TK-0001-series-parity/EP-0015-US-0016-TK-0001-series-parity.md) | engine | AC-01 |
| [EP-0015-US-0016-TK-0002-performance](EP-0015-US-0016-TK-0002-performance/EP-0015-US-0016-TK-0002-performance.md) | engine | AC-02 |
| [EP-0015-US-0016-TK-0003-native-acceptance](EP-0015-US-0016-TK-0003-native-acceptance/EP-0015-US-0016-TK-0003-native-acceptance.md) | client-tools | AC-03 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.
## Итог выполнения

AC-02 Release performance и AC-03 native managed-window пройдены, протокол в [FinalValidation](../FinalValidation.md). AC-01 остаётся открытым: четыре серии точно совпали; «Налёт» — 856/140 против 857/139, одинаковые результаты по всем сидам на Windows и Ubuntu. Полная продуктовая/физическая приёмка остаётся отдельной.
