---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0001-content-and-loader
title: "Данные тактического боя в контенте игры"
stage: in-progress
dependencies: []
---

# Данные тактического боя в контенте игры

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как разработчик, я держу все числа тактического боя — рамы, модули (оружие, двигатели, щиты, броня), пилотов, компоновки, ИИ, правила и сценарии — в JSON контента игры с тем же устройством, что `VData/Planning/V4.0/Data`. Engine загружает и проверяет их вместе с остальным контентом; ошибка в данных боя не ломает остальную игру, а делает бой недоступным с понятной причиной.

## Зависимости

none

## Completion evidence

- **AC-01.** В `src/DeepSpaceSaga.Client/Data/TacticalBattle/` лежат `rules.json`, `ai.json`, `pilots.json`, `layouts.json`, `scenarios.json`, `Frames/<id>.json` (4), `Modules/Weapons|Engines|Shields|Armor/<id>.json` (7 + 6 + 1 + 6). Значения совпадают с `VData/Planning/V4.0/Data`. В коде боя нет чисел, которых нет в этих файлах, кроме закрытого списка определений Spec (записан в `TacticalBattleConstants` с комментарием-ссылкой): 180° кормы и хвоста, 90° заслона, 2 броска погони, 2 и 100 в ошибке прицела и /100 шанса, порядок слотов ролей striker → interceptor → fighter, стороны A/B, умолчания `shieldSlots` = 1 и `shieldCount` = 1, множитель целого двигателя 1.0, начальная роль «истребитель» и режим ударника `in`, константы mulberry32.

- **AC-02.** `Settings.json` → `typeData.tacticalBattle: "Data/TacticalBattle"`; файлы копируются в output; путь разрешается относительно `AppContext.BaseDirectory`. Данные читаются `EngineContentLoader` при каждом создании Engine (New Game, Load, Scenario — как весь контент), а не на каждый бой; доступность боя — свойство сессии Engine.

- **AC-03.** Загрузчик строит неизменяемые определения: каталоги из папок в порядке имён файлов (`StringComparer.Ordinal`), `id` = имя файла; **порядок ключей JSON-объектов сохраняется** (сценарий по умолчанию — первый ключ `scenarios.json`, порядок тактик — порядок `ai.json`; Combat §1.1). Проверяется всё, что проверяет `wing4.validate` (`wing4.py:91-158`), а не только Generation §2: корпус > 0; `damageMin`, `piercing`, броня, масса щита ≥ 0; рама `carrier` и целое `carrier.maneuver`; пары навыков min ≤ max; компоновка носителя — класса `carrier`, дрона — нет; хотя бы один дрон на сторону; `goal.seeds` ≥ 1; шаги ИИ — целые > 0; `startTactic` не adaptive; отсутствующее поле — ошибка; `true`/`false` в числовом поле — ошибка; `rules.version == "4.0"`. Ошибка — `ContentException` с файлом и полем; мир игры загружается; Engine сообщает `TacticalBattleAvailability.Unavailable(reason)`, Client пишет причину в InterfaceLog (R-08), F8 открывает подготовку с причиной и без «Начать бой».

- **AC-04.** `tools/DeepSpaceSaga.CarrierWingSim/scripts/v4_update.py --export-game` копирует `V4.0/Data` (только JSON) в игру; тест `ContentSyncTests` сверяет игровой контент с `VData/Planning/V4.0/Data` по значениям и порядку ключей. Противоречие «любое число дронов» (Generation.md §2) с проверкой «≥ 1 дрона» исправлено в тексте Generation.md.

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

D-USER-07: `VData/Planning/V4.0` должен быть в checkout. Схема V4.0 — каноническая: менять её только через V4.0 и экспорт.

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0001-TK-0001-content-data](EP-0015-US-0001-TK-0001-content-data/EP-0015-US-0001-TK-0001-content-data.md) | content-data | AC-01, AC-02 |
| [EP-0015-US-0001-TK-0002-content-definitions](EP-0015-US-0001-TK-0002-content-definitions/EP-0015-US-0001-TK-0002-content-definitions.md) | engine | AC-01, AC-03 |
| [EP-0015-US-0001-TK-0003-content-loader](EP-0015-US-0001-TK-0003-content-loader/EP-0015-US-0001-TK-0003-content-loader.md) | engine | AC-03 |
| [EP-0015-US-0001-TK-0004-content-sync](EP-0015-US-0001-TK-0004-content-sync/EP-0015-US-0001-TK-0004-content-sync.md) | tooling | AC-04 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.
