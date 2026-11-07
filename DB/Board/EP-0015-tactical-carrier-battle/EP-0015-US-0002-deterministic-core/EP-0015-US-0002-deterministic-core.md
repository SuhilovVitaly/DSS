---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0002-deterministic-core
title: "Детерминированное ядро и общая математика боя"
stage: done
dependencies: []
---

# Детерминированное ядро и общая математика боя

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как разработчик, я получаю в C# те же числа, что справочная реализация V4: Python-семантику вычислений, генератор mulberry32 (в Engine), геометрию, лимиты движения и шанс попадания (общими функциями Motion для Engine и прогноза Client) — бит в бит с эталонами, собранными на glibc.

## Зависимости

none

## Completion evidence

- **AC-01.** `PyMath` закрывает ловушки C# (R-01): Python-`clamp`/`min`/`max` (первый при равенстве, без исключения при lo > hi, поведение на ±0.0), `FloorNonNeg`, `Fmod`, первый минимум, упорядоченная уникализация (аналог `dict.fromkeys`); тест-поиск по исходникам `TacticalBattle/` запрещает `List.Sort`, `Array.Sort`, `Math.Clamp`, `Math.Min/Max`, `IEEERemainder`, `HashSet`/`Distinct` для порядка, `ToString("F0")`/`"N0"` для показа и деление int/int.

- **AC-02.** Эталоны V4.0 экспортированы в JSON **только на Linux/glibc** (WSL2 Ubuntu на VDI исполнения или Linux-хост, R-05): перед записью `v4_reference.py --check` подтверждает, что md-эталоны воспроизводятся этой средой; заголовок — версии Python (ровно `3.10.12` или записанная новая), glibc, ОС, дата, и тест C# сверяет версию CPython в заголовке с версией порта `vector_norm`. Double — 64-битный образ IEEE в hex (`struct.pack('<d')`), читается `BitConverter.Int64BitsToDouble`, парсер покрыт тестом. Состав: `battles.json` (все 90 боёв: исход, **структурная** причина, ходы, потери, корпус носителей, вся статистика §7.1 с пушками, `roleChanges`, броски после сборки и **число бросков после каждого этапа каждого хода**), `trace-seed-1.json` (каждый этап, все поля), `geometry.json` (крайние входы: `norm` около ±180 и «крайний случай не исправлять» Combat.md:12, оси `atan2`, `dist` = 0, большие углы; плюс **все фактические аргументы и результаты `atan2`/`sin`/`cos`/`hypot` из 90 эталонных боёв**), `series.json` (целые счётчики по тактикам: исходы A/B/draw/timeout, winA1, winA0, суммы ходов, потерь, гибелей носителей, выстрелов, попаданий, rearKills, systems). Нижняя часть `series.md` (`expected_hits`) — вне эпика.

- **AC-03.** `Mulberry32` (Engine — RNG принадлежит Engine) даёт контрольные значения Generation §1 и совпадает с эталоном в `uniform`, `randint`, `choice`, `weighted` (последняя строка по умолчанию); арифметика `unchecked` uint, умножение 64-битное с маской; счётчик вызовов `Calls` для сверки «42 + N».

- **AC-04.** `TacticalGeometry` (`norm`, `bearing`, `vec`, `dist`, `clamp`, путь и конец пути Combat §5) бит в бит совпадает с `geometry.json` по `BitConverter.DoubleToInt64Bits`. `dist` — **дословный порт CPython `vector_norm` для n = 2 той версии, что в заголовке эталона**, не `double.Hypot` и не `Math.Sqrt(dx*dx+dy*dy)`; `rad = a · 0.017453292519943295`, `deg = r · 57.29577951308232`.

- **AC-05.** `TacticalLimits` (масса → k → vmin, vmax, change, turn; `clamp_speed`; предел поворота; масса — сумма двигателей отдельно и прибавляется в конце, `wing4.py:250-251`) и `TacticalHitChance` (слагаемые Combat §6.2 в порядке суммирования, `rangeBonus`, без бросков) живут в Motion и совпадают со слагаемыми выстрелов `trace-seed-1.json`; их используют и Engine, и Client.

- **AC-06.** AC-04/05 проходят на Windows (VDI исполнения) и Linux. Если тригонометрия .NET на Windows расходится с glibc хотя бы на одном аргументе `geometry.json`: (1) сначала — порт корректно округлённых `Sin`/`Cos`/`Atan2` в `DeterministicMath` (Motion), сверка со всеми аргументами эталона; (2) если это не даёт совпадения — разрешена перегенерация эталонов (D-USER-13): тот же алгоритм в Python, `v4_update.py`, запись в `VData/Planning/V4.0-archive/Changes/` и снимки баланса до/после, новая цель в Documentation.md §1. Решение и замер записаны.

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

G-03 закрывается фактом замера TK-0006. Без него US-0004 не закрывается.

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0002-TK-0001-py-math](EP-0015-US-0002-TK-0001-py-math/EP-0015-US-0002-TK-0001-py-math.md) | motion | AC-01 |
| [EP-0015-US-0002-TK-0002-reference-fixtures](EP-0015-US-0002-TK-0002-reference-fixtures/EP-0015-US-0002-TK-0002-reference-fixtures.md) | tooling | AC-02 |
| [EP-0015-US-0002-TK-0003-mulberry32](EP-0015-US-0002-TK-0003-mulberry32/EP-0015-US-0002-TK-0003-mulberry32.md) | engine | AC-03 |
| [EP-0015-US-0002-TK-0004-geometry](EP-0015-US-0002-TK-0004-geometry/EP-0015-US-0002-TK-0004-geometry.md) | motion | AC-04 |
| [EP-0015-US-0002-TK-0005-limits-and-chance](EP-0015-US-0002-TK-0005-limits-and-chance/EP-0015-US-0002-TK-0005-limits-and-chance.md) | motion | AC-05 |
| [EP-0015-US-0002-TK-0006-platform-determinism](EP-0015-US-0002-TK-0006-platform-determinism/EP-0015-US-0002-TK-0006-platform-determinism.md) | motion | AC-06 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.

## Execution result

Six tickets complete under D-USER-14/15. Numerical platform evidence and remaining integration/coverage gates: [PlatformValidation.md](../PlatformValidation.md). This status does not claim playable tactical combat.
