---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0010-battle-end
title: "Конец боя, новый бой и возврат на карту"
stage: done
dependencies: [EP-0015-US-0009-fire-and-results]
---

# Конец боя, новый бой и возврат на карту

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как игрок, по окончании боя я вижу итог и могу начать новый бой или вернуться на карту; новый бой можно начать и посреди боя; мир остаётся таким же, как до F8.

## Зависимости

[EP-0015-US-0009-fire-and-results](../EP-0015-US-0009-fire-and-results/EP-0015-US-0009-fire-and-results.md)

## Completion evidence

- **AC-01.** Итог: «Победа»/«Поражение»/«Ничья» по `outcome == playerSide`, причины (типизированные, локализованные; у ничьей — обе); «Новый бой» в верхней части панели доступен и посреди боя (`ScreenEvent.NewTacticalBattle`, возврат к подготовке с прежними настройками); «На карту»/Escape (с подтверждением посреди боя) вызывает `ExitTacticalBattleAsync` и `CloseTacticalBattle`.

- **AC-02.** Путь игрока F8 → бой → «На карту» в Client: мир после выхода идентичен миру до F8 (сериализованное сравнение через соединение), скорость и камера карты восстановлены, сохранение и меню снова доступны. Engine-уровень песочницы — US-0005 AC-03; здесь — сквозной путь через экраны.

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Язык — русский (D-USER-11); смена языка по-прежнему применяется после перезапуска (`Localization.cs:5-13`).

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0010-TK-0001-battle-end-ui](EP-0015-US-0010-TK-0001-battle-end-ui/EP-0015-US-0010-TK-0001-battle-end-ui.md) | client | AC-01 |
| [EP-0015-US-0010-TK-0002-sandbox-flow](EP-0015-US-0010-TK-0002-sandbox-flow/EP-0015-US-0010-TK-0002-sandbox-flow.md) | client | AC-02 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.

## Execution review — 2026-11-06

Both tickets technically complete. Actual local window and Engine verify active restart/exit, typed terminal presentation, exact world save identity and camera/speed restoration. Full Client2479 PASS. Native/physical and human acceptance remain open.
