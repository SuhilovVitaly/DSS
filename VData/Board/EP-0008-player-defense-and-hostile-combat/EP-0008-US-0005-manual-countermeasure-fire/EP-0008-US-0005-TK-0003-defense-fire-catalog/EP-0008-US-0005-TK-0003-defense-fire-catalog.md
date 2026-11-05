---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0005-manual-countermeasure-fire
ticket: EP-0008-US-0005-TK-0003-defense-fire-catalog
title: "Ручная команда в каталоге ПРО"
stage: implemented
layer: content-data
depends_on: ["EP-0008-US-0005-TK-0002-manual-launch-execution"]
files_touched: 4
serves: ["AC-0001"]
created: 2026-10-07
revision: 1
---

# Ручная команда в каталоге ПРО

STATUS: IMPLEMENTED (automated checks; self-review)

## Why

Игрок выбирает вражескую торпеду и запускает ПРО с конкретного готового аппарата на расстоянии до200км. Он может рисковать при нулевом шансе или неизвестной встрече, повторять попытки и запускать две ПРО по одной цели. Этот тикет обеспечивает: ручная команда в каталоге про.

## Decisions

Решения Q05–Q11,Q16,Q25–Q26,Q29,Q31,Q35,Q48 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0005-manual-countermeasure-fire.md): AC-0001. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Commands/CountermeasureLauncher/commands.json` | Существует: src/DeepSpaceSaga.Client/Data/Commands/CountermeasureLauncher/commands.json:4 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Client.Tests/CountermeasureContentTests.cs` | Существует: tests/DeepSpaceSaga.Client.Tests/CountermeasureContentTests.cs:6 (baseline 6907c58) | Только результат и шаги этой карточки |

## Execution scope update — 2026-10-11

Baseline `83166f8` published/remote verified. Add `tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs` to update its obsolete automatic-only command catalog expectation while retaining module values and target assertions.

Catalog scope correction before edit: add `src/DeepSpaceSaga.Client/Data/module-types.json`; module category owns commandTypeIds independently of the command definition file. Source/output tests reproduced the missing binding, so both declarations must change together.

## Dependencies

Шаг **18** [общего порядка](../../Tickets.md).
- [EP-0008-US-0005-TK-0002-manual-launch-execution](../EP-0008-US-0005-TK-0002-manual-launch-execution/EP-0008-US-0005-TK-0002-manual-launch-execution.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Добавить модульную defense.fire с явным target; сохранить enable/disable. Команда не заводит generic command-cycle cooldown поверх flight/reload. Проверить registry cross-check command type, real file load и готовность двух module instances.
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

- [x] `Defense_fire_is_registered_for_countermeasure_module`
- [x] `Toggle_commands_remain_available`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [x] Все AC-0001 в scope этой карточки покрыты реальным результатом/проверками.
- [x] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [x] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [x] Review проведён, ограничения/native статус описаны честно.
- [x] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [x] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.

## Execution evidence — 2026-10-11

Dependency `83166f856df987c718d97a6eec5de4f92ef197a3` published/remote verified. Real source/output catalogs bind defense.fire to the countermeasure category with explicit object target and zero energy cost; Enable/Disable remain available. Both real player module instances start Ready without generic active command cooldown.

Initial focused catalog tests exposed missing category binding (4 failures); corrected module-types.json as documented before edit. Full Client **1780/1780 PASS** (`ep8-us5-tk3-client.trx`); affected Engine ScenarioEngineTests/ManualCountermeasureFireTests **93/93 PASS** (`ep8-us5-tk3-engine.trx`). Client build 0 warnings/errors, scoped whitespace verify and git diff --check PASS. Self-review complete; native/final corpus OPEN. Separate ticket commit/push is the required publication gate before TK-0004.
