---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0009-authoritative-player-defeat
ticket: EP-0008-US-0009-TK-0003-defeat-clock-boundary
title: "Остановка времени на событии поражения"
stage: draft
layer: engine
depends_on: ["EP-0008-US-0009-TK-0002-defeat-transition"]
files_touched: 5
serves: ["AC-0001","AC-0002"]
created: 2026-10-07
revision: 1
---

# Остановка времени на событии поражения

STATUS: IMPLEMENTED — self-reviewed; publication recorded by the next execution entry

## Why

При нуле HP корабля игрока поражение фиксируется в Engine. Мир останавливается на границе гибели и не продолжает бой или экономику в остатке большого шага. Этот тикет обеспечивает: остановка времени на событии поражения.

## Decisions

Решения Q27–Q28 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0009-authoritative-player-defeat.md): AC-0001, AC-0002. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationClock.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationClock.cs:9 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.PlayerDefeat.cs` | Создаётся предыдущим тикетом EP-0008-US-0009-TK-0002-defeat-transition | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:6 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/DefeatTimeBoundaryTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Шаг **32** [общего порядка](../../Tickets.md).
- [EP-0008-US-0009-TK-0002-defeat-transition](../EP-0008-US-0009-TK-0002-defeat-transition/EP-0008-US-0009-TK-0002-defeat-transition.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Обрезать обработку большого physical/calendar interval на первом terminal boundary, синхронизировать clock baselines и processed cursors; не проводить остаточные production/meal/RNG/AI события. Lock order согласовать с UpdateAndCapture; Speed0 не только client request. Восстановление modal прежней скорости не может снять terminal stop; новая загруженная сессия независима.
3. Реализовать перечисленные ниже регрессии/проверки. Изменение прежних ожиданий допустимо только при изменённом согласованном контракте; не ослаблять независимые assertions.
4. Review относительно AC и контракта эпика; исправить подтверждённые findings и повторить затронутые проверки.
5. При исполнении через общий промт: записать evidence, отдельный commit с ID тикета и push; не смешивать другой тикет.

## Invariants и Out of scope

- Architecture и временные домены — [эпик](../../Documentation.md): snapshot immutable, Engine authoritative, physical/calendar/real-time разделены.
- Нельзя выдавать full-suite green за доказательство ненаписанного сценария; native проверяется отдельно.
- Недопустимы неоговорённые fuel/ammo/патрульные механики, user commands от имени NPC, скрытые изменения характеристик и повторные RNG после Load.
- Новая фаза требует current-format save; version0 NewGame не путать со старым save.
- Другие production layers не реализуются в этой карточке; необходимое расширение оформляется до изменения.

## Tests и проверки

Имена ниже **планируемые**, не результат выполненного тестирования:

- [x] `Lethal_hit_in_large_step_stops_all_systems`
- [x] `Split_steps_have_same_defeat_boundary`
- [x] `Resume_and_modal_close_cannot_restart_defeated_world`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [x] Все AC-0001, AC-0002 в scope этой карточки покрыты реальным результатом/проверками.
- [x] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [x] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [x] Review проведён, ограничения/native статус описаны честно.
- [x] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [x] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution evidence — 2026-10-11

### Correction scope — 2026-10-12 final review

Confirmed A09 discrepancy: DefeatedClock used floor(death), exposing a session timestamp before a fractional lethal hit. Correct it to ceiling(death), retaining exact event/calendar cutoff and the terminal latch. Existing allowlist covers PlayerDefeat.cs and DefeatTimeBoundaryTests.cs. Add a real fractional-hit whole/split regression; publish a separate correction commit under this ticket before completing final review. No native runs.

Dependency `f7b8ecd753e17f5c762ffbea086bb8033becd944` published/remote verified. SimulationClock now latches the first terminal boundary: direct speed changes, baseline reset and repeated updates cannot resume it; validated new-world Reset clears the latch. Player defeat uses this operation under the existing world-lock then clock-lock order. World cutoff/processed cursors introduced necessarily in TK-0002 are exercised by the new tests without redundant scheduler changes.

Three regressions cover real large clock advancement into a lethal hit, identical whole/split death boundaries, station production input reservation without later completion, no meal/ship events or residual launches, frozen runtime world/RNG/history, repeated direct and Engine speed changes, and independent New Game clocks. Initial full Engine1844/1845 exposed a new test attempting to JSON-serialize default internal ImmutableArrays; replaced with direct immutable runtime record comparison. Final affected Engine113/113 PASS (Defeat, SimulationClock, EconomyTimeContinuity), solution build0warnings/errors, scoped format verify/diffPASS. Initial focused fixture assumption that station inventory contained only its one explicit row was corrected to identify the ration row; default catalog rows remain enabled. Self-review complete, not independent approval. Native modal integration remains US-0010/0012.

Correction validation: new real fractional-hit regression failed before the fix (expected5002, actual5001), then affected Engine196/196 passed in Release (Defeat, PhaseThree, SimulationClock, EconomyTimeContinuity). Build via test succeeded; scoped format/diff PASS. Exact death/calendar event remains unchanged; only integer terminal MotionTimeMs now follows A09 ceiling. No game launched. Logs: D:/DeepSpaceSaga/ep8-defeat-ceiling-red.log and ep8-defeat-ceiling-green.log.
