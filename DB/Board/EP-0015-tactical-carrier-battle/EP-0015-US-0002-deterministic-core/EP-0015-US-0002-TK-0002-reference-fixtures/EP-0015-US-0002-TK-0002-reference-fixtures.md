---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0002-deterministic-core
ticket: EP-0015-US-0002-TK-0002-reference-fixtures
title: reference-fixtures
stage: draft
layer: tooling
depends_on: ["EP-0015-US-0002-TK-0001-py-math"]
files_touched: 7
serves: [AC-02]
created: 2026-11-05
revision: 3
---

# reference-fixtures

## Why
Тестам C# нужны полные машиночитаемые эталоны, собранные той же математикой, что md.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `DB/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `DB/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tools/DeepSpaceSaga.CarrierWingSim/scripts/v4_reference.py` | пишет md | Ещё JSON эталонов AC-02, заголовок версий, IEEE-hex, структурная причина, броски по этапам |
| `tools/DeepSpaceSaga.CarrierWingSim/scripts/v4_update.py` | пересобирает V4.0 | `--check` сверяет и JSON; отказ записи не на glibc |
| `DB/Planning/V4.0/Reference/{battles,trace-seed-1,geometry,series}.json` | new | Эталоны |
| `DB/Planning/V4.0/README.md` | описание Reference | Формат JSON и требование glibc |

## Public API after the change
Формат JSON эталонов (описан в V4.0/README).

## Implementation steps
1. Запускать в WSL2/Linux; скрипт отказывается писать JSON, если `platform.libc_ver()` не glibc.
2. Перед записью — `v4_reference.py --check` по существующим md.
3. Сбор аргументов тригонометрии — обёртка вокруг `math.atan2/sin/cos/hypot` в режиме записи, без изменения результатов боёв.
4. Python-тесты инструмента зелёные; `v4_update.py --check` зелёный.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `DB/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Python-тесты инструмента на Linux/glibc. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `DB/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
