---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0017-project-documentation
ticket: EP-0015-US-0017-TK-0001-impact-inventory
title: impact-inventory
stage: draft
layer: documentation
depends_on: ["EP-0015-US-0001-TK-0004-content-sync", "EP-0015-US-0002-TK-0006-platform-determinism", "EP-0015-US-0003-TK-0004-turn-and-outcome", "EP-0015-US-0004-TK-0004-golden-battles", "EP-0015-US-0005-TK-0006-session-tests", "EP-0015-US-0006-TK-0006-text-and-numbers", "EP-0015-US-0007-TK-0004-battle-keys-guard", "EP-0015-US-0008-TK-0005-movement-animation", "EP-0015-US-0009-TK-0004-forecast-tab", "EP-0015-US-0010-TK-0002-sandbox-flow", "EP-0015-US-0011-TK-0004-save-format-20", "EP-0015-US-0012-TK-0006-client-assets-locale-removal", "EP-0015-US-0013-TK-0010-legacy-save-dto-removal", "EP-0015-US-0014-TK-0005-tools-cleanup", "EP-0015-US-0015-TK-0001-locale-coverage", "EP-0015-US-0016-TK-0003-native-acceptance"]
files_touched: 1
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# impact-inventory

## Why
Инвентаризация.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `DB/Board/EP-0015-tactical-carrier-battle/DocumentationImpact.md` | заготовка | Перечень влияния |

## Public API after the change
—

## Implementation steps
1. По итоговому diff, API, командам, конфигурации, версиям схем; поиск упоминаний старого боя по `DB/Documentation`.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Проверка полноты. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
