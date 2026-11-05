---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0009-authoritative-player-defeat
ticket: EP-0008-US-0009-TK-0002-defeat-transition
title: "Атомарная гибель игрока и запрет действий"
stage: implemented
layer: engine
depends_on: ["EP-0008-US-0009-TK-0001-defeat-contract"]
files_touched: 6
serves: ["AC-0001","AC-0002","AC-0003"]
created: 2026-10-07
revision: 1
---

# Атомарная гибель игрока и запрет действий

STATUS: IMPLEMENTED; self-review complete; dedicated clock boundary verification follows TK-0003

## Why

При нуле HP корабля игрока поражение фиксируется в Engine. Мир останавливается на границе гибели и не продолжает бой или экономику в остатке большого шага. Этот тикет обеспечивает: атомарная гибель игрока и запрет действий.

## Decisions

Решения Q27–Q28 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0009-authoritative-player-defeat.md): AC-0001, AC-0002, AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.PlayerDefeat.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/PlayerDefeatTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

Execution scope update — 2026-10-11: baseline `a4b8a71b1b80be55f0ad948e9dccd4d47ab4d0b2` published/remote verified. Add `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs` for an immediate return at terminal motion boundary: without it, the transition would still finish later economic work in the same call. This is required for a safe actual Engine transition; dedicated clock terminal latch, boundary/cursor and equivalence regressions remain TK-0003. Review scope addition: also add `src/DeepSpaceSaga.Engine/SimulationEngine.Time.cs`; station travel bypasses the ordinary command dispatcher and could reset the clock/change district after a lethal hit during travel. Guard both entry and post-advance before side effects. Effective scope six files.

## Dependencies

Шаг **31** [общего порядка](../../Tickets.md).
- [EP-0008-US-0009-TK-0001-defeat-contract](../EP-0008-US-0009-TK-0001-defeat-contract/EP-0008-US-0009-TK-0001-defeat-contract.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. На lethal impact сохранить outcome до удаления player object, остановить новые commands/AI и сохранить финальный журнал/wreck. CaptureSaveState guard централизованный: ни quick/manual/auto путь не обойдёт его. No player object не вызывает исключение в snapshot/cargo/focus, no automatic victory при гибели врага.
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

- [x] `Three_actual_hits_defeat_player_once`
- [x] `Defeated_world_rejects_save_and_gameplay`
- [x] `Final_snapshot_without_player_is_valid`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [x] Все AC-0001, AC-0002, AC-0003 в scope этой карточки покрыты реальным результатом/проверками.
- [x] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [x] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [x] Review проведён, ограничения/native статус описаны честно.
- [x] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [x] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution evidence — 2026-10-11

Dependency `a4b8a71b1b80be55f0ad948e9dccd4d47ab4d0b2` published/remote verified. Lethal player hit records outcome before removing the ship, preserves Hit/Destroyed facts and wreck identity, performs lost-PR cleanup without further RNG, then stops combat/AI and remaining world advancement. Snapshot repeatedly publishes the terminal outcome at its mapped death boundary; absent player yields valid empty modules/crew/trade projection. Pending gameplay commands and dialogue are rejected with session_defeated; Engine speed changes and station travel cannot resume it. Central save guard runs both before and after advancement, so a save that itself reaches death also fails. Invalid Load keeps the old outcome; validated new world resets it; enemy death does not terminate play.

Five new transition tests include three actual150HP impacts, repeated terminal snapshots, command families/dialogue/station travel/save rejection, lethal save advancement, atomic invalid Load and valid New Game, and enemy-only destruction. Full Engine **1842/1842 PASS** excluding final CorrectnessCorpus (`ep8-us9-tk2-engine.trx`); affected Client **130/130 PASS** (`ep8-us9-tk2-client.trx`). Solution build0warnings/errors, scoped format verify/diffPASS. Own review complete. Dedicated clock latch, production/meal/contract cutoff and partition-equivalence proof follow TK-0003; defeat UI/save transport handling remains US-0010.
