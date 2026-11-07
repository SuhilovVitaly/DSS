---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0006-client-input-and-controls
ticket: EP-0015-US-0006-TK-0006-text-and-numbers
title: text-and-numbers
stage: done
layer: client
depends_on: ["EP-0015-US-0006-TK-0005-widgets-lists", "EP-0015-US-0006-TK-0007-linux-native-runtime"]
files_touched: 5
serves: [AC-05]
created: 2026-11-05
revision: 3
---

# text-and-numbers

## Why
Тексты и числа как в справочной странице.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleFormat.cs` | new | Инвариантные числа, банковское округление, знак «−» |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/GlyphCoverageTests.cs` | new | Покрытие символов шрифтами экранов боя на Windows и Linux |

## Public API after the change
`BattleFormat`.

## Implementation steps
1. Культура ru-RU не меняет разделитель.
2. `ToString("F0")` запрещён (половина от нуля).

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-05

Five Client code files: two planned plus Controls/DropDown.cs (shared symbol substitution cache used before measuring/wrapping), TabStrip.cs (generic labels), ShortcutButton.cs (generic button captions). Visual run widgets-lists-02/03 confirmed Verdana arrow missing; fix actual shared rendering, not only a test formatter. Keep existing typefaces, use required symbols where all new-control regular/bold faces support them, otherwise verified ASCII equivalents. Lazy immutable mapping shared across MenuStyle/Xenon font faces, no per-frame glyph probes; old game rendering unchanged. BattleFormat provides invariant fixed decimals, explicit ToEven before formatting, normalized signed zero, signed/percent/degree helpers, exact long seed and localized safe text. Source text/log data remain intact; display substitutes only. Glyph and numeric tests run Windows/Linux; CPU raster not physical/native acceptance.

## Windows execution evidence — 2026-11-05

BattleFormat and actual new-control rendering implemented. Windows Verdana glyph ID for arrow U+2192 is0; display substitutes ASCII -> through cached shared font checks while source Text/log stays intact. Existing font family preserved. Explicit ToEven precedes invariant fixed-decimal formatting; signed zero normalized; exact signed64 seed avoids double conversion. Non-finite presentation uses a supported dash; command rejection remains Engine-owned.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-text-01/: build0 warnings/errors, scoped format/diff/self-review PASS; Client113/113 (new numeric/glyph cases plus full tactical controls and existing text input), Engine378/378 including source guards and all180 connection battles PASS. glyph-coverage.json records actual font glyph IDs and substitutions; glyphs.png and renders/widgets-lists-2.png visually inspected: arrow square replaced, captions/tooltip readable. CPU raster only. Linux glyph validation pending next CI push; ticket remains in-progress until that required gate. Code commit is published to trigger Linux; final evidence update follows before next ticket.

## Required Linux gate and completion — 2026-11-05

TK-0007 prerequisite completed/published. Run42113068311 / job127545443551 at4080197095f45cb7cce7ec48ff846d08867c61a7 PASS: Motion1376, Engine378, Tactical Client97. All numeric/glyph/raster checks execute with actual Linux native Skia and DejaVu Sans; original required symbols supported there, while Windows Verdana arrow gets supported ASCII display replacement. No assertions weakened or tests skipped. Linux glyph JSON/image and dependency log retained in D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-linux-runtime-01/; linux-glyphs.png visually inspected and readable. Earlier failed run42112081477 remains recorded as native-runtime reproduction, not PASS. AC-05 complete for available Windows/Linux fonts. Native window/input/human acceptance remains open. Code cab5d313 was pushed/verified; final evidence commit separately published.
