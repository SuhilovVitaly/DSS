---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0011-phase-three-save-continuation
ticket: EP-0008-US-0011-TK-0001-new-save-version-gate
title: "Новая версия и явный отказ старым сохранениям"
stage: draft
layer: engine
depends_on: ["EP-0008-US-0010-TK-0003-defeat-navigation"]
files_touched: 5
serves: ["AC-0001","AC-0003"]
created: 2026-10-07
revision: 1
---

# Новая версия и явный отказ старым сохранениям

STATUS: DRAFT

## Why

Сохранение новой игры восстанавливает обе установки, активные снаряды и поведение противников. Старые сохранения явно отклоняются, а стартовые сценарии остаются доступными. Этот тикет обеспечивает: новая версия и явный отказ старым сохранениям.

## Decisions

Решения Q14,Q25,Q28,Q42,Q49 из [Decisions.md](../../Decisions.md), итоговый [контракт и A01–A12](../../Documentation.md). Применимые критерии [истории](../EP-0008-US-0011-phase-three-save-continuation.md): AC-0001, AC-0003. Карточка не вводит нового решения пользователя.

## Assumptions и baseline

HEAD `6907c58`, проверка 2026-10-07. Технические assumptions A01–A12 централизованы в эпике. Новые API и файлы являются планом; не утверждать их наличие до выполнения зависимостей. При новом baseline актуализировать пути и версии до изменения кода. Плановый scope — один production layer, до пяти файлов вместе с тестами. При выполнении через общий промт разрешено обоснованное расширение с записью в Code context.

## Code context

Пути относительно `D:/DeepSpaceSaga/DSS`. Это write scope; остальные первичные файлы можно читать для проверки зависимостей.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs` | Существует: src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:7 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs` | Существует: src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:11 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs` | Существует: src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:8 (baseline 6907c58) | Только результат и шаги этой карточки |
| `src/DeepSpaceSaga.Engine/Scenario/HostileCombatSaveData.cs` | Новый файл этого тикета | Versioned payload AI, используемый GameStateData |
| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeSaveVersionTests.cs` | Новый файл этого тикета | Только результат и шаги этой карточки |

## Dependencies

Шаг **35** [общего порядка](../../Tickets.md).
- [EP-0008-US-0010-TK-0003-defeat-navigation](../../EP-0008-US-0010-defeat-screen-and-recovery/EP-0008-US-0010-TK-0003-defeat-navigation/EP-0008-US-0010-TK-0003-defeat-navigation.md).
Предыдущий шаг должен быть выполнен и проверен; при автономном исполнении — также опубликован. Нельзя заменять отсутствующий production-контракт временным mock.

## Public API after the change

Предлагаемые DTO/методы/поля реализуют контракт EP-0008 в перечисленных production-файлах. Публичные изменения сериализуемы; Engine владеет guard/outcome, Motion — общей математикой, Client — отображением. Точные имена перечислены в шагах и могут уточняться без изменения семантики.

## Implementation steps

1. Перепроверить baseline, состояния зависимостей и реальные файлы; сохранить чужую работу.
2. Bump15→16 после повторной проверки baseline. Ввести versioned HostileCombatSaveData и поле GameStateData для AI snapshot/epochs; DTO определить в этом же тикете, полная валидация следует в следующем. На реальном CreateFromSaveFile/Load boundary reject all prior saves включая старые v0 files, но разрешать version0 через NewGame path. Не переписывать старые файлы, не запускать legacy ratings/migration path для отказанного save. Internal scenario fixtures остаются сценариями, нельзя маскировать старый save как новый простым номером.
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

- [ ] `Old_save_versions_are_rejected_without_mutation`
- [ ] `Version_zero_new_game_is_allowed`
- [ ] `Legacy_zero_version_save_is_rejected_as_save`

Команды из корня DSS после реализации:

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
git diff --check
```

Для изменённых C# дополнительно `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <точные изменённые C# пути>`. Фактические команды, backend и failures записать в evidence. Для документации проверить links/counts/schema; новые runtime tests для одной текстовой правки не требуются.

## Definition of Done

- [ ] Все AC-0001, AC-0003 в scope этой карточки покрыты реальным результатом/проверками.
- [ ] Все шаги выполнены, нет скрытых stub или неразрешённых зависимостей.
- [ ] Соответствующие тесты/build/format/diff пройдены; вызванные изменением регрессии исправлены.
- [ ] Review проведён, ограничения/native статус описаны честно.
- [ ] Board evidence актуально; при execution отдельный commit/push подтверждён.
- [ ] Собственное review не помечено пользовательским APPROVED.

## Self-containment check

Решения, числа, порядок событий, допущения, write scope, реальные зависимости и named checks доступны здесь и в связанных документах эпика. Для смысла коротких ответов не требуется искать чат. Предлагаемые API не являются доказательством готовой реализации.
