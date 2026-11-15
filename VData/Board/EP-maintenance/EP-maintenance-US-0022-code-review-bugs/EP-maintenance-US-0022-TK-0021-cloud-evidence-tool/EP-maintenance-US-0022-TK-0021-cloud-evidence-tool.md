---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0021-cloud-evidence-tool
title: Замер облаков падает на бою без аномалий
stage: draft
implementation_status: not_started
severity: minor
layer: tooling
test_project: tools/DeepSpaceSaga.Performance
serves: [AC21]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Замер облаков падает на бою без аномалий

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Замер `DSS_TACTICAL_CLOUD_TIMING=1` на бою без сгустков записывает «skipped», при вырожденном охвате не даёт бесконечного масштаба, при отказе поверхности не падает.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0024-US-0004-TK-0002. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** `clumps.MaxBy(...)!` даёт null, `clumps.Min(...)` на пустой коллекции бросает InvalidOperationException. При `right == left` (один сгусток с σ = 0) `PixelsPerUnit = ∞`. `SKSurface.Create(context, …)` не проверяется.

**Сценарий.** Нативный прогон доказательств с включённым замером на бою без аномалий падает.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `tools/DeepSpaceSaga.Performance/TacticalBattleCloudEvidence.cs:10-21, 30` | Расчёт охвата и поверхность. |
| `tools/DeepSpaceSaga.Performance/TacticalBattleNativeEvidence.cs:208` | Вызов. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Пустые сгустки → «skipped»; охват ограничить снизу; проверять поверхность на null.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

Прогон инструмента на бою без аномалий и с одним сгустком завершается с отчётом.

## Definition of Done

- AC21 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
