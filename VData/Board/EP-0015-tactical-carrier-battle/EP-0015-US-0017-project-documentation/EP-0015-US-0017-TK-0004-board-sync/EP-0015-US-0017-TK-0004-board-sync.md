---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0017-project-documentation
ticket: EP-0015-US-0017-TK-0004-board-sync
title: board-sync
stage: draft
layer: documentation
depends_on: ["EP-0015-US-0017-TK-0003-documentation-legacy-rewrite"]
files_touched: 12
serves: [AC-04]
created: 2026-11-05
revision: 3
---

# board-sync

## Why
Board и evidence.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `DB/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `DB/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `DB/Board/{EP-0006-basic-torpedo-combat,EP-0007-countermeasure-combat,EP-0008-player-defense-and-hostile-combat,EP-0009-battle,EP-0010-combat-reports,EP-0012-combat-rebuild,EP-0013-debug-tuning-panel,EP-0014-module-panels}/Documentation.md` | действующие описания старого боя | Запись Superseded |
| `DB/Board/EP-0015-tactical-carrier-battle/{Documentation,Tickets,AcceptanceCoverage,ExecutionStatus}.md` | план | Статусы, evidence, Graphify |

## Public API after the change
—

## Implementation steps
1. Соответствие тикетов и SHA.
2. Graphify — локально, результат в ExecutionStatus.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `DB/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Проверка ссылок и статусов. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-04 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `DB/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
