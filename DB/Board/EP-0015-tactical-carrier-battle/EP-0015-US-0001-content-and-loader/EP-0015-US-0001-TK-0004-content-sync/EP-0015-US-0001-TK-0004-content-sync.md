---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0001-content-and-loader
ticket: EP-0015-US-0001-TK-0004-content-sync
title: content-sync
stage: done
layer: tooling
depends_on: ["EP-0015-US-0001-TK-0003-content-loader"]
files_touched: 7
serves: [AC-04]
created: 2026-11-05
revision: 3
---

# content-sync

## Why
Не допустить расхождения игры и V4.0 после ребаланса.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tools/tactical-battle/Export-Content.ps1` | экспорт Markdown TK-0001 | -Check и изолированный -OutputDirectory |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ContentSyncTests.cs` | new | Семантическое равенство игрового контента и V4.0/Data, порядок ключей |
| `VData/Planning/V4.0/README.md` | раздел «Как менять» | Шаг экспорта в игру |
| `VData/Planning/V4.0/Spec/Generation.md` | §2 «любое число дронов» | Привести к проверке «≥ 1 дрона на сторону» |

## Public API after the change
CLI `--export-game`; тест ищет корень репозитория вверх от каталога тестов.

## Implementation steps
1. Экспорт копирует только JSON.
2. Тест сравнивает каждый файл по значениям и порядку ключей.
3. Описать порядок в V4.0/README; `v4_update.py --check` зелёный.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Engine.Tests; Python `python -m unittest discover -s tests` в инструменте. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-04 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-05

D-USER-14/15 replaces the absent Python tool with the existing PowerShell Markdown exporter. Add `tools/tactical-battle/README.md` for the supported workflow and update `VData/Planning/V4.0/Data/README.md` to describe Markdown as the current source. Six files total: exporter, sync tests, tool README, source README, Data README and Generation.md. The CLI is `pwsh -File tools/tactical-battle/Export-Content.ps1 [-Check] [-OutputDirectory <path>]`. Independent C# tests compare all table cells, optional fields and ordered catalogs/scenario units; a corrupted isolated JSON fixture must make -Check fail without writing it. Existing Reference Markdown is never regenerated from the new engine.
Generated output in scope: src/DeepSpaceSaga.Client/Data/TacticalBattle/scenarios.json. Markdown has no separate description; exporter uses the source scenario name rather than a hardcoded composition string. No gameplay values change.

## Execution evidence — 2026-11-05

Markdown-to-JSON export is the supported workflow. Independent semantic tests cover every source table, all 29 files, field absence, exact scalar values and ordered catalogs/units. CLI check passes production content and rejects a corrupted isolated fixture without overwriting it. Unicode minus normalization applies only to numbers; labels remain unchanged. Generation §2 now requires at least one drone on each side. Existing reference evidence is preserved.

`D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-sync-01/`: export/check PASS (29 JSON); solution build 0 warnings/errors; Engine TacticalBattle 71/71, Client content 2/2; scoped format verification and diff PASS. Fixture directories cleaned. Review: source/target values, order and production loader integration verified; independent review NOT RUN. Prior full baseline/regression retained as recorded, not freshly rerun. User GitHub override applies to publication; SHA verified after commit.

Story review: all four content tickets implemented. Real Engine bootstrap and invalid-data world continuity are tested in TK-0003. The future Client availability/error presentation portion of AC-03 remains open in US-0007. No tactical battle can yet be played; remaining epic stories are open.