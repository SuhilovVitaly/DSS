---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0005-session-contracts
title: "Контракты, сессия, транспорт боя"
stage: draft
dependencies: [EP-0015-US-0004-battle-ai]
---

# Контракты, сессия, транспорт боя

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как игрок, я управляю своей стороной через сессию игры: Client вызывает бой через соединение сессии, Engine ведёт бой и ИИ за обе стороны, мои приказы и цели только заменяют приказы моих дронов, а снимки боя неизменяемы и полны для карты и панелей.

## Зависимости

[EP-0015-US-0004-battle-ai](../EP-0015-US-0004-battle-ai/EP-0015-US-0004-battle-ai.md)

## Completion evidence

- **AC-01.** Contracts. **Start:** `scenarioId` (пусто — первый сценарий), `seed` (`long`, Engine берёт `& 0xFFFFFFFF`), `ownTactic`/`enemyTactic` (пусто — из сценария), `loadout[]` `{droneId, gunId, plateId, shieldId?, shieldCount}`. **Move:** частичный словарь `droneId → (v, dc)`. **Fire:** `droneId → targetId | null`. **Снимок:** фаза (move/fire/over), ход, сторона игрока, сид, id и имя сценария, id тактик сторон; юниты (id рамы, позиция, курс, скорость, корпус/броня с максимумом, состояние, id роли, `jammed`, множители двигателей, системы, лимиты, |dc| прошлого хода, сборка с `dmgMin`/`dmgMax`/`rangeBonus` и навыки); полные `suggest` своих дронов (без округления) — только в фазе move; `paths` движения; цели своих пушек (`list` с расстоянием, углом, кормой, `rearKill`, шансом; `default`) и точные выстрелы противника `incoming` — только в фазе fire; выстрелы последнего залпа в порядке бросков (шанс, урон, корма, kill, killed); `fireSkipped`; блок правил для прогноза (`hit.*`, `turnAtMax`, `rearArc`, `substeps`); потери; исход со списком типизированных причин. Журнал — типизированные события с ходом и фазой (включая фазу move для смен ролей и «целей нет»); ответ команды несёт **новые** события, запрос состояния — весь журнал. Options: сценарии (с целью, стороной игрока, дронами и слотами щитов), пушки, плиты, щиты, тактики (id), доступность боя и причина. Отказ — типизированный код + текущая фаза.

- **AC-02.** Engine: фазы как `play4.py` — ИИ планирует за обе стороны (броски тратятся и за игрока). **Дрон без приказа летит по предложению ИИ** (D-USER-10). К каждому приказу, включая предложение, применяется `clamp_speed`, затем `dc` ограничивается пределом поворота при этой скорости. Отказы, а не молчаливое игнорирование: NaN/±∞, id не своего или не участвующего дрона, неизвестные id пушки/плиты/щита, `shieldCount` вне `1…shieldSlots` при щите или ≠ 0 без щита, неверная фаза. На `fire` без цели дрон не стреляет; без целей у пушек игрока залп идёт сам, запись «целей нет» — до залпа. Предела ходов в бою игрока нет.

- **AC-03.** Пока бой активен, Engine отказывает в командах мира, сохранении, быстром сохранении и загрузке (тик-цикл LocalClient не трогается: мир стоит на Speed0 модальной паузы Client); бой никогда не выставляет поражение мира (`IsDefeated`, DefeatScreen). Флаг `IsTacticalBattleActive` — в снимке мира.

- **AC-04.** Транспорт: `IGameSessionConnection` получает `GetTacticalBattleOptionsAsync`, `StartTacticalBattleAsync`, `GetTacticalBattleStateAsync`, `MoveTacticalBattleAsync`, `FireTacticalBattleAsync`, `ExitTacticalBattleAsync` **default-методами интерфейса** (как `TravelStationAsync`, `IGameSessionConnection.cs:12-29`), чтобы тестовые и Performance-реализации не менялись; `LocalGameSessionConnection` выполняет их последовательно с тиком Engine; на UI-потоке ничего не бросается.

- **AC-05.** Client: `GameSessionHandle` даёт экранам методы боя и пишет каждую команду и каждый отказ в InterfaceLog (Engine и LocalClient InterfaceLog не видят, `LocalGameSessionConnection.cs:84-85`).

- **AC-06.** Через соединение на **всех 90 боях `battles.json`** в двух режимах — пустые приказы и цели `default`; явные приказы = `suggest` и цели = `default` — итог совпадает с эталоном.

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Песочница (D-USER-03): состояние боя не сохраняется. Победа/поражение — по `outcome == playerSide`.

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0005-TK-0001-contracts](EP-0015-US-0005-TK-0001-contracts/EP-0015-US-0005-TK-0001-contracts.md) | contracts | AC-01 |
| [EP-0015-US-0005-TK-0002-engine-session](EP-0015-US-0005-TK-0002-engine-session/EP-0015-US-0005-TK-0002-engine-session.md) | engine | AC-02 |
| [EP-0015-US-0005-TK-0003-world-guard](EP-0015-US-0005-TK-0003-world-guard/EP-0015-US-0005-TK-0003-world-guard.md) | engine | AC-03 |
| [EP-0015-US-0005-TK-0004-connection-api](EP-0015-US-0005-TK-0004-connection-api/EP-0015-US-0005-TK-0004-connection-api.md) | engine-local-client | AC-04 |
| [EP-0015-US-0005-TK-0005-client-session-handle](EP-0015-US-0005-TK-0005-client-session-handle/EP-0015-US-0005-TK-0005-client-session-handle.md) | client | AC-05 |
| [EP-0015-US-0005-TK-0006-session-tests](EP-0015-US-0005-TK-0006-session-tests/EP-0015-US-0005-TK-0006-session-tests.md) | engine-local-client | AC-03, AC-06 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.
