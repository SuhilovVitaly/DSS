---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0001-content-and-loader
ticket: EP-0015-US-0001-TK-0004-content-sync
title: content-sync
stage: draft
layer: tooling
depends_on: ["EP-0015-US-0001-TK-0003-content-loader"]
files_touched: 4
serves: [AC-04]
created: 2026-11-05
revision: 3
---

# content-sync

## Why
Не допустить расхождения игры и V4.0 после ребаланса.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `DB/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `DB/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tools/DeepSpaceSaga.CarrierWingSim/scripts/v4_update.py` | пересобирает V4.0 | Флаг `--export-game` |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ContentSyncTests.cs` | new | Семантическое равенство игрового контента и V4.0/Data, порядок ключей |
| `DB/Planning/V4.0/README.md` | раздел «Как менять» | Шаг экспорта в игру |
| `DB/Planning/V4.0/Spec/Generation.md` | §2 «любое число дронов» | Привести к проверке «≥ 1 дрона на сторону» |

## Public API after the change
CLI `--export-game`; тест ищет корень репозитория вверх от каталога тестов.

## Implementation steps
1. Экспорт копирует только JSON.
2. Тест сравнивает каждый файл по значениям и порядку ключей.
3. Описать порядок в V4.0/README; `v4_update.py --check` зелёный.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `DB/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Engine.Tests; Python `python -m unittest discover -s tests` в инструменте. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-04 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `DB/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
