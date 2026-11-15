---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0008-anomaly-field
title: Аномалии: правила, математика, генерация
stage: done
dependencies: [EP-0020-US-0006-decoy-resolution]
created: 2026-11-08T11:46:28Z
source_request: "Ввести две неподвижные аномалии тактического боя — температурную и пылевую — как кластеры эллиптических гауссиан, сгенерированные из seed боя отдельным потоком RNG; общая математика поля, порога и доли линии огня в `DeepSpaceSaga.Motion`; аномалии в снимке."
current_review: epic
revision: 1
---

# Аномалии: правила, математика, генерация

## Входное техническое задание

D-24, D-25, D-29–D-31, A-23–A-25. Сборка боя — `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs:19,61-68` (основной поток `Mulberry32(seed)`, разброс стартов); производный сид — `src/DeepSpaceSaga.Engine/Rng/RngStreamSeedDerivation.cs:27` (`DeriveStreamSeed`); правила — `rules.json` и таблица `VData/Planning/V4.0/Data/README.md`; геометрия — `src/DeepSpaceSaga.Motion/TacticalBattle/TacticalGeometry.cs:88` (`Path`); снимок — `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs:79-85`.

## User story

Как игрок, я хочу, чтобы на поле боя были аномалии, расположение которых видно с начала боя и не меняется, чтобы планировать манёвр и огонь с их учётом.

## Dependencies

[EP-0020-US-0006-decoy-resolution](../EP-0020-US-0006-decoy-resolution/EP-0020-US-0006-decoy-resolution.md)

## Completion evidence

- Блок `anomalies` в `rules.json` и таблице V4 Data; загрузчик проверяет диапазоны.
- `TacticalAnomalyMath` (Motion) считает поле, принадлежность точки и долю отрезка внутри зон; генератор создаёт аномалии каждого боя без изменения основного потока RNG.
- Снимок передаёт материализованные аномалии; старые нагрузки читаются.

## Acceptance criteria

- **US8-AC1.** `rules.json` получает блок `anomalies`: `zoneEdge 170`, `background 100`, `absorbSamples 200`, `heatCount [1,3]`, `dustCount [1,2]`, `clumps [6,10]`, `spread [40,55]`, `clumpAmp [70,160]`, `clumpSigmaX [0.18,0.34]`, `clumpSigmaY [0.14,0.26]` (доли размаха), `centerSpread [0.45,0.35]`, `clearance 60`, `bandFraction 0.6`, `maxAttempts 64`, `aiDustSearch` (сетка поиска ИИ, US-0009). Загрузчик отвергает пустые или перевёрнутые диапазоны, отрицательные значения, `zoneEdge ≤ background`, `absorbSamples < 1`. `ContentSyncTests` и `Export-Content.ps1 -Check` проходят.
- **US8-AC2.** `TacticalAnomalyMath.Field(kind, x, y)` = `background + Σ A·exp(−(u²/2σx² + v²/2σy²))` по пятнам этого вида (`u, v` — поворот на `rot`); `Inside(kind, x, y)` = `Field ≥ zoneEdge`; `Absorption(a, b)` = доля из `absorbSamples` равных подотрезков отрезка `a→b`, чья середина внутри температурной зоны; `Segments(a, b)` — те же подотрезки, слитые в интервалы `[t0, t1]`. Одна реализация для движка и клиента.
- **US8-AC3.** Генератор после разброса стартов берёт поток `Mulberry32(DeriveStreamSeed(seed, "tactical-anomalies"))`, выбирает число аномалий, центры в средней полосе между флотами (A-25) с зазором `clearance` до носителей и стартов дронов, пятна по распределениям US8-AC1; при неудаче за `maxAttempts` аномалия пропускается. Основной поток боя, `RandomCalls` до первого броска поглощения и все прежние исходы при пустом списке аномалий не меняются.
- **US8-AC4.** `TacticalBattleSnapshot.Anomalies` (`ImmutableArray<TacticalAnomalySnapshot>`, init, по умолчанию пусто): `Id`, `Kind` (`Heat`/`Dust`), `Clumps[] (X, Y, Amplitude, SigmaX, SigmaY, Rotation)`; `TacticalForecastRules.Anomalies` (`ZoneEdge`, `Background`, `AbsorbSamples`). Старые нагрузки читаются.

## Non-goals

Эффекты на стрельбу (US-0009), отрисовка (US-0010), движение аномалий, влияние на скорость, другие виды аномалий.

## Invariants

Тактический бой не сохраняется. Аномалии неподвижны весь бой (D-30). Состав аномалий определяется только seed боя и данными.

## Решения и согласование

Ожидает утверждения эпика пользователем (см. [открытые вопросы](../Documentation.md#открытые-вопросы)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0020-US-0008-TK-0001-anomaly-rules-content](EP-0020-US-0008-TK-0001-anomaly-rules-content/EP-0020-US-0008-TK-0001-anomaly-rules-content.md) | Правила аномалий в данных | content-data | `EP-0020-US-0005-TK-0001-decoy-content` | US8-AC1 |
| [EP-0020-US-0008-TK-0002-anomaly-math](EP-0020-US-0008-TK-0002-anomaly-math/EP-0020-US-0008-TK-0002-anomaly-math.md) | Математика поля аномалий | motion | `EP-0020-US-0008-TK-0001-anomaly-rules-content` | US8-AC2 |
| [EP-0020-US-0008-TK-0003-anomaly-generator](EP-0020-US-0008-TK-0003-anomaly-generator/EP-0020-US-0008-TK-0003-anomaly-generator.md) | Генерация аномалий из seed боя | engine | `EP-0020-US-0008-TK-0002-anomaly-math`, `EP-0020-US-0006-TK-0005-decoy-regression` | US8-AC3, US8-AC4 |

## Исполнение

US8 завершена. AC1 Content174PASS/export30PASS (TK1); AC2 Motion20PASS (TK2); AC3 генерация1000сидов+5000before/afterexactPASS (TK3); AC4 Contracts28PASS и реальная Session.Capture. Evidence в карточках. Самопроверка ownership/RNG/immutable snapshot пройдена; независимая и человеческая приёмка этим не заменяются.
