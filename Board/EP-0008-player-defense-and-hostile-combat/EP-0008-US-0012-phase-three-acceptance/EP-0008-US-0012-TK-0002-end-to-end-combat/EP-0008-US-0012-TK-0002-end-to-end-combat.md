---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0012-phase-three-acceptance
ticket: EP-0008-US-0012-TK-0002-end-to-end-combat
title: "Сквозное доказательство боя с настоящим AI"
stage: draft
layer: engine
depends_on: ["EP-0008-US-0012-TK-0001-compatibility-fixtures"]
files_touched: 3
serves: ["AC-0001","AC-0002","AC-0004"]
created: 2026-10-07
revision: 1
---

# Сквозное доказательство боя с настоящим AI

STATUS: DRAFT

## Why

Поставка проверяется на настоящем сценарии и нескольких враждебных кораблях. Автоматические проверки, нативное окно и результаты review фиксируются отдельно. Этот тикет обеспечивает: сквозное доказательство боя с настоящим ai.

## Decisions

Решения Q01–Q51 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0012-phase-three-acceptance.md): AC-0001, AC-0002, AC-0004. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeCombatEndToEndTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeDeterminismTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeMultiHostileTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Шаг **41** [общего порядка](../../Tickets.md).
- [EP-0008-US-0012-TK-0001-compatibility-fixtures](../../EP-0008-US-0012-phase-three-acceptance/EP-0008-US-0012-TK-0001-compatibility-fixtures/EP-0008-US-0012-TK-0001-compatibility-fixtures.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Production registry и generated PlayerShipOnly; scripted player commands, Enemy auto fire без forged user command. Детерминированные seed для hit/miss и0/100, custom nondefault modules, несколько enemies/PR/same-time events. Pre-death actual file saves, pause/accelerated/split continuation, no scenario relocation во время прогона. Не требовать попадания каждой торпеды при заведомо более быстром target.
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

- [x] `Real_battle_exercises_auto_and_manual_defense`
- [x] `Multiple_hostiles_share_no_player_authority`
- [x] `Single_step_and_partitioned_battle_match`
- [x] `Defeat_stops_world_and_rejects_save`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [x] Все AC-0001, AC-0002, AC-0004 в scope этой карточки покрыты реальным результатом/проверками.
- [x] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [x] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [x] Review проведён, ограничения/native статус описаны честно.
- [x] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [x] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution evidence — 2026-10-11

Dependency TK-0001 `0cdcc7af7b774096a307fc5520e92f4ac12acffc` published; live remote SHA verified before implementation.

- `Real_battle_exercises_auto_and_manual_defense_then_defeat_and_file_recovery`: actual production registry and generated PlayerShipOnly seed42, real LocalSession and Brake command, NPC pursuit/fire, auto PR plus second manual PR, actual current save file and ten-second continuation equality, three real hull hits, frozen defeat and rejected overwrite with unchanged bytes, new session from pre-death file. No runtime relocation or HP edits.
- `Single_step_and_partitioned_battle_match`: seeds1/42/99, generated full world, one30-minute physical interval versus irregular steps, Speed4 x100 and17-second real pauses. Objects, combat journal (including RNG resolutions), outcome and paused calendar/physical time agree.
- `Multiple_hostiles_share_no_player_authority`: two independently firing Enemy NPCs at the same initial boundary, no forged NPC command/receipt, two manual modules, exact0/100% outcomes, one RNG draw per encounter, stable large/split interval result. Custom PR speed9, turn45, range111, TTL8000 and reload7000 are configured before scenario load. This is a separate custom combat fixture, not a claim that default PlayerShipOnly starts with two enemies.

Initial multi-enemy fixture placed the second target behind the player: its PR legitimately expired before contact, so the expected two resolutions was invalid. The starting fixture now places both targets ahead; production guidance/TTL assertions were not weakened. Initial end-to-end horizon was too short for the default distant NPC; test now uses a player Brake command and sufficient physical time.

New matrix6/6 PASS (`D:/DeepSpaceSaga/ep8-us12tk2-matrix-final.log`). Full corpus-inclusive solution4302/4302 passed in prerequisite TK-0001; this ticket changes tests only. Native acceptance is separate TK-0003. Self-review checks real authority, time domains, file bytes and explicit test-fixture boundaries; no user approval inferred.

Final Engine regression1922/1922 PASS (all non-corpus tests, log `D:/DeepSpaceSaga/ep8-us12tk2-engine.log`); full corpus was passed immediately before this test-only ticket. Scoped whitespace verification and diffPASS. Final solution build recorded below before publication.

Final solution build PASS, 0 warnings/errors. Separate ticket commit and immediate push follow; live SHA is verified before TK-0003.
