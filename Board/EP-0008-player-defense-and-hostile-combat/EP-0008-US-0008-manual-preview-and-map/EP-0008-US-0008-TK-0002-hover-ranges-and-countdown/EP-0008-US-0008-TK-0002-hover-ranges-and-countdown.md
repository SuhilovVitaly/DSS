---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0008-manual-preview-and-map
ticket: EP-0008-US-0008-TK-0002-hover-ranges-and-countdown
title: "Круги hover и время полёта на карте"
stage: draft
layer: client
depends_on: ["EP-0008-US-0008-TK-0001-preview-geometry"]
files_touched: 5
serves: ["AC-0002","AC-0003"]
created: 2026-10-07
revision: 1
---

# Круги hover и время полёта на карте

STATUS: DRAFT

## Why

Hover над пуском показывает прогноз конкретного аппарата. Радиусы появляются только у соответствующей панели, а активная ПРО показывает шанс и остаток времени. Этот тикет обеспечивает: круги hover и время полёта на карте.

## Decisions

Решения Q30,Q38,Q45–Q46,Q50 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0008-manual-preview-and-map.md): AC-0002, AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs:7 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualDefense.cs` | Создаётся предыдущим тикетом EP-0008-US-0007-TK-0002-panel-command-routing | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs:15 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Client.Tests/DefenseMapAnnotationsTests.cs` | Существует: tests/DeepSpaceSaga.Client.Tests/DefenseMapAnnotationsTests.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `tests/DeepSpaceSaga.Client.Tests/PhaseThreeMapTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Шаг **27** [общего порядка](../../Tickets.md).
- [EP-0008-US-0008-TK-0001-preview-geometry](../../EP-0008-US-0008-manual-preview-and-map/EP-0008-US-0008-TK-0001-preview-geometry/EP-0008-US-0008-TK-0001-preview-geometry.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Убрать permanent selected enemy defense circle; hover relevant module panel/его controls задаёт ровно нужные круги. PR count down из expires−presentation physical time capped pause, не walltime TTL. Не менять 2s real effect lifetime, скрытые обычные torpedo trails,5px markers. У нескольких PR labels различимы и не обрезаны.
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

- [ ] `No_hover_has_no_weapon_range_rings`
- [ ] `Hovered_module_uses_own_ranges`
- [ ] `Countdown_freezes_on_pause_and_resumes`
- [ ] `Multiple_countermeasures_keep_distinct_labels`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0002, AC-0003 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.
