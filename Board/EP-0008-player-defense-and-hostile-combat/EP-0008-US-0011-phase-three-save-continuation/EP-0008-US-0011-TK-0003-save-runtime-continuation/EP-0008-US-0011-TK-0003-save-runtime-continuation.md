---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0011-phase-three-save-continuation
ticket: EP-0008-US-0011-TK-0003-save-runtime-continuation
title: "Атомарное восстановление активного боя и AI"
stage: draft
layer: engine
depends_on: ["EP-0008-US-0011-TK-0002-save-combat-validation"]
files_touched: 5
serves: ["AC-0002","AC-0003","AC-0004"]
created: 2026-10-07
revision: 1
---

# Атомарное восстановление активного боя и AI

STATUS: DRAFT

## Why

Сохранение новой игры восстанавливает обе установки, активные снаряды и поведение противников. Старые сохранения явно отклоняются, а стартовые сценарии остаются доступными. Этот тикет обеспечивает: атомарное восстановление активного боя и ai.

## Decisions

Решения Q14,Q25,Q28,Q42,Q49 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0011-phase-three-save-continuation.md): AC-0002, AC-0003, AC-0004. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs:6 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs` | Создаётся предыдущим тикетом EP-0008-US-0004-TK-0003-hostile-controller | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.PlayerDefeat.cs` | Создаётся предыдущим тикетом EP-0008-US-0009-TK-0002-defeat-transition | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.cs:17 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeSaveContinuationTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Шаг **38** [общего порядка](../../Tickets.md).
- [EP-0008-US-0011-TK-0002-save-combat-validation](../../EP-0008-US-0011-phase-three-save-continuation/EP-0008-US-0011-TK-0002-save-combat-validation/EP-0008-US-0011-TK-0002-save-combat-validation.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Stage immutable validated new runtime до world replacement; сохранить next controller/fire boundary, command receipts и RNG counter. Restore не генерирует map/crew/handedness и не сбрасывает attempt/TTL. Уничтоженное состояние не сериализовать; clean reset terminal на загрузке живой сессии. Сравнить uninterrupted vs actual serialized continuation на фазах pursuit/orbit/guiding/miss/reload.
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

- [x] `Two_launchers_resume_with_distinct_deadlines`
- [x] `Reload_does_not_reset_automatic_attempt`
- [x] `Orbit_handedness_and_rng_continue_exactly`
- [x] `Saved_geography_and_voyage_finance_are_preserved`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [x] Все AC-0002, AC-0003, AC-0004 в scope этой карточки покрыты реальным результатом/проверками.
- [x] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [x] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [x] Review проведён, ограничения/native статус описаны честно.
- [x] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [x] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution evidence — 2026-10-11

Capture/atomic hostile restore shipped together in prerequisite TK-0002 to avoid an invalid intermediate writer. This ticket adds 11 real-file continuation cases through strict current-version loader and fresh Engine: pursuit/orbit at middle and exact controller boundary; both manual launchers in Guiding/MissedCoast/Reloading with distinct TTL/reload deadlines; durable command identity; automatic attempt retention; exact named RNG/journal/whole-save equality; A07 next-shot floor(last termination)+1 boundary; registry-stage invalid engine rejection without world replacement; generated geography and active voyage finance preservation. Existing defeat tests prove live Load clears terminal state. No unnecessary production rewrite.

Validation: initial9 cases8/9 corrected test fixture (automatic defense requires positive chance). Full Engine1915/1916 exposed only a new test's hard-coded integer contact assumption; replaced it with the contractual floor(actual precise hit)+1 assertion. Final affected254/254 PASS, including all11 new cases; scoped format verification, build and diff PASS. Self-review against AC-0002/0003/0004 complete. Native acceptance/final corpus OPEN. Separate commit/push; remote SHA recorded by next ticket.
