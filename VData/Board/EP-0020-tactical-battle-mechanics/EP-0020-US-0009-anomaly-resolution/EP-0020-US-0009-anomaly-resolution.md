---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0009-anomaly-resolution
title: Аномалии в движке: поглощение, пыль, ИИ
stage: draft
dependencies: [EP-0020-US-0008-anomaly-field]
created: 2026-11-08T11:46:28Z
source_request: "Температурная аномалия поглощает выстрел с вероятностью, равной доле линии огня внутри неё (отдельный бросок до попадания); дрон в пыли не стреляет; ИИ не заканчивает ход в пыли, если оттуда были бы цели, и выбирает цель по итоговому шансу."
current_review: epic
revision: 1
---

# Аномалии в движке: поглощение, пыль, ИИ

## Входное техническое задание

D-26–D-33, A-26–A-29. Залп — `TacticalBattleSalvo.Candidates/DefaultTarget/Fire` (`src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:24-48,50-154`, после M1/M2); ИИ движения — `TacticalBattleAi.Plan` (`src/DeepSpaceSaga.Engine/TacticalBattle/Ai/TacticalCommander.cs:98-124`); сессия — `TacticalBattleSession.Fire/ResolveFire`.

## User story

Как игрок, я хочу, чтобы аномалии честно и предсказуемо меняли стрельбу: шанс поглощения виден заранее, дрон в пыли не стреляет, а ИИ играет по тем же правилам.

## Dependencies

[EP-0020-US-0008-anomaly-field](../EP-0020-US-0008-anomaly-field/EP-0020-US-0008-anomaly-field.md)

## Completion evidence

- Поглощение, пыль и правила ИИ действуют одинаково в `Run/Step`, явном пути и сессии.
- Снимок даёт итоговые шансы, долю поглощения и признак «в пыли»; результаты — поглощённые выстрелы.
- Ожидания исторических тестов обновлены с записанной причиной; отчёт о влиянии аномалий записан.

## Acceptance criteria

- **US9-AC1.** `TacticalCandidate.Absorption` = `TacticalAnomalyMath.Absorption(стрелок, цель)` для любых целей, включая носители. Итоговый шанс = `шанс × (1 − Absorption)`, где `шанс` — после ограничения 5–95 %, уклонения и ловушки. `DefaultTarget` выбирает по итоговому шансу.
- **US9-AC2.** Шаг с выстрелом при `Absorption > 0`: сначала один бросок `Random() < Absorption` → выстрел поглощён: без броска попадания, урона и броска кормы; иначе обычный бросок попадания. При `Absorption = 0` бросков столько же, сколько раньше. Поглощённый выстрел тратит ловушку цели (A-27) и считается выстрелом в статистике (`Shots`, `Absorbed`).
- **US9-AC3.** Дрон, центр которого на момент фиксации планов внутри пылевой зоны, получает шаг `SkipReason.InDust` (после `Evading`, до `WeaponJammed`) без бросков; его приказ с целью → `InDust` отказ; уклонение и ловушка ему доступны. По дрону в пыли стрелять можно.
- **US9-AC4.** ИИ движения: если конец пути предложенного приказа внутри пыли и из конечной позиции у дрона был бы хотя бы один кандидат без учёта пыли, приказ заменяется ближайшим по сетке `aiDustSearch` (поворот от предложенного шагом ¼ предела в обе стороны, затем скорости min/max) с концом вне пыли; если такого нет — приказ прежний. Без бросков RNG.
- **US9-AC5.** Снимок: `TacticalTargetSnapshot.Absorption`, `TacticalIncomingShotSnapshot.Absorption`, `TacticalUnitSnapshot.InDust` (на текущей позиции), `TacticalShotSnapshot.Absorption`, `Absorbed`; журнал `ShotAbsorbed`.
- **US9-AC6.** Регрессия: прямой и явный пути совпадают на 90 боях и сериях 1000×5; ожидания обновлены с причинами. Отчёт на 1000 сидов × 5 тактик: число аномалий, доля выстрелов через температурные зоны, средняя доля поглощения, доля поглощённых, доля ходов в пыли у дронов ИИ и игрока (по предложениям ИИ), средняя длина боя — в artifacts и «Исполнение».

## Non-goals

Интерфейс (US-0010), изменение чисел по итогам отчёта, движение аномалий, урон от аномалий.

## Invariants

Engine владеет RNG. Аномалии не влияют на движение (D-28). Тактический бой не сохраняется.

## Решения и согласование

Ожидает утверждения эпика пользователем (см. [открытые вопросы](../Documentation.md#открытые-вопросы)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0020-US-0009-TK-0001-anomaly-contracts](EP-0020-US-0009-TK-0001-anomaly-contracts/EP-0020-US-0009-TK-0001-anomaly-contracts.md) | Контракты поглощения и пыли | contracts | `EP-0020-US-0008-TK-0003-anomaly-generator` | US9-AC3, US9-AC5 |
| [EP-0020-US-0009-TK-0002-salvo-anomalies](EP-0020-US-0009-TK-0002-salvo-anomalies/EP-0020-US-0009-TK-0002-salvo-anomalies.md) | Поглощение и пыль в залпе | engine | `EP-0020-US-0009-TK-0001-anomaly-contracts` | US9-AC1, US9-AC2, US9-AC3 |
| [EP-0020-US-0009-TK-0003-ai-dust-avoidance](EP-0020-US-0009-TK-0003-ai-dust-avoidance/EP-0020-US-0009-TK-0003-ai-dust-avoidance.md) | ИИ: не заканчивать ход в пыли | engine | `EP-0020-US-0009-TK-0002-salvo-anomalies` | US9-AC4 |
| [EP-0020-US-0009-TK-0004-session-anomalies](EP-0020-US-0009-TK-0004-session-anomalies/EP-0020-US-0009-TK-0004-session-anomalies.md) | Сессия: отказ в пыли и журнал | engine | `EP-0020-US-0009-TK-0003-ai-dust-avoidance` | US9-AC3, US9-AC5 |
| [EP-0020-US-0009-TK-0005-anomaly-regression](EP-0020-US-0009-TK-0005-anomaly-regression/EP-0020-US-0009-TK-0005-anomaly-regression.md) | Регрессия боя с аномалиями и отчёт | engine | `EP-0020-US-0009-TK-0004-session-anomalies` | US9-AC6 |
