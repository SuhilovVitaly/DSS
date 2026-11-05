---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0002-deterministic-core
ticket: EP-0015-US-0002-TK-0004-geometry
title: geometry
stage: done
layer: motion
depends_on: ["EP-0015-US-0002-TK-0003-mulberry32"]
files_touched: 4
serves: [AC-04]
created: 2026-11-05
revision: 3
---

# geometry

## Why
Общая геометрия боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Motion/TacticalBattle/TacticalGeometry.cs` | new | norm, bearing, vec, dist (порт vector_norm), clamp, Path/Integrate |
| `tests/DeepSpaceSaga.Motion.Tests/TacticalBattle/GeometryTests.cs` | new | Сверка с geometry.json бит в бит; версия CPython |

## Public API after the change
`static class TacticalGeometry`.

## Implementation steps
1. `norm` через `%` (fmod) и правило Combat §1, крайний случай не исправлять.
2. `dist` — порт `vector_norm` CPython той версии, что в заголовке эталона (ссылка на тег исходника в комментарии).
3. `clamp` — `PyMath`.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Motion.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-04 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-05

Geometry fixture header is CPython 3.12.3 / glibc 2.39, measured on Linux in TK-0002. Port mathmodule.c v3.12.3 vector_norm for two elements, including dl_fast_sum and FMA product residual; `Math.ILogB` + `Math.ScaleB` implement frexp/ldexp. Add `src/DeepSpaceSaga.Motion/TacticalBattle/CPython-LICENSE.txt` for required copyright/license and summary of the C# adaptation. No RNG or content loading enters Motion. Tests verify independent edge images, signed axes and movement order. Any observed Windows trig difference is a measured G-03 input to TK-0006; do not substitute a tolerance for its bitwise gate.
Additional owning-layer scope before packaging edit: src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj copies CPython-LICENSE.txt to Licenses/ in build/publish output so binary distribution retains the upstream notice. No gameplay contract changes.

## Execution evidence — 2026-11-05

Implemented pure geometry and allocation-free Integrate plus immutable start/substep Path. Course normalizes only after the final substep. Hypot is the n=2 CPython 3.12.3 FMA vector_norm port, including scaling subnormals and infinity-before-NaN behavior. License and adaptation summary are retained and copied transitively to Client build/publish output (SHA256 source/output equal).

`D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261105-geometry-01/`: final solution build 0 warnings/errors; full Motion 1372/1372 (1088 independent Linux/glibc vectors plus 4 behavior tests added); Engine TacticalBattle 96/96; scoped format/diff PASS. Every fixture value, including raw .NET sin/cos/atan2 on Windows, matched exact IEEE bits. No tolerance substituted. Path tests verify substep order and course crossing ±180. Independent review NOT RUN; self-review completed. Linux parity of this newly published geometry follows in CI/TK-0006; no claim of all actual battle arguments, absent from original MD (D-USER-15).

Previous RNG ticket passed Linux run 42087606609 at SHA 53144cdbace23309b2f18889af102caf8fd6b680. Own publication uses the user GitHub override and SHA is checked after commit.