---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0001-consciousness
title: Сознание пилота
stage: done
dependencies: [EP-0019-tactical-battle-ui-redesign]
created: 2026-11-08T11:46:28Z
source_request: "Ввести у каждого пилота дрона ресурс «сознание» с правилами из данных: старт 100, максимум 100, +5 в начале каждого хода; значения видны клиенту в снимках."
current_review: epic
revision: 1
---

# Сознание пилота

## Входное техническое задание

D-05, D-11. Пилоты и сборка — `TacticalBattleAssembly.cs:55-58,83-89`; ход начинается в `TacticalBattle.Planning` (`TacticalBattle.cs:14-23`). Правила — `rules.json`, обязательно синхронны с таблицей `VData/Planning/V4.0/Data/README.md` (`ContentSyncTests.cs:22-48`).

## User story

Как игрок, я хочу видеть у каждого своего пилота настоящий уровень сознания, чтобы понимать, сколько раз он ещё может уклониться.

## Dependencies

[EP-0019-tactical-battle-ui-redesign](../../EP-0019-tactical-battle-ui-redesign/Documentation.md) (весь эпик)

## Completion evidence

- `rules.json` содержит блоки `consciousness` и `evasion`; таблица V4 Data и экспортёр синхронны; загрузчик проверяет диапазоны.
- У каждого дрона обеих сторон в начале боя сознание 100; в начале каждого следующего хода +5, не выше 100.
- Снимок передаёт сознание каждого дрона; старые полезные нагрузки без поля читаются.

## Acceptance criteria

- **US1-AC1.** `rules.json` получает `consciousness {start 100, max 100, recoveryPerTurn 5}` и `evasion {hitPenalty 20, cost 10, minConsciousness 10, aiHullFraction 0.5}`; `TacticalRules` их содержит; загрузчик отвергает отрицательные значения, `start > max`, `minConsciousness > max`, `aiHullFraction` вне [0,1]; `ContentSyncTests` и `Export-Content.ps1 -Check` проходят.
- **US1-AC2.** `TacticalDrone.Consciousness` = `start` после сборки боя; `Planning()` при `Turn > 1` добавляет `recoveryPerTurn` с ограничением `max`; путь сессии и пакетный `Run/Step` дают одинаковые значения; RNG не расходуется.
- **US1-AC3.** `TacticalUnitSnapshot.Consciousness` (`int?`, init) заполнен для дронов и `null` для носителей; JSON без поля читается как `null`.

## Non-goals

Расход сознания (US-0002), влияние сознания на точность, манёвр и инициативу (будущая механика), перенос сознания между боями.

## Invariants

Тактический бой не сохраняется (Save20 не меняется). Геометрия, порядок стрельбы и поток RNG прежние.

## Решения и согласование

Ожидает утверждения эпика пользователем (см. [открытые вопросы](../Documentation.md#открытые-вопросы)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0020-US-0001-TK-0001-rules-content](EP-0020-US-0001-TK-0001-rules-content/EP-0020-US-0001-TK-0001-rules-content.md) | Правила сознания и уклонения в данных | content-data | — | US1-AC1 |
| [EP-0020-US-0001-TK-0002-consciousness-state](EP-0020-US-0001-TK-0002-consciousness-state/EP-0020-US-0001-TK-0002-consciousness-state.md) | Сознание в состоянии боя и снимках | engine | `EP-0020-US-0001-TK-0001-rules-content` | US1-AC2, US1-AC3 |

## Итоговая проверка

US1-AC1: rules/content loader и ContentSync 118/118 PASS. US1-AC2: реальное восстановление, сессия/direct и RNG control; US1-AC3: снимки и legacy JSON, 23/23 Contracts PASS. 1000×5 series parity PASS, результаты в карточках. Save20 не меняется. Self-review истории выполнен, независимое review не заявляется.
