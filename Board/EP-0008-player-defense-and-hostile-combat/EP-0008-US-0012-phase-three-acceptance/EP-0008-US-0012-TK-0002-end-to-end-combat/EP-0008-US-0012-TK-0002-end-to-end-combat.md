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

Шаг **40** [общего порядка](../../Tickets.md).
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

- [ ] `Real_battle_exercises_auto_and_manual_defense`
- [ ] `Multiple_hostiles_share_no_player_authority`
- [ ] `Single_step_and_partitioned_battle_match`
- [ ] `Defeat_stops_world_and_rejects_save`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0001, AC-0002, AC-0004 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.
