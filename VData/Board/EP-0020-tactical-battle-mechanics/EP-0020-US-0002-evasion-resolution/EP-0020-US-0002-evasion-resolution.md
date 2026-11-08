---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0002-evasion-resolution
title: Уклонение в движке
stage: draft
dependencies: [EP-0020-US-0001-consciousness]
created: 2026-11-08T11:46:28Z
source_request: "Добавить в фазу стрельбы выбор «уклонение» вместо выстрела: −20 к шансу любого выстрела по уклоняющемуся дрону, −10 сознания, ИИ противника уклоняется при корпусе < 50 %, фаза стрельбы открывается при угрозе дронам игрока."
current_review: epic
revision: 1
---

# Уклонение в движке

## Входное техническое задание

D-01–D-10. Шанс — `TacticalHitChance.Terms` (`src/DeepSpaceSaga.Motion/TacticalBattle/TacticalHitChance.cs:17-39`); залп — `TacticalBattleSalvo.Fire` (`TacticalBattleSalvo.cs:50-154`); сессия — `TacticalBattleSession.Fire/ResolveFire/Move` (`:102-122`, `:124-176`, `:92-97`); контракт — `TacticalBattleRequests.cs:13-24`, `TacticalBattleSnapshot.cs:17-21,44-56`.

## User story

Как игрок, я хочу в фазе стрельбы увести ценного пилота или повреждённый дрон в уклонение вместо выстрела, чтобы его было труднее подбить.

## Dependencies

[EP-0020-US-0001-consciousness](../EP-0020-US-0001-consciousness/EP-0020-US-0001-consciousness.md)

## Completion evidence

- Приказ огня допускает `Evade`; сессия проверяет его и сознание; ИИ противника выбирает уклонение по правилу D-07.
- Каждый выстрел по уклоняющемуся дрону в этом залпе получает −20 до ограничения 5–95 %; уклоняющийся не стреляет и тратит 10 сознания.
- Фаза стрельбы открывается, если игроку есть в кого стрелять или противник может стрелять по его дронам.
- Уклонение противника видно только в результатах залпа.

## Acceptance criteria

- **US2-AC1.** `TacticalFireTarget(DroneId, TargetId, bool Evade = false)`. `Evade` с непустой `TargetId` → `InvalidRequest`; `Evade` при сознании < `minConsciousness` → `EvasionUnavailable`; `Evade` разрешён при пустом списке кандидатов и у заклинившего оружия. Отказ атомарный, до мутаций.
- **US2-AC2.** Слагаемое `Evasion = −hitPenalty`, если цель уклоняется, иначе 0; входит в сумму до ограничения; действует против дронов и носителей, включая выстрелы в корму. При `Evasion = 0` все шансы побитно равны прежним (V4 trace seed 1 проходит).
- **US2-AC3.** Решения об уклонении фиксируются вместе с планами до первого шага залпа; сознание списывается в этот момент (`−cost`). Уклоняющийся шаг получает `SkipReason.Evading` без бросков RNG. Статус уклонения действует весь залп.
- **US2-AC4.** ИИ уклоняется, если `Hull < aiHullFraction × MaxHull` и `Consciousness ≥ minConsciousness`; правило одно для `Run/Step`, явного пути и стороны противника в сессии; дроны игрока сами не уклоняются.
- **US2-AC5.** `Move` пропускает фазу стрельбы только если у игрока нет кандидатов **и** ни у одного активного дрона противника нет кандидата-дрона игрока.
- **US2-AC6.** Снимок: `TacticalIncomingShotSnapshot.ChanceIfEvading` (шанс по этой цели, если она уклонится), `TacticalShotSnapshot.TargetEvading`, `TacticalForecastRules.Evasion` (числа правил). В фазе планирования нет признаков будущего уклонения противника.
- **US2-AC7.** Регрессия: прямой и явный пути совпадают на 90 боях `battles.json` и сериях 1000×5; ручные случаи уклонения проверены; ожидания `SessionTests`/`GoldenBattleTests` обновлены с записью причин.

## Non-goals

Интерфейс (US-0004), калибровка числа (US-0003), влияние сознания на другие действия.

## Invariants

Engine владеет RNG и исходом. Выбор уклонения не тратит RNG. Тактический бой не сохраняется. Порядок инициативы прежний.

## Решения и согласование

Ожидает утверждения эпика пользователем (см. [открытые вопросы](../Documentation.md#открытые-вопросы)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0020-US-0002-TK-0001-evasion-contracts](EP-0020-US-0002-TK-0001-evasion-contracts/EP-0020-US-0002-TK-0001-evasion-contracts.md) | Контракты уклонения | contracts | `EP-0020-US-0001-TK-0002-consciousness-state` | US2-AC1, US2-AC6 |
| [EP-0020-US-0002-TK-0002-evasion-hit-term](EP-0020-US-0002-TK-0002-evasion-hit-term/EP-0020-US-0002-TK-0002-evasion-hit-term.md) | Слагаемое уклонения в шансе попадания | motion | `EP-0020-US-0002-TK-0001-evasion-contracts` | US2-AC2 |
| [EP-0020-US-0002-TK-0003-salvo-evasion](EP-0020-US-0002-TK-0003-salvo-evasion/EP-0020-US-0002-TK-0003-salvo-evasion.md) | Уклонение в разрешении залпа и правило ИИ | engine | `EP-0020-US-0002-TK-0002-evasion-hit-term` | US2-AC2, US2-AC3, US2-AC4 |
| [EP-0020-US-0002-TK-0004-session-evasion](EP-0020-US-0002-TK-0004-session-evasion/EP-0020-US-0002-TK-0004-session-evasion.md) | Сессия: приказ уклонения и открытие фазы стрельбы | engine | `EP-0020-US-0002-TK-0003-salvo-evasion` | US2-AC1, US2-AC5, US2-AC6 |
| [EP-0020-US-0002-TK-0005-evasion-regression](EP-0020-US-0002-TK-0005-evasion-regression/EP-0020-US-0002-TK-0005-evasion-regression.md) | Регрессия боя с уклонением | engine | `EP-0020-US-0002-TK-0004-session-evasion` | US2-AC7 |
