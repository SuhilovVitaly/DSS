---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0006-client-input-and-controls
ticket: EP-0015-US-0006-TK-0002-input-settings
title: input-settings
stage: done
layer: client
depends_on: ["EP-0015-US-0006-TK-0001-keyboard"]
files_touched: 5
serves: [AC-02]
created: 2026-11-05
revision: 3
---

# input-settings

## Why
Клавиша боя задаётся данными UI (D-USER-06).

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/UI/tactical-battle-input.json` | new | Клавиша боя, повтор |
| `src/DeepSpaceSaga.Client/TacticalBattleInputSettings.cs` | new | Чтение и проверка |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TacticalBattleInputSettingsTests.cs` | new | Неизвестная клавиша, дубли, конфликты, умолчания |

## Public API after the change
`TacticalBattleInputSettings.Load(path)`.

## Implementation steps
1. Конфликт — по фактическому содержимому `combat-input.json` (после US-0011 TK-0003 — файла клавиш маневрирования).
2. Ключи этого тикета — в `Data/Locale/Russian.json` (рабочий язык, D-USER-11) и теми же ключами в `English.json` черновым переводом без вычитки (правило R-07).

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-05

Add KeyboardEdgeTracker.cs (track configurable launch key and apply captured repeat timings) and SkiaWindow.cs (load actual current bindings at startup) to three planned positions. Four code files + one UI JSON; locales R-07 exempt. Strict schema1 rejects unknown/duplicate/missing fields, numeric key spellings, reserved/game-binding collisions and invalid timings; logs localized fallback with path/field. Repeat delay may be zero, period must be positive. If custom game bindings also occupy fallback F8, retain default values but mark ShortcutAvailable=false and explicitly log that conflict; a future clickable launch entry stays available, preventing two actions on one key. This fail-safe is a technical assumption, not a user decision. US-0007 must use ShortcutAvailable for keyboard entry/hint. Legacy combat bindings are not silently rewritten. The tracker still includes fixed F8/0/keypad keys from TK-0001.

## Execution evidence — 2026-11-05

AC-02 implemented with packaged schema1 F8/400/80, strict parsing and localized InterfaceLog fallback. Configured key joins native tracker and captured repeat timing; conflict validation uses actual passed or sibling combat-input bindings, including custom debugPanel/maneuver keys. Unknown/numeric/case-mismatched keys, duplicate/unknown/missing fields, schema/types/timing errors and missing files verified. Default F8 collision fails safe as documented above, without changing game controls.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-input-settings-01/: build 0 warnings/errors; scoped format/diff/self-review PASS; 103/103 Client cases PASS (35 new settings cases plus tactical/keyboard/held-input regression). Native physical input and independent review NOT RUN. UI launch follows US-0007, which must honor ShortcutAvailable. Separate immediate origin publication authorized.
