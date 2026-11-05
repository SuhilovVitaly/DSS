---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0001-content-and-loader
ticket: EP-0015-US-0001-TK-0003-content-loader
title: content-loader
stage: draft
layer: engine
depends_on: ["EP-0015-US-0001-TK-0002-content-definitions"]
files_touched: 3
serves: [AC-03]
created: 2026-11-05
revision: 3
---

# content-loader

## Why
Загрузить и проверить данные; ошибка боя не ломает мир.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleContentLoader.cs` | new | Дерево, id = имя файла, сортировка Ordinal, порядок ключей через `JsonDocument`, все проверки `wing4.validate`, `rules.version` |
| `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs` | загрузка typeData при создании Engine | Подключить `tacticalBattle`; ошибку не пробрасывать в загрузку мира, а сохранить как `TacticalBattleAvailability` |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ContentLoaderTests.cs` | new | Тест на каждую проверку `wing4.validate` + production-контент + мир грузится при ошибке боя |

## Public API after the change
`TacticalBattleContentLoader.Load(folder) → TacticalBattleCatalog`; `TacticalBattleAvailability { Available | Unavailable(reason: file, field, message) }`.

## Implementation steps
1. Перечень проверок AC-03 — по `wing4.py:91-158`; каждая — отдельный тест-кейс.
2. Причина — структурой (файл, поле, сообщение); Engine не пишет в InterfaceLog (R-08).
3. Остальной контент грузится как раньше.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Engine.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-03 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
