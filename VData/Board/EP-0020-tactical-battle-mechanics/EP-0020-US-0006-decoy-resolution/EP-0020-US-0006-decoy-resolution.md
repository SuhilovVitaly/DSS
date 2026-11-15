---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0006-decoy-resolution
title: Ловушки в движке
stage: done
dependencies: [EP-0020-US-0005-decoy-module]
created: 2026-11-08T11:46:28Z
source_request: "Добавить в фазу стрельбы пуск ловушки вместе с выстрелом или без него: ловушка «съедает» первый выстрел по дрону в очерёдности огня этого залпа, шанс промаха этого выстрела ×1,5; заряд −1; несовместимо с уклонением; ИИ пускает при угрозе ≥ 40 %."
current_review: epic
revision: 1
---

# Ловушки в движке

## Входное техническое задание

D-15–D-21. Шанс — `src/DeepSpaceSaga.Motion/TacticalBattle/TacticalHitChance.cs:17-39`; залп после M1 — `TacticalBattleSalvo.Fire` с `TacticalFirePlan` и множеством `Evading` (US-0002 TK-0003); сессия — `TacticalBattleSession.Fire/ResolveFire` (US-0002 TK-0004); контракты — `TacticalBattleRequests.cs:13-24`, `TacticalBattleSnapshot.cs:17-21,44-56,79-85`.

## User story

Как игрок, я хочу выпустить ловушку, когда по моему дрону целятся, чтобы первый выстрел по нему, скорее всего, ушёл в ловушку, а причина промаха была понятна.

## Dependencies

[EP-0020-US-0005-decoy-module](../EP-0020-US-0005-decoy-module/EP-0020-US-0005-decoy-module.md)

## Completion evidence

- Приказ огня допускает `Decoy`; сессия проверяет заряды и несовместимость с уклонением; ИИ пускает ловушки по правилу D-21.
- Первый выстрел по дрону с ловушкой получает шанс `max(5 %, 1 − 1,5 × (1 − шанс))`; заряд списывается при пуске.
- Ловушки противника видны только в результатах залпа.
- Отчёт о действии ловушек в боях записан в «Исполнение».

## Acceptance criteria

- **US6-AC1.** `TacticalFireTarget(DroneId, TargetId, bool Evade = false, bool Decoy = false)`. `Decoy` вместе с `Evade` → `InvalidRequest`; `Decoy` без модуля или при 0 зарядов → `DecoyUnavailable`; `Decoy` разрешён с пустой целью и при заклинившем оружии. Отказ атомарный, до мутаций.
- **US6-AC2.** `TacticalHitChance.WithDecoy(double chance, double missFactor, double minChance)` = `max(minChance, 1 − missFactor × (1 − chance))`; применяется к шансу после ограничения 5–95 %. Без ловушки все шансы побитно прежние (V4 trace seed 1 проходит).
- **US6-AC3.** При фиксации планов дрон с ловушкой теряет 1 заряд и попадает в множество `Lured`. Первый шаг залпа с броском по этому дрону получает шанс `WithDecoy` и снимает дрон из `Lured`; следующие выстрелы по нему обычные. Пропущенные шаги ловушку не тратят; неиспользованная ловушка пропадает в конце залпа. Пуск не тратит RNG.
- **US6-AC4.** ИИ: после фиксации целей и уклонений обеих сторон дрон под управлением ИИ (не уклоняется, есть заряд) пускает ловушку, если хотя бы один зафиксированный выстрел противника по нему имеет шанс без ловушки ≥ `decoy.aiThreatChance`. Правило одно для `Run/Step`, явного пути и стороны противника в сессии; дроны игрока сами ловушки не пускают.
- **US6-AC5.** Снимок: `TacticalIncomingShotSnapshot.ChanceIfDecoy` заполнен только у первого по очерёдности огня входящего выстрела по каждому дрону игрока; `TacticalShotSnapshot.Decoyed` и `ChanceWithoutDecoy`; `TacticalBattleSnapshot.DecoysLaunched` — id дронов, пустивших ловушку в последнем залпе; `TacticalForecastRules.Decoy`. В фазе планирования признаков будущих ловушек противника нет.
- **US6-AC6.** Регрессия: прямой и явный пути совпадают на 90 боях `battles.json` и сериях 1000×5; ожидания обновлены с записью причин. Отчёт на 1000 сидов × 5 тактик: пусков за бой, доля ловушек, поглотивших выстрел, средний шанс поглощённых выстрелов до и после, доля промахов среди них — в artifacts и «Исполнение».

## Non-goals

Интерфейс (US-0007), изменение чисел модуля по итогам отчёта, распознавание ловушек сенсорами.

## Invariants

Engine владеет RNG и исходом. Пуск ловушки не тратит RNG. Тактический бой не сохраняется. Порядок инициативы прежний.

## Решения и согласование

Ожидает утверждения эпика пользователем (см. [открытые вопросы](../Documentation.md#открытые-вопросы)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0020-US-0006-TK-0001-decoy-contracts](EP-0020-US-0006-TK-0001-decoy-contracts/EP-0020-US-0006-TK-0001-decoy-contracts.md) | Контракты ловушки | contracts | `EP-0020-US-0005-TK-0002-decoy-state`, `EP-0020-US-0002-TK-0001-evasion-contracts` | US6-AC1, US6-AC5 |
| [EP-0020-US-0006-TK-0002-decoy-hit-transform](EP-0020-US-0006-TK-0002-decoy-hit-transform/EP-0020-US-0006-TK-0002-decoy-hit-transform.md) | Преобразование шанса ловушкой | motion | `EP-0020-US-0006-TK-0001-decoy-contracts` | US6-AC2 |
| [EP-0020-US-0006-TK-0003-salvo-decoy](EP-0020-US-0006-TK-0003-salvo-decoy/EP-0020-US-0006-TK-0003-salvo-decoy.md) | Ловушки в разрешении залпа и правило ИИ | engine | `EP-0020-US-0006-TK-0002-decoy-hit-transform` | US6-AC2, US6-AC3, US6-AC4, US6-AC5 |
| [EP-0020-US-0006-TK-0004-session-decoy](EP-0020-US-0006-TK-0004-session-decoy/EP-0020-US-0006-TK-0004-session-decoy.md) | Сессия: приказ ловушки и скрытие до результатов | engine | `EP-0020-US-0006-TK-0003-salvo-decoy` | US6-AC1, US6-AC5 |
| [EP-0020-US-0006-TK-0005-decoy-regression](EP-0020-US-0006-TK-0005-decoy-regression/EP-0020-US-0006-TK-0005-decoy-regression.md) | Регрессия боя с ловушками и отчёт | engine | `EP-0020-US-0006-TK-0004-session-decoy` | US6-AC6 |

## Исполнение

Все5тикетов завершены. AC1 Session142/Contracts27PASS; AC2 Motion12PASS; AC3/4/5 focused55+Golden/state94PASS; AC6 Engine2850PASS,5000exact seriesPASS,5000 reportPASS. Измерение:64776пусков,62097эффектов; средний шанс59.03%→38.66% (−34.51% ожидаемой частоты попаданий), initial scenario. Самопроверка выполнена; тактическое состояние не сохраняется, world Save20 suitePASS. Native/human UI приёмка здесь не заявляется.
