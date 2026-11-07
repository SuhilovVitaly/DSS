---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0017-project-documentation
ticket: EP-0015-US-0017-TK-0002-documentation-new-contract
title: documentation-new-contract
stage: done
layer: documentation
depends_on: ["EP-0015-US-0017-TK-0001-impact-inventory"]
files_touched: 10
serves: [AC-02]
created: 2026-11-05
revision: 3
---

# documentation-new-contract

## Why
Каноническая документация нового боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/04-Engineering/{TacticalBattle,SaveFormat}.md` | new / Save19 | Контракт боя; Save20 |
| `DB/Documentation/01-Requirements/EngineRequirements.md` | §Бой о старом бое | Новый контракт |
| `DB/Documentation/02-FirstRelease/Screens/{ScreenCatalog,GameSession,TacticalBattleSetup,TacticalBattle}.md` | без боя V4 | Экраны (новые — `TacticalBattleSetup.md`, `TacticalBattle.md`) и клавиши |
| `DB/Documentation/02-FirstRelease/Mechanics/TacticalMapAndManeuvering.md` | клавиши карты | F8, клавиши во время боя |
| `DB/Documentation/06-Tooling/CarrierWingSim.md` | new | v4_update, экспорт, эталоны, LongRunning |

## Public API after the change
—

## Implementation steps
1. Числа — ссылками на JSON (DocumentationSystem.md).

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Ссылки, `git diff --check`, только .md в Documentation. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope confirmed before edits — 2026-11-06

Document the observed tactical battle, actual Save20 and input boundary, Windows/Linux validation procedure and supported data exporter. Link to the Board for parity, native and product status instead of asserting acceptance in the game contract.

- DB/Documentation/04-Engineering/TacticalBattle.md
- DB/Documentation/04-Engineering/SaveFormat.md
- DB/Documentation/02-FirstRelease/Screens/TacticalBattleSetup.md
- DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md
- DB/Documentation/02-FirstRelease/Screens/GameSession.md
- DB/Documentation/06-Tooling/CarrierWingSim.md
- DB/Documentation/01-Requirements/EngineRequirements.md
- DB/Documentation/02-FirstRelease/Screens/ScreenCatalog.md
- DB/Documentation/02-FirstRelease/Mechanics/TacticalMapAndManeuvering.md
- tools/tactical-battle/README.md

## Execution evidence — 2026-11-06

Wrote current TacticalBattle architecture and Save20, preparation and battle screen contracts, navigation, world controls and the independent V4 export/LongRunning guide. Data values link to owning files. Checked 75 relative links across the 10 touched documents (none broken) and clean diff.
