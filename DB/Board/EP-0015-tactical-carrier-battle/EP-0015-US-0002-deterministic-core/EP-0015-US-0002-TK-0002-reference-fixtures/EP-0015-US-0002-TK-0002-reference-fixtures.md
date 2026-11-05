---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0002-deterministic-core
ticket: EP-0015-US-0002-TK-0002-reference-fixtures
title: reference-fixtures
stage: done
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
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tools/DeepSpaceSaga.CarrierWingSim/scripts/v4_reference.py` | пишет md | Ещё JSON эталонов AC-02, заголовок версий, IEEE-hex, структурная причина, броски по этапам |
| `tools/DeepSpaceSaga.CarrierWingSim/scripts/v4_update.py` | пересобирает V4.0 | `--check` сверяет и JSON; отказ записи не на glibc |
| `VData/Planning/V4.0/Reference/{battles,trace-seed-1,geometry,series}.json` | new | Эталоны |
| `VData/Planning/V4.0/README.md` | описание Reference | Формат JSON и требование glibc |

## Public API after the change
Формат JSON эталонов (описан в V4.0/README).

## Implementation steps
1. Запускать в WSL2/Linux; скрипт отказывается писать JSON, если `platform.libc_ver()` не glibc.
2. Перед записью — `v4_reference.py --check` по существующим md.
3. Сбор аргументов тригонометрии — обёртка вокруг `math.atan2/sin/cos/hypot` в режиме записи, без изменения результатов боёв.
4. Python-тесты инструмента зелёные; `v4_update.py --check` зелёный.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Python-тесты инструмента на Linux/glibc. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-05

D-USER-14/15 replaces the missing simulator export/check with a source-only Markdown converter. Code context now: `tools/tactical-battle/export_references.py` (new, standard-library Python converter, no battle simulation); `.github/workflows/tactical-battle.yml` (new Ubuntu Linux/glibc validation/export route); `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ReferenceFixtureTests.cs` (new schema/coverage, source fingerprint and IEEE parser tests); `VData/Planning/V4.0/Reference/{battles,trace-seed-1,geometry,series}.json` (generated outputs); `VData/Planning/V4.0/README.md` (format/provenance/coverage). Four code/context positions plus source README, replacing the old seven positions. Game content and existing reference Markdown stay unchanged.

The converter refuses writes off Linux/glibc. Geometry edge vectors are independent CPython/glibc evaluations with IEEE hex. Battle/trace/series expectations are parsed only from existing Markdown. Header records actual exporter Python/glibc/OS and source hashes; it does not claim those were the original simulator environment. Original unrounded trace bits, per-weapon/role statistics, stage calls for all 90 battles and actual trig arguments are absent; coverage lists them explicitly. Series rounded averages remain decimals, not invented integer sums. Linux bootstrap infrastructure may be published as a service commit to execute this validation before the ticket is marked done; the ticket commit then includes verified generated evidence and Windows parser tests.
## Execution evidence — 2026-11-05

Linux bootstrap service commits `a83a446b55323d231bd508299ab1104f896b3c4b` and `3ddc28dcb18fd2a0d4b4fb36f7783542f4f01285` published and remote SHA verified. First workflow was rejected before jobs due to runner context in job env; corrected to an absolute /tmp validation root. Run https://github.com/SuhilovVitaly/DSS/actions/runs/42086320971 succeeded: CPython 3.12.3, glibc 2.39, Ubuntu 24.04. Export/check 4 JSON PASS, content check 29 JSON PASS, solution build PASS, Motion 280/280, Engine TacticalBattle 71/71, Client content 2/2. Generated JSON was retrieved from that Linux job's exact chunked output, not computed on Windows.

Windows evidence `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-reference-01/`: solution build 0 warnings/errors, Engine TacticalBattle 87/87 (16 fixture tests added), scoped format/diff PASS. `linux-bootstrap.log` retains the Linux run log except the bulky JSON transfer lines; generated files themselves are tracked evidence. All 90 rows/counters, source fingerprints, 19 trace turns and 1088 geometry edge vectors checked. New parser tests run on Linux on the ticket's published SHA; that follow-up result is recorded in the ledger when observed.

Self-review: no battle engine imported or invoked; no source Markdown changed; no expected missing fields synthesized. CI checks tracked fixtures before export once fixtures exist. --check compares data and stable source metadata, preserving original export environment provenance across kernel updates. Missing original evidence stays listed in each header. Independent review NOT RUN. Full battle and trig-platform parity remain dependent TK-0004/TK-0006/US-0004 gates, not claimed by fixture conversion.