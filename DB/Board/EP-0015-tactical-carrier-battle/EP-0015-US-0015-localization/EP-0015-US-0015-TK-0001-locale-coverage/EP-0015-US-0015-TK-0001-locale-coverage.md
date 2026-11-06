---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0015-localization
ticket: EP-0015-US-0015-TK-0001-locale-coverage
title: locale-coverage
stage: done
layer: client
depends_on: ["EP-0015-US-0014-TK-0005-tools-cleanup"]
files_touched: 8
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# locale-coverage

## Why
Не допустить нелокализованных строк.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs` | new | Покрытие и симметрия ключей |

## Public API after the change
—

## Implementation steps
1. Проверка на реальных экранах боя.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before edits — 2026-11-06

The authoritative role catalog already defines Russian role names; the UI duplicated and changed one of those values. Project the display name alongside its stable ID on unit and journal rows and remove duplicate locale role strings. Display the catalog pilot name explicitly in the side card and test packaged locales and actual screens.

- Edit: src/DeepSpaceSaga.Client/Data/Locale/English.json
- Edit: src/DeepSpaceSaga.Client/Data/Locale/Russian.json
- Edit: src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleLogFormatter.cs
- Edit: src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSidesTab.cs
- Edit: src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs
- Edit: src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs
- Edit: src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs

## Execution evidence — 2026-11-06

RU/EN packaged key sets and format arity agree; direct and dynamic source keys resolve, and the retired world combat locale prefixes are absent. Authoritative catalog role names accompany stable role IDs in unit and journal rows; sides expose the pilot name from content. Both locale tests use the real setup, Engine session, rendered battle screen and actual role-change journal, verifying unchanged Russian V4 names. Scoped format, zero-warning build/diff, 167 Client, 380 Engine and 175 Contracts tests passed in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-tactical-locale-02/`. English editorial acceptance is outside scope.
