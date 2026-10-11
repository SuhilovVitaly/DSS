---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0006-lifetime-and-auto-arbitration
ticket: EP-0008-US-0006-TK-0003-combat-journal-facts
title: "Журнал ручных пусков и истечения времени полёта"
stage: implemented
layer: engine
depends_on: ["EP-0008-US-0006-TK-0002-auto-launcher-selection"]
files_touched: 5
serves: ["AC-0001","AC-0002","AC-0004"]
created: 2026-10-07
revision: 1
---

# Журнал ручных пусков и истечения времени полёта

STATUS: IMPLEMENTED (automated checks; self-review)

## Why

Все противоракеты имеют ограниченное время полёта из своего модуля. Автоматика выбирает лучший по времени перехвата доступный аппарат и не дублирует автоматическую попытку. Этот тикет обеспечивает: журнал ручных пусков и истечения времени полёта.

## Decisions

Решения Q08–Q09,Q12–Q15,Q35,Q37,Q47–Q48 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0006-lifetime-and-auto-arbitration.md): AC-0001, AC-0002, AC-0004. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs:4 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs` | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/SimulationEngine.ManualDefense.cs` | Создаётся предыдущим тикетом EP-0008-US-0005-TK-0002-manual-launch-execution | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeJournalTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Execution scope update — 2026-10-11: baseline `e606480fbc22f726b65f102746cb08186a012388` published/remote verified. Add `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` to route the authoritative expiry event through journal recording. ManualDefense.cs needs no edit because shared spawn already records manual launch facts. Effective scope remains four files including tests.

Full Engine regression found one obsolete journal event sequence. Before edit, add `tests/DeepSpaceSaga.Engine.Tests/CombatJournalTests.cs` to include the new explicit Expired transition and verify its termination reason; retain event uniqueness/time/RNG assertions. Effective scope is five files including tests.

Шаг **22** [общего порядка](../../Tickets.md).
- [EP-0008-US-0006-TK-0002-auto-launcher-selection](../../EP-0008-US-0006-lifetime-and-auto-arbitration/EP-0008-US-0006-TK-0002-auto-launcher-selection/EP-0008-US-0006-TK-0002-auto-launcher-selection.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Публиковать launch mode, module ID, accuracy/maneuverability/frozen chance/roll и expired reason через существующие numeric journal facts и Result, без локализованной логики Engine. Различать time-expired и target/owner-lost, не создавать ложный Miss без roll. Повторный snapshot не дублирует события.
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

- [x] `Zero_percent_contact_records_roll_and_miss`
- [x] `Expiry_is_not_miss_or_damage`
- [x] `Concurrent_intercepts_record_single_target_destruction`

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

Dependency `e606480fbc22f726b65f102746cb08186a012388` published/remote verified. Countermeasure resolution/loss/expiry journal facts now include module/mode/captured accuracy/maneuverability/chance and explicit termination. Expired distinguishes lifetime from normal missed-coast completion; neither invents a roll or damage. Contact at lifetime emits actual resolution before expiry. Owner loss is distinct from target loss. Existing shared spawn already supplied manual/auto launch facts.

Focused lifecycle/journal tests **37/37 PASS**, full non-corpus Engine **1836/1837** initially found one obsolete expected journal sequence, fixed within documented scope. Final complete affected journal/defense group **131/131 PASS** (`ep8-us6-tk3-review.trx`); affected Client **68/68 PASS** (`ep8-us6-tk3-client.trx`). No repeated clean full-suite claim. New5 tests prove zero/100% contact, two simultaneous interceptors with one target termination, owner destruction, same-time miss/expiry, snapshot non-duplication and actual serializer/loader journal continuation. Initial new test assertions corrected ImmutableArray identity comparison and allowed the separately authorized A07 next hostile shot while still asserting original target removal. Solution build 0 warnings/errors, scoped format verify and diff PASS. Self-review complete; native/final corpus OPEN.
