---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0018-review-corrections
title: "Исправления по код-ревью эпика"
stage: done
dependencies: [EP-0015-US-0017-project-documentation]
---

# Исправления по код-ревью эпика

## Связь с техническим заданием

Источник — код-ревью ветки `codex/ep-0015-tactical-carrier-battle` (диапазон `a9404103..b3303748`, 2026-11-06). Правила боя `VData/Planning/V4.0` не меняются. Решение пользователя 2026-11-06 (D-USER-REVIEW-01): при гибели корабля игрока от службы безопасности станции показывается простой терминальный экран «Корабль уничтожен» с кнопками «Загрузить» и «Главное меню», без старой боевой статистики; механика безопасности станции не меняется.

## User story

Как игрок, я не могу застрять в игре: гибель корабля ведёт на экран гибели, сохранение никогда не записывает погибший корабль, а окно тактического боя всегда закрывается и не блокирует мир.

## Зависимости

[EP-0015-US-0017-project-documentation](../EP-0015-US-0017-project-documentation/EP-0015-US-0017-project-documentation.md)

## Исправляемые места

Номера соответствуют находкам ревью. Строки — по `b3303748`.

| № | Место | Дефект | Исправление | Тикет |
|---|---|---|---|---|
| 1 | `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:430` (`PollGameSessionAutoTransition`), `src/DeepSpaceSaga.Client/GameSessionHandle.cs` | Удалены `DefeatScreen`/`IsDefeated`, а `StationSecuritySystem` по-прежнему уничтожает корабль; клиент без терминального пути, F5 бросает `player_destroyed` | Новый `ShipDestroyedScreen`; признак гибели — `IsDestroyed` строки корабля игрока в snapshot; F5/скорость/сохранение блокируются | TK-0002 |
| 2 | `src/DeepSpaceSaga.Engine/SimulationEngine.cs:1082` (`CaptureSaveStateCore`) | `UpdateStationSecurity` после последней проверки остановки — Save20 записывает погибший корабль | Проверка остановки после `UpdateStationSecurity` | TK-0001 |
| 3 | `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs:58` (`ExitAsync`), `SkiaWindow.cs:819` | При `NoBattle`/`WrongBattle`/`Unavailable` модальное окно нельзя закрыть | `ExitAsync` сверяется с живым боем движка; без боя — очищает состояние; `WrongBattle` — принимает живой бой и повторяет выход | TK-0003 |
| 4 | `TacticalBattleFlow.cs:72` (`Submit`) | Отказ `AlreadyActive` с живым снимком отбрасывается — осиротевший бой блокирует мир | Принимать живое состояние из отказов `AlreadyActive`/`WrongBattle` | TK-0003 |
| 5 | `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs:15` (`ReceiveDialogueCommand`), `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs:160` | Во время боя бросается `InvalidOperationException` | Команда помечается обработанной и получает диалоговое событие-отказ `tactical_battle_active` | TK-0001 |
| 6 | `SimulationEngine.cs:663` (`BuildSnapshot`) | Остановка внутри `UpdateStationSecurity` публикует время позже гибели, следующий snapshot прыгает назад | После `UpdateStationSecurity` снова применить часы остановки | TK-0001 |
| 7 | `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs:33` (`ProjectWorld`) | Возможные висячие ссылки на удалённые при миграции объекты | Регрессионный тест миграции Save19 с активным диалогом, инцидентом и удаляемым врагом; правка кода только если тест найдёт висячую ссылку | TK-0001 |
| 8 | `SimulationEngine.cs:152` (`ReceiveCommand`) | `_worldStateLock` перед `_commandGate` — отправка команды ждёт долгих операций мира | Флаг активного боя пишется под `_commandGate` при старте/выходе; `ReceiveCommand` берёт только `_commandGate` | TK-0001 |

## Completion evidence

- **AC-01.** Engine: сохранение после гибели бросает `player_destroyed` и не пишет файл; snapshot, в котором произошла остановка, несёт часы остановки; диалоговая команда во время боя не бросает и получает отказ; `ReceiveCommand` не берёт `_worldStateLock` и всё так же отклоняет команды мира во время боя; миграция Save19 с удаляемым врагом даёт загружаемый мир без висячих ссылок.
- **AC-02.** Client: гибель корабля игрока переводит окно на «Корабль уничтожен»; «Загрузить» и «Главное меню» работают; F5, смена скорости и сохранение заблокированы; ключи RU/EN.
- **AC-03.** Client: выход из боя завершается при `NoBattle`, при `WrongBattle` (живой бой выходит) и при закрытии подготовки с осиротевшим боем движка; отказ `AlreadyActive` принимает живой бой.

Automated, native/input и product/human acceptance фиксируются раздельно.

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0018-TK-0001-engine-world-integrity](EP-0015-US-0018-TK-0001-engine-world-integrity/EP-0015-US-0018-TK-0001-engine-world-integrity.md) | engine | AC-01 |
| [EP-0015-US-0018-TK-0002-ship-destroyed-screen](EP-0015-US-0018-TK-0002-ship-destroyed-screen/EP-0015-US-0018-TK-0002-ship-destroyed-screen.md) | client | AC-02 |
| [EP-0015-US-0018-TK-0003-battle-exit-recovery](EP-0015-US-0018-TK-0003-battle-exit-recovery/EP-0015-US-0018-TK-0003-battle-exit-recovery.md) | client | AC-03 |

## Не исправляется в этой истории

Находки ревью №9 (копия журнала в каждом ответе, неочищаемые `_tacticalIdentities`) и №10 (пересчёт `TacticalDrone.Limits`) — производительность, низкий приоритет, вне объёма по решению пользователя.

## Execution review — 2026-11-06

Все три тикета выполнены. №1–№6 и №8 исправлены, №7 подтверждён регрессионным тестом без правки кода. Не прогнаны 111 тестов тяжёлых корпусов Engine (превышают cap 300000 ms на класс). Известные посторонние падения: `ContentSyncTests` (нет `pwsh`), `SeriesParity.Raid` (US-0016 TK-0001), `ResultsPhaseTests.ActualSalvoWaits…` (падает и на baseline). Native/physical и human acceptance не выполнялись.
