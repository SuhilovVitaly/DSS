---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0017-project-documentation
ticket: EP-0015-US-0017-TK-0003-documentation-legacy-rewrite
title: documentation-legacy-rewrite
stage: done
layer: documentation
depends_on: ["EP-0015-US-0017-TK-0002-documentation-new-contract"]
files_touched: 17
serves: [AC-03]
created: 2026-11-05
revision: 3
---

# documentation-legacy-rewrite

## Why
Документация без отменённого контракта.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `DB/Documentation/04-Engineering/{PointAimCombat,CombatReports,DebugTuning,DebugTuningPanel,ManualManeuver,NpcObservedTrajectories}.md` | старый бой | Удалить или переписать |
| `DB/Documentation/01-Requirements/FirstReleaseRequirements.md, DB/Documentation/02-FirstRelease/Mechanics/CommandPanels.md` | старый бой | Переписать |
| `DB/Documentation/02-FirstRelease/Screens/{Save,Defeat}.md, DB/Documentation/03-Design/TacticalMapSpecification.md` | старый бой | Переписать или удалить |
| `DB/Documentation/06-Tooling/GameSessionScreenUI.md, DB/Documentation/README.md, DB/Documentation/00-Process/CLAUDE.md` | старый бой, ссылки | Переписать |

## Public API after the change
—

## Implementation steps
1. Не помечать — удалять или переписывать (DocumentationSystem.md).
2. Ссылки на удалённые документы исправить.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Ссылки, `git diff --check`, поиск упоминаний старого боя. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-03 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope confirmed before edits — 2026-11-06

Rewrite the live world, UI, input, debug, save and tooling contracts after realtime combat retirement. Delete three obsolete live descriptions; old runs and writer procedures remain historical in Git/Board. Update dependent navigation and source/tool documentation without modifying noncanonical VData copies.

- DB/Documentation/04-Engineering/DebugTuning.md
- DB/Documentation/04-Engineering/DebugTuningPanel.md
- DB/Documentation/04-Engineering/ManualManeuver.md
- DB/Documentation/04-Engineering/NpcObservedTrajectories.md
- DB/Documentation/02-FirstRelease/Mechanics/CommandPanels.md
- DB/Documentation/02-FirstRelease/Screens/Save.md
- DB/Documentation/03-Design/TacticalMapSpecification.md
- DB/Documentation/06-Tooling/GameSessionScreenUI.md
- DB/Documentation/01-Requirements/FirstReleaseRequirements.md
- DB/Documentation/README.md
- DB/Documentation/00-Process/CLAUDE.md
- DB/Documentation/06-Tooling/GraphifyRebuild.md
- tools/DeepSpaceSaga.Performance/README.md
- tests/DeepSpaceSaga.Engine.Tests/Fixtures/Saves/README.md
- DB/Documentation/04-Engineering/PointAimCombat.md
- DB/Documentation/04-Engineering/CombatReports.md
- DB/Documentation/02-FirstRelease/Screens/Defeat.md

## Execution evidence — 2026-11-06

Removed PointAimCombat, CombatReports and Defeat from current documentation; updated Save screen, world controls/modules/forecast, engine tuning, requirements, navigation, design and live tooling. Historical writer procedure and evidence remain historical; source links point to current owners. All 300 relative links across 60 canonical and adjacent guide paths passed; Markdown-only and stale live references checks passed, diff clean.
